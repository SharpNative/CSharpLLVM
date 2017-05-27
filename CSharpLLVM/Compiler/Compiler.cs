using Mono.Cecil;
using Mono.Collections.Generic;
using Swigged.LLVM;
using System;
using System.Collections.Generic;
using CSharpLLVM.Generator;
using CSharpLLVM.Helpers;
using System.Reflection;

namespace CSharpLLVM.Compiler
{
    class Compiler
    {
        private ModuleRef m_module;
        private ContextRef m_context;
        private MethodCompiler m_methodCompiler;

        private PassManagerRef m_functionPassManager;
        private PassManagerRef m_passManager;

        public Assembly Asm;
        public CompilerSettings Settings { get; private set; }
        public ModuleRef Module { get { return m_module; } }
        public ContextRef ModuleContext { get { return m_context; } }
        public CodeGenerator CodeGen { get; private set; }
        public TargetDataRef TargetData { get; private set; }

        private Dictionary<string, ValueRef> m_functionLookup = new Dictionary<string, ValueRef>();
        private Dictionary<FieldReference, ValueRef> m_staticFieldLookup = new Dictionary<FieldReference, ValueRef>();
        private List<MethodDefinition> m_cctors = new List<MethodDefinition>();

        /// <summary>
        /// Creates a new Compiler
        /// </summary>
        /// <param name="settings">The compiler settings</param>
        public Compiler(CompilerSettings settings)
        {
            Settings = settings;
            m_methodCompiler = new MethodCompiler(this);
            CodeGen = new CodeGenerator();
        }

        /// <summary>
        /// Gets a function
        /// </summary>
        /// <param name="name">The name</param>
        /// <returns>The function</returns>
        public ValueRef? GetFunction(string name)
        {
            if (m_functionLookup.ContainsKey(name))
                return m_functionLookup[name];

            return null;
        }

        /// <summary>
        /// Adds a function
        /// </summary>
        /// <param name="name">The name</param>
        /// <param name="function">The function</param>
        public void AddFunction(string name, ValueRef function)
        {
            m_functionLookup.Add(name, function);
        }

        /// <summary>
        /// Gets a static field
        /// </summary>
        /// <param name="field">The static field</param>
        /// <returns>The field</returns>
        public ValueRef? GetStaticField(FieldReference field)
        {
            if (m_staticFieldLookup.ContainsKey(field))
                return m_staticFieldLookup[field];

            return null;
        }

        /// <summary>
        /// Verifies and optimizes a function
        /// </summary>
        /// <param name="function">The function</param>
        public void VerifyAndOptimizeFunction(ValueRef function)
        {
            LLVM.VerifyFunction(function, VerifierFailureAction.PrintMessageAction);
            LLVM.RunFunctionPassManager(m_functionPassManager, function);
        }

        /// <summary>
        /// Compiles an IL assembly to LLVM bytecode
        /// </summary>
        public void Compile()
        {
            // Create LLVM module and its context
            LLVM.EnablePrettyStackTrace();
            m_module = LLVM.ModuleCreateWithName(Settings.ModuleName);
            m_context = LLVM.GetModuleContext(m_module);

            // Targets
            LLVM.InitializeAllTargetInfos();
            LLVM.InitializeAllTargets();
            LLVM.InitializeAllTargetMCs();
            LLVM.InitializeAllAsmParsers();
            LLVM.InitializeAllAsmPrinters();

            //string triplet = LLVM.GetDefaultTargetTriple();
            string triplet = "x86_64-pc-linux";
            string error;

            LLVM.SetTarget(m_module, triplet);
            TargetRef target;
            if (LLVM.GetTargetFromTriple(triplet, out target, out error))
            {
                throw new InvalidOperationException(error);
            }

            // Initialize types and runtime
            string dataLayout = LLVM.GetDataLayout(Module);
            TargetData = LLVM.CreateTargetData(dataLayout);
            TypeHelper.Init(TargetData);
            RuntimeHelper.ImportFunctions(Module);
            
            // Optimizer
            // TODO: more optimizations, the ones here are just the basic ones that are always active
            m_functionPassManager = LLVM.CreateFunctionPassManagerForModule(m_module);
            LLVM.InitializeFunctionPassManager(m_functionPassManager);
            LLVM.AddPromoteMemoryToRegisterPass(m_functionPassManager);
            /*LLVM.AddConstantPropagationPass(m_functionPassManager);
            LLVM.AddReassociatePass(m_functionPassManager);
            LLVM.AddInstructionCombiningPass(m_functionPassManager);
            LLVM.AddMemCpyOptPass(m_functionPassManager);
            LLVM.AddLoopUnrollPass(m_functionPassManager);
            LLVM.AddLoopUnswitchPass(m_functionPassManager);
            LLVM.AddTailCallEliminationPass(m_functionPassManager);
            LLVM.AddGVNPass(m_functionPassManager);
            LLVM.AddJumpThreadingPass(m_functionPassManager);
            LLVM.AddCFGSimplificationPass(m_functionPassManager);*/

            m_passManager = LLVM.CreatePassManager();
            LLVM.AddAlwaysInlinerPass(m_passManager);
            LLVM.AddFunctionInliningPass(m_passManager);
            /*LLVM.AddStripDeadPrototypesPass(m_passManager);
            LLVM.AddStripSymbolsPass(m_passManager);*/

            compileModules();

            LLVM.RunPassManager(m_passManager, Module);

            // Debug: print LLVM assembly code
            Console.WriteLine(LLVM.PrintModuleToString(m_module));

            // Verify and throw exception on error
            if (LLVM.VerifyModule(m_module, VerifierFailureAction.PrintMessageAction, out error))
            {
                throw new InvalidOperationException(error);
            }

            // Output
            TargetMachineRef machine = LLVM.CreateTargetMachine(target, triplet, "generic", "", CodeGenOptLevel.CodeGenLevelDefault, RelocMode.RelocDefault, CodeModel.CodeModelDefault);
            LLVM.SetModuleDataLayout(m_module, LLVM.CreateTargetDataLayout(machine));
            if (LLVM.TargetMachineEmitToFile(machine, m_module, "./out.o", CodeGenFileType.ObjectFile, out error))
            {
                throw new InvalidOperationException(error);
            }

            if (LLVM.TargetMachineEmitToFile(machine, m_module, "./out.s", CodeGenFileType.AssemblyFile, out error))
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

            foreach (MethodDefinition method in m_cctors)
            {
                LLVM.BuildCall(builder, GetFunction(NameHelper.CreateMethodName(method)).Value, new ValueRef[0], string.Empty);
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
                    m_cctors.Add(method);
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

            // Log
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine(string.Format("Compiling type {0}", type.FullName));
            Console.ForegroundColor = ConsoleColor.Gray;

            // Fields
            foreach (FieldDefinition field in type.Fields)
            {
                if (field.FullName[0] == '<')
                    continue;

                if (field.IsStatic)
                {
                    TypeRef fieldType = TypeHelper.GetTypeRefFromType(field.FieldType);
                    ValueRef val = LLVM.AddGlobal(Module, fieldType, NameHelper.CreateFieldName(field.FullName));

                    // Note: the initializer may be changed later if the compiler sees that it can be constant
                    LLVM.SetInitializer(val, LLVM.ConstNull(fieldType));
                    m_staticFieldLookup.Add(field, val);
                }
            }

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
            return m_methodCompiler.Compile(methodDef);
        }
    }
}
