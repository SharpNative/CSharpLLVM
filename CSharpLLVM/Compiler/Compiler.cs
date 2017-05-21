using Mono.Cecil;
using Mono.Collections.Generic;
using Swigged.LLVM;
using System;
using System.Collections.Generic;
using CSharpLLVM.Generator;

namespace CSharpLLVM.Compiler
{
    class Compiler
    {
        private CompilerSettings m_settings;
        private ModuleRef m_module;
        private ContextRef m_context;
        private PassManagerRef m_passManager;
        private MethodCompiler m_methodCompiler;

        public ModuleRef Module { get { return m_module; } }
        public ContextRef ModuleContext { get { return m_context; } }
        public CodeGenerator CodeGen { get; private set; }

        public Dictionary<string, ValueRef> m_functionLookup = new Dictionary<string, ValueRef>();

        /// <summary>
        /// Creates a new Compiler
        /// </summary>
        /// <param name="settings">The compiler settings</param>
        public Compiler(CompilerSettings settings)
        {
            m_settings = settings;
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
        /// Verifies and optimizes a function
        /// </summary>
        /// <param name="function">The function</param>
        public void VerifyAndOptimizeFunction(ValueRef function)
        {
            LLVM.VerifyFunction(function, VerifierFailureAction.PrintMessageAction);
            LLVM.RunFunctionPassManager(m_passManager, function);
        }

        /// <summary>
        /// Compiles an IL assembly to LLVM bytecode
        /// </summary>
        public void Compile()
        {
            // Create LLVM module and its context
            LLVM.EnablePrettyStackTrace();
            m_module = LLVM.ModuleCreateWithName(m_settings.ModuleName);
            m_context = LLVM.GetModuleContext(m_module);

            // Optimizer
            // TODO: more optimizations, the ones here are just the basic ones that are always active
            m_passManager = LLVM.CreateFunctionPassManagerForModule(m_module);
            LLVM.AddPromoteMemoryToRegisterPass(m_passManager);
            LLVM.AddConstantPropagationPass(m_passManager);
            LLVM.AddReassociatePass(m_passManager);
            LLVM.AddTailCallEliminationPass(m_passManager);
            LLVM.AddInstructionCombiningPass(m_passManager);
            LLVM.AddGVNPass(m_passManager);
            LLVM.AddCFGSimplificationPass(m_passManager);
            LLVM.InitializeFunctionPassManager(m_passManager);

            // Loop through the modules within the IL assembly
            // Note: A single assembly can contain multiple IL modules.
            //       We use a single LLVM module to contain all of this
            AssemblyDefinition asmDef = AssemblyDefinition.ReadAssembly(m_settings.InputFile);
            Collection<ModuleDefinition> modules = asmDef.Modules;
            foreach (ModuleDefinition moduleDef in modules)
            {
                compileModule(moduleDef);
            }

            // Debug: print LLVM assembly code
            Console.WriteLine(LLVM.PrintModuleToString(m_module));

            // Verify and throw exception on error
            string error = "";
            if (LLVM.VerifyModule(m_module, VerifierFailureAction.PrintMessageAction, out error))
            {
                throw new InvalidOperationException(error);
            }

            LLVM.InitializeAllTargetInfos();
            LLVM.InitializeAllTargets();
            LLVM.InitializeAllTargetMCs();
            LLVM.InitializeAllAsmParsers();
            LLVM.InitializeAllAsmPrinters();

            //string triplet = LLVM.GetDefaultTargetTriple();
            string triplet = "x86_64-pc-linux";

            LLVM.SetTarget(m_module, triplet);
            TargetRef target;
            if (LLVM.GetTargetFromTriple(triplet, out target, out error))
            {
                throw new InvalidOperationException(error);
            }

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
        }

        /// <summary>
        /// Compiles a module
        /// </summary>
        /// <param name="moduleDef">The IL module definition</param>
        private void compileModule(ModuleDefinition moduleDef)
        {
            Collection<TypeDefinition> types = moduleDef.Types;
            foreach (TypeDefinition type in types)
            {
                compileType(type);
            }
        }

        /// <summary>
        /// Compiles a type
        /// </summary>
        /// <param name="type">The type definition</param>
        private void compileType(TypeDefinition type)
        {
            Collection<MethodDefinition> methods = type.Methods;
            foreach (MethodDefinition methodDef in methods)
            {
                compileMethod(methodDef);
            }
        }

        /// <summary>
        /// Compiles a method
        /// </summary>
        /// <param name="methodDef">The method definition</param>
        private void compileMethod(MethodDefinition methodDef)
        {
            m_methodCompiler.Compile(methodDef);
        }
    }
}
