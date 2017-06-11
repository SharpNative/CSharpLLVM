﻿using Mono.Cecil;
using Mono.Collections.Generic;
using Swigged.LLVM;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using CSharpLLVM.Generator;
using CSharpLLVM.Helpers;
using CSharpLLVM.Lookups;
using System.IO;

namespace CSharpLLVM.Compilation
{
    class Compiler
    {
        private ModuleRef mModule;
        private ContextRef mContext;
        private MethodCompiler mMethodCompiler;
        private TypeCompiler mTypeCompiler;
        private BuiltinRuntimeFunctions mBuiltinCompiler;

        private PassManagerRef mFunctionPassManager;
        private PassManagerRef mPassManager;

        public AssemblyDefinition AssemblyDef { get; private set; }
        public Options Options { get; private set; }
        public ModuleRef Module { get { return mModule; } }
        public ContextRef ModuleContext { get { return mContext; } }
        public CodeGenerator CodeGen { get; private set; }
        public TargetDataRef TargetData { get; private set; }
        public Lookup Lookup { get; private set; }

        /// <summary>
        /// Creates a new Compiler.
        /// </summary>
        /// <param name="options">The compiler options.</param>
        public Compiler(Options options)
        {
            Options = options;

            CodeGen = new CodeGenerator();
            Lookup = new Lookup();

            mMethodCompiler = new MethodCompiler(this);
            mBuiltinCompiler = new BuiltinRuntimeFunctions(this);
            mTypeCompiler = new TypeCompiler(this);
        }

        /// <summary>
        /// Verifies and optimizes a function.
        /// </summary>
        /// <param name="function">The function.</param>
        public void VerifyAndOptimizeFunction(ValueRef function)
        {
            if (LLVM.VerifyFunction(function, VerifierFailureAction.ReturnStatusAction))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                LLVM.VerifyFunction(function, VerifierFailureAction.PrintMessageAction);
                Console.ForegroundColor = ConsoleColor.Gray;
                throw new Exception("Compiling of function failed.");
            }

            LLVM.RunFunctionPassManager(mFunctionPassManager, function);
        }

        /// <summary>
        /// Compiles an IL assembly to LLVM bytecode.
        /// </summary>
        /// <param name="moduleName">The module name.</param>
        public void Compile(string moduleName)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            // Create LLVM module and its context.
            LLVM.EnablePrettyStackTrace();
            mModule = LLVM.ModuleCreateWithName(moduleName);
            mContext = LLVM.GetModuleContext(mModule);

            // Targets.
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

            // Optimizer.
            mFunctionPassManager = LLVM.CreateFunctionPassManagerForModule(mModule);
            mPassManager = LLVM.CreatePassManager();
            LLVM.InitializeFunctionPassManager(mFunctionPassManager);

            // O0
            if (Options.Optimization >= OptimizationLevel.O0)
            {
                // Function passes.
                LLVM.AddPromoteMemoryToRegisterPass(mFunctionPassManager);
                LLVM.AddConstantPropagationPass(mFunctionPassManager);
                LLVM.AddReassociatePass(mFunctionPassManager);
                LLVM.AddInstructionCombiningPass(mFunctionPassManager);

                // Module passes.
                LLVM.AddStripDeadPrototypesPass(mPassManager);
                LLVM.AddStripSymbolsPass(mPassManager);
            }

            // O1
            if (Options.Optimization >= OptimizationLevel.O1)
            {
                // Function passes.
                LLVM.AddLowerExpectIntrinsicPass(mFunctionPassManager);
                LLVM.AddEarlyCSEPass(mFunctionPassManager);
                LLVM.AddLoopRotatePass(mFunctionPassManager);
                LLVM.AddLoopUnswitchPass(mFunctionPassManager);
                LLVM.AddLoopUnrollPass(mFunctionPassManager);
                LLVM.AddLoopDeletionPass(mFunctionPassManager);
                LLVM.AddTailCallEliminationPass(mFunctionPassManager);
                LLVM.AddGVNPass(mFunctionPassManager);
                LLVM.AddDeadStoreEliminationPass(mFunctionPassManager);
                LLVM.AddJumpThreadingPass(mFunctionPassManager);
                LLVM.AddCFGSimplificationPass(mFunctionPassManager);
                LLVM.AddMemCpyOptPass(mFunctionPassManager);

                // Module passes.
                LLVM.AddAlwaysInlinerPass(mPassManager);
                LLVM.AddDeadArgEliminationPass(mPassManager);
                LLVM.AddAggressiveDCEPass(mFunctionPassManager);
            }

            // O2
            if (Options.Optimization >= OptimizationLevel.O2)
            {
                // Function passes.
                LLVM.AddLoopVectorizePass(mFunctionPassManager);
                LLVM.AddSLPVectorizePass(mFunctionPassManager);

                // Module passes.
                LLVM.AddFunctionInliningPass(mPassManager);
                LLVM.AddConstantMergePass(mPassManager);
                LLVM.AddArgumentPromotionPass(mPassManager);
            }

            // Initialize types and runtime.
            string dataLayout = LLVM.GetDataLayout(Module);
            TargetData = LLVM.CreateTargetData(dataLayout);

            TypeHelper.Init(TargetData, this);
            RuntimeHelper.ImportFunctions(Module);
            mBuiltinCompiler.Compile();
            compileModules();
            LLVM.RunPassManager(mPassManager, Module);

            // Log time.
            stopWatch.Stop();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("Compilation time: " + stopWatch.Elapsed);
            Console.ForegroundColor = ConsoleColor.Gray;

