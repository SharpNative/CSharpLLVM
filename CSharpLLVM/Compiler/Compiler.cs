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
        private ModuleRef mModule;
        private ContextRef mContext;
        private MethodCompiler mMethodCompiler;
        private TypeCompiler mTypeCompiler;

        private PassManagerRef mFunctionPassManager;
        private PassManagerRef mPassManager;

        public Assembly Asm;
        public CompilerSettings Settings { get; private set; }
        public ModuleRef Module { get { return mModule; } }
        public ContextRef ModuleContext { get { return mContext; } }
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

            mMethodCompiler = new MethodCompiler(this);
            mTypeCompiler = new TypeCompiler(this, Lookup);
        }
        
        /// <summary>
        /// Verifies and optimizes a function
        /// </summary>
        /// <param name="function">The function</param>
        public void VerifyAndOptimizeFunction(ValueRef function)
        {
            LLVM.VerifyFunction(function, VerifierFailureAction.PrintMessageAction);
            LLVM.RunFunctionPassManager(mFunctionPassManager, function);
        }

        /// <summary>
        /// Compiles an IL assembly to LLVM bytecode
        /// </summary>
        public void Compile()
        {
            // Create LLVM module and its context
            LLVM.EnablePrettyStackTrace();
            mModule = LLVM.ModuleCreateWithName(Settings.ModuleName);
            mContext = LLVM.GetModuleContext(mModule);

            // Targets
            LLVM.InitializeAllTargetInfos();
            LLVM.InitializeAllTargets();
            LLVM.InitializeAllTargetMCs();
            LLVM.InitializeAllAsmParsers();
            LLVM.InitializeAllAsmPrinters();

            //string triplet = LLVM.GetDefaultTargetTriple();
            string triplet = "x86_64-pc-linux";
            string error;

            LLVM.SetTarget(mModule, triplet);
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
            // TODO
            mFunctionPassManager = LLVM.CreateFunctionPassManagerForModule(mModule);
            LLVM.InitializeFunctionPassManager(mFunctionPassManager);
            LLVM.AddPromoteMemoryToRegisterPass(mFunctionPassManager);
            /*LLVM.AddConstantPropagationPass(mFunctionPassManager);
            LLVM.AddReassociatePass(mFunctionPassManager);
            LLVM.AddInstructionCombiningPass(mFunctionPassManager);
            LLVM.AddMemCpyOptPass(mFunctionPassManager);
            LLVM.AddLoopUnrollPass(mFunctionPassManager);
            LLVM.AddLoopUnswitchPass(mFunctionPassManager);
            LLVM.AddTailCallEliminationPass(mFunctionPassManager);
            LLVM.AddGVNPass(mFunctionPassManager);
            LLVM.AddJumpThreadingPass(mFunctionPassManager);*/
            LLVM.AddCFGSimplificationPass(mFunctionPassManager);

            mPassManager = LLVM.CreatePassManager();
            LLVM.AddAlwaysInlinerPass(mPassManager);
            LLVM.AddFunctionInliningPass(mPassManager);
            /*LLVM.AddStripDeadPrototypesPass(mPassManager);
            LLVM.AddStripSymbolsPass(mPassManager);*/

            compileModules();

            LLVM.RunPassManager(mPassManager, Module);

            // Debug: print LLVM assembly code
            Console.WriteLine(LLVM.PrintModuleToString(mModule));

            // Verify and throw exception on error
            if (LLVM.VerifyModule(mModule, VerifierFailureAction.PrintMessageAction, out error))
            {
                throw new InvalidOperationException(error);
            }

            // Output
            TargetMachineRef machine = LLVM.CreateTargetMachine(target, triplet, "generic", "", CodeGenOptLevel.CodeGenLevelDefault, RelocMode.RelocDefault, CodeModel.CodeModelDefault);
            LLVM.SetModuleDataLayout(mModule, LLVM.CreateTargetDataLayout(machine));
            if (LLVM.TargetMachineEmitToFile(machine, mModule, "./out.o", CodeGenFileType.ObjectFile, out error))
            {
                throw new InvalidOperationException(error);
            }

            if (LLVM.TargetMachineEmitToFile(machine, mModule, "./out.s", CodeGenFileType.AssemblyFile, out error))
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

            mTypeCompiler.Compile(type);
            
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
            return mMethodCompiler.Compile(methodDef);
        }
    }
}
