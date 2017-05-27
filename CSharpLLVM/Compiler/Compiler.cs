using Mono.Cecil;
using Mono.Collections.Generic;
using Swigged.LLVM;
using System;
using System.Collections.Generic;
using System.Reflection;
using CSharpLLVM.Generator;
using CSharpLLVM.Helpers;

namespace CSharpLLVM.Compiler
{
    class Compiler
    {
        private ModuleRef mmodule;
        private ContextRef mcontext;
        private MethodCompiler mmethodCompiler;
        private TypeCompiler mtypeCompiler;

        private PassManagerRef mfunctionPassManager;
        private PassManagerRef mpassManager;

        public Assembly Asm;
        public CompilerSettings Settings { get; private set; }
        public ModuleRef Module { get { return mmodule; } }
        public ContextRef ModuleContext { get { return mcontext; } }
        public CodeGenerator CodeGen { get; private set; }
        public TargetDataRef TargetData { get; private set; }
        public Lookup Lookup { get; private set; }
        
        /// <summary>
        /// Creates a new Compiler
        /// </summary>
        /// <param name="settings">The compiler settings</param>
        public Compiler(CompilerSettings settings)
        {
            Settings = settings;
            
            CodeGen = new CodeGenerator();
            Lookup = new Lookup();

            mmethodCompiler = new MethodCompiler(this);
            mtypeCompiler = new TypeCompiler(this, Lookup);
        }
        
        /// <summary>
        /// Verifies and optimizes a function
        /// </summary>
        /// <param name="function">The function</param>
        public void VerifyAndOptimizeFunction(ValueRef function)
        {
            LLVM.VerifyFunction(function, VerifierFailureAction.PrintMessageAction);
            LLVM.RunFunctionPassManager(mfunctionPassManager, function);
        }

        /// <summary>
        /// Compiles an IL assembly to LLVM bytecode
        /// </summary>
        public void Compile()
        {
            // Create LLVM module and its context
            LLVM.EnablePrettyStackTrace();
            mmodule = LLVM.ModuleCreateWithName(Settings.ModuleName);
            mcontext = LLVM.GetModuleContext(mmodule);

            // Targets
            LLVM.InitializeAllTargetInfos();
            LLVM.InitializeAllTargets();
            LLVM.InitializeAllTargetMCs();
            LLVM.InitializeAllAsmParsers();
            LLVM.InitializeAllAsmPrinters();

            //string triplet = LLVM.GetDefaultTargetTriple();
            string triplet = "x86_64-pc-linux";
            string error;

            LLVM.SetTarget(mmodule, triplet);
            TargetRef target;
            if (LLVM.GetTargetFromTriple(triplet, out target, out error))
            {
                throw new InvalidOperationException(error);
            }

            // Initialize types and runtime
            string dataLayout = LLVM.GetDataLayout(Module);
            TargetData = LLVM.CreateTargetData(dataLayout);
            TypeHelper.Init(TargetData, Lookup);
            RuntimeHelper.ImportFunctions(Module);
            
            // Optimizer
            // TODO: more optimizations, the ones here are just the basic ones that are always active
            mfunctionPassManager = LLVM.CreateFunctionPassManagerForModule(mmodule);
            LLVM.InitializeFunctionPassManager(mfunctionPassManager);
            LLVM.AddPromoteMemoryToRegisterPass(mfunctionPassManager);
            /*LLVM.AddConstantPropagationPass(mfunctionPassManager);
            LLVM.AddReassociatePass(mfunctionPassManager);
            LLVM.AddInstructionCombiningPass(mfunctionPassManager);
            LLVM.AddMemCpyOptPass(mfunctionPassManager);
            LLVM.AddLoopUnrollPass(mfunctionPassManager);
            LLVM.AddLoopUnswitchPass(mfunctionPassManager);
            LLVM.AddTailCallEliminationPass(mfunctionPassManager);
            LLVM.AddGVNPass(mfunctionPassManager);
            LLVM.AddJumpThreadingPass(mfunctionPassManager);
            LLVM.AddCFGSimplificationPass(mfunctionPassManager);*/

            mpassManager = LLVM.CreatePassManager();
            LLVM.AddAlwaysInlinerPass(mpassManager);
            LLVM.AddFunctionInliningPass(mpassManager);
            /*LLVM.AddStripDeadPrototypesPass(mpassManager);
            LLVM.AddStripSymbolsPass(mpassManager);*/

            compileModules();

            LLVM.RunPassManager(mpassManager, Module);

            // Debug: print LLVM assembly code
            Console.WriteLine(LLVM.PrintModuleToString(mmodule));

            // Verify and throw exception on error
            if (LLVM.VerifyModule(mmodule, VerifierFailureAction.PrintMessageAction, out error))
            {
                throw new InvalidOperationException(error);
            }

            // Output
            TargetMachineRef machine = LLVM.CreateTargetMachine(target, triplet, "generic", "", CodeGenOptLevel.CodeGenLevelDefault, RelocMode.RelocDefault, CodeModel.CodeModelDefault);
            LLVM.SetModuleDataLayout(mmodule, LLVM.CreateTargetDataLayout(machine));
            if (LLVM.TargetMachineEmitToFile(machine, mmodule, "./out.o", CodeGenFileType.ObjectFile, out error))
            {
                throw new InvalidOperationException(error);
            }

            if (LLVM.TargetMachineEmitToFile(machine, mmodule, "./out.s", CodeGenFileType.AssemblyFile, out error))
            {
                throw new InvalidOperationException(error);
            }

            // Cleanup
            LLVM.DisposeTargetData(TargetData);
        }