            // Debug: print LLVM assembly code.
#if DEBUG
            Console.WriteLine(LLVM.PrintModuleToString(mModule));
#endif

            // Verify and throw exception on error.
            Console.ForegroundColor = ConsoleColor.DarkGray;
            if (LLVM.VerifyModule(mModule, VerifierFailureAction.ReturnStatusAction, out error))
            {
                Console.WriteLine("Compilation of module failed");
                Console.WriteLine(error);
                LLVM.DisposeTargetData(TargetData);
                Console.ForegroundColor = ConsoleColor.Gray;
                return;
            }
            else
            {
                Console.WriteLine("Compilation of module succeeded");
            }
            Console.ForegroundColor = ConsoleColor.Gray;

            // Output assembly or object file.
            if (!Options.OutputLLVM)
            {
                TargetMachineRef machine = LLVM.CreateTargetMachine(target, triplet, "generic", "", CodeGenOptLevel.CodeGenLevelDefault, RelocMode.RelocDefault, CodeModel.CodeModelDefault);
                LLVM.SetModuleDataLayout(mModule, LLVM.CreateTargetDataLayout(machine));
                CodeGenFileType type = (Options.OutputAssembly) ? CodeGenFileType.AssemblyFile : CodeGenFileType.ObjectFile;

                if (LLVM.TargetMachineEmitToFile(machine, mModule, Options.OutputFile, type, out error))
                {
                    throw new InvalidOperationException(error);
                }
            }
            // Output LLVM code.
            else
            {
                File.WriteAllText(Options.OutputFile, LLVM.PrintModuleToString(mModule));
            }

            // Cleanup.
            LLVM.DisposeTargetData(TargetData);
        }

        /// <summary>
        /// Compiles the modules.
        /// </summary>
        private void compileModules()
        {
            AssemblyDef = AssemblyDefinition.ReadAssembly(Options.InputFile);

            // Loop through the modules within the IL assembly.
            // Note: A single assembly can contain multiple IL modules.
            //       We use a single LLVM module to contain all of this.
            Collection<ModuleDefinition> modules = AssemblyDef.Modules;
            foreach (ModuleDefinition moduleDef in modules)
            {
                compileModule(moduleDef);
            }

            // Create init method containing the calls to the .cctors.
            compileInitMethod();
        }

        /// <summary>
        /// Compiles the init method.
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
        /// Method to sort types based on dependencies.
        /// </summary>
        /// <param name="left">Left.</param>
        /// <param name="right">Right.</param>
        /// <returns>Order number.</returns>
        private int sortTypes(TypeDefinition left, TypeDefinition right)
        {
            if (TypeHelper.InheritsFrom(left, right))
            {
                Lookup.SetNeedVirtualCall(right, true);
                Lookup.SetNeedVirtualCall(left, true);
                return 1;
            }
            else if (TypeHelper.InheritsFrom(right, left))
            {
                Lookup.SetNeedVirtualCall(right, true);
                Lookup.SetNeedVirtualCall(left, true);
                return -1;
            }

            return 0;
        }

        /// <summary>
        /// Compiles a module.
        /// </summary>
        /// <param name="moduleDef">The IL module definition.</param>
        private void compileModule(ModuleDefinition moduleDef)
        {
            List<MethodDefinition> methods = new List<MethodDefinition>();
            List<MethodDefinition> ctors = new List<MethodDefinition>();

            Collection<TypeDefinition> types = moduleDef.Types;
            List<TypeDefinition> sortedTypes = new List<TypeDefinition>();

            // Sort types to help dependencies.
            foreach (TypeDefinition type in types)
            {
                if (type.FullName == "<Module>")
                    continue;

                sortedTypes.Add(type);
            }
            sortedTypes.Sort(sortTypes);

            // Compiles types and adds methods.
            foreach (TypeDefinition type in sortedTypes)
            {
                compileType(type, methods, ctors);
            }

            // Compile .ctors.
            foreach (MethodDefinition ctor in ctors)
            {
                compileMethod(ctor);
            }

            // Compile methods.
            foreach (MethodDefinition method in methods)
            {
                ValueRef? function = compileMethod(method);
                if (method.Name == ".cctor")
                    Lookup.AddCctor(method);
            }

            // Compile VTables.
            foreach (VTable vtable in Lookup.VTables)
            {
                vtable.Compile();
            }
        }

        /// <summary>
        /// Compiles a type.
        /// </summary>
        /// <param name="type">The type definition.</param>
        /// <param name="methods">The list of methods to add ours to.</param>
        /// <param name="ctors">The list of ctors to add our to.</param>
        private void compileType(TypeDefinition type, List<MethodDefinition> methods, List<MethodDefinition> ctors)
        {
            // Nested types.
            foreach (TypeDefinition inner in type.NestedTypes)
            {
                compileType(inner, methods, ctors);
            }

            mTypeCompiler.Compile(type);

            // Note: First, we need all types to generate before we can generate methods,
            //       because methods may refer to types that are not yet generated.
            if (!type.IsInterface)
            {
                foreach (MethodDefinition method in type.Methods)
                {
                    if (method.Name == ".ctor")
                        ctors.Add(method);
                    else
                        methods.Add(method);
                }
            }
        }

        /// <summary>
        /// Compiles a method.
        /// </summary>
        /// <param name="methodDef">The method definition.</param>
        /// <returns>The function.</returns>
        private ValueRef? compileMethod(MethodDefinition methodDef)
        {
            return mMethodCompiler.Compile(methodDef);
        }
    }
}
