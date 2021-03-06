﻿using CSharpLLVM.Generator;
using CSharpLLVM.Helpers;
using CSharpLLVM.Runtime.CIL;
using Mono.Cecil;
using Swigged.LLVM;
using System;

namespace CSharpLLVM.Compilation
{
    class MethodCompiler
    {
        private Compiler mCompiler;
        private CILRuntimeMethodCompiler mRuntimeCompiler;

        /// <summary>
        /// Creates a new MethodCompiler.
        /// </summary>
        /// <param name="compiler">The compiler.</param>
        public MethodCompiler(Compiler compiler)
        {
            mCompiler = compiler;
            mRuntimeCompiler = new CILRuntimeMethodCompiler(compiler);
        }

        /// <summary>
        /// Creates the declaration for a method.
        /// </summary>
        /// <param name="methodDef">The method definition.</param>
        public void CreateDeclaration(MethodDefinition methodDef)
        {
            string methodName = NameHelper.CreateMethodName(methodDef);
            int paramCount = methodDef.Parameters.Count;
            
            // CIL runtime method?
            if (methodDef.IsRuntime && !mRuntimeCompiler.MustHandleMethod(methodDef))
                return;

            // If we expect an instance reference as first argument, then we need to make sure our for loop has an offset.
            int offset = 0;
            if (methodDef.HasThis)
            {
                paramCount++;
                offset = 1;
            }

            // Fill in arguments.
            TypeRef[] argTypes = new TypeRef[paramCount];
            for (int i = offset; i < paramCount; i++)
            {
                TypeReference type = methodDef.Parameters[i - offset].ParameterType;
                argTypes[i] = TypeHelper.GetTypeRefFromType(type);
            }

            // If needed, fill in the instance reference.
            if (methodDef.HasThis)
            {
                argTypes[0] = TypeHelper.GetTypeRefFromType(methodDef.DeclaringType);

                // We need to pass the valuetype as a pointer because we need to modify its contents in the constructor.
                if (methodDef.DeclaringType.IsValueType)
                    argTypes[0] = LLVM.PointerType(argTypes[0], 0);
            }

            // Create LLVM function type and add function to the lookup table.
            TypeRef functionType = LLVM.FunctionType(TypeHelper.GetTypeRefFromType(methodDef.ReturnType), argTypes, false);
            ValueRef function = LLVM.AddFunction(mCompiler.Module, methodName, functionType);
            mCompiler.Lookup.AddFunction(methodName, function);
        }

        /// <summary>
        /// Compiles a method.
        /// </summary>
        /// <param name="methodDef">The method definition.</param>
        public void Compile(MethodDefinition methodDef)
        {
            string methodName = NameHelper.CreateMethodName(methodDef);
            ValueRef? function = mCompiler.Lookup.GetFunction(methodName);

            // It is possible we didn't create a declaration because we don't want to generate this method.
            // In that case, don't continue.
            if (!function.HasValue)
                return;

            // A method has internal linkage if one (or both) of the following is true:
            // 1) The method is a private method.
            // 2) The compiler got an option to set the linkage of instance methods to internal.
            if (!methodDef.IsStatic && (methodDef.IsPrivate || mCompiler.Options.InstanceMethodInternalLinkage))
            {
                LLVM.SetLinkage(function.Value, Linkage.InternalLinkage);
                if (mCompiler.Options.InternalMethodsFastCC)
                    LLVM.SetFunctionCallConv(function.Value, 8);
            }

            // Only generate if it has a body.
            if (!methodDef.HasBody || methodDef.Body.CodeSize == 0)
            {
                LLVM.SetLinkage(function.Value, Linkage.ExternalLinkage);
                return;
            }

            // Compile instructions.
            MethodContext ctx = new MethodContext(mCompiler, methodDef, function.Value);
            InstructionEmitter emitter = new InstructionEmitter(ctx);
            try
            {
                emitter.EmitInstructions(mCompiler.CodeGen);
                mCompiler.VerifyAndOptimizeFunction(function.Value);
            }
            catch (Exception e)
            {
                Logger.LogError("Exception inside method {0}: {1}", methodDef, e.Message);
                Logger.LogDetail("----------");
                Logger.LogInfo(e.StackTrace);
            }
        }
    }
}