        /// <summary>
        /// Compiles the modules
        /// </summary>
        private void compileModules()
        {
            Asm = Assembly.LoadFrom(Settings.InputFile);
            AssemblyDefinition asmDef = AssemblyDefinition.ReadAssembly(Settings.InputFile);

            // Loop through the modules within the IL assembly
            // Note: A single assembly can contain multiple IL modules.
            //       We use a single LLVM module to contain all of this
            Collection<ModuleDefinition> modules = asmDef.Modules;
            foreach (ModuleDefinition moduleDef in modules)
            {
                compileModule(moduleDef);
            }
            
            // Create init method containing the calls to the .cctors
            compileInitMethod();
        }

        /// <summary>
        /// Compiles the init method
        /// </summary>
        private void compileInitMethod()
        {
            TypeRef type = LLVM.FunctionType(TypeHelper.Void, new TypeRef[0], false);
            ValueRef func = LLVM.AddFunction(Module, "initCctors", type);
            BuilderRef builder = LLVM.CreateBuilderInContext(ModuleContext);
            LLVM.PositionBuilderAtEnd(builder, LLVM.AppendBasicBlockInContext(ModuleContext, func, string.Empty));

            MethodDefinition[] cctors = Lookup.GetStaticConstructors();
            foreach (MethodDefinition method in cctors)
            {
                LLVM.BuildCall(builder, Lookup.GetFunction(NameHelper.CreateMethodName(method)).Value, new ValueRef[0], string.Empty);
            }

            LLVM.BuildRetVoid(builder);
            LLVM.DisposeBuilder(builder);
        }

        /// <summary>
        /// Compiles a module
        /// </summary>
        /// <param name="moduleDef">The IL module definition</param>
        private void compileModule(ModuleDefinition moduleDef)
        {
            List<MethodDefinition> methods = new List<MethodDefinition>();
            Collection<TypeDefinition> types = moduleDef.Types;

            // Compiles types and adds methods
            foreach (TypeDefinition type in types)
            {
                compileType(type, methods);
            }

            // Compile methods
            foreach (MethodDefinition method in methods)
            {
                ValueRef? function = compileMethod(method);
                if (method.Name == ".cctor")
                    Lookup.AddCctor(method);
            }
        }

        /// <summary>
        /// Compiles a type
        /// </summary>
        /// <param name="type">The type definition</param>
        private void compileType(TypeDefinition type, List<MethodDefinition> methods)
        {
            // Nested types
            foreach (TypeDefinition inner in type.NestedTypes)
            {
                compileType(inner, methods);
            }

            mtypeCompiler.Compile(type);
            
            // Note: we first need all types to generate before we can generate methods
            //       because methods may refer to types that are not yet generated
            methods.AddRange(type.Methods);
        }

        /// <summary>
        /// Compiles a method
        /// </summary>
        /// <param name="methodDef">The method definition</param>
        /// <returns>The function</returns>
        private ValueRef? compileMethod(MethodDefinition methodDef)
        {
            return mmethodCompiler.Compile(methodDef);
        }
    }
}
