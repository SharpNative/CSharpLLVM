﻿using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Stack;
using CSharpLLVM.Helpers;
using Mono.Cecil;
using CSharpLLVM.Compilation;
using System;

namespace CSharpLLVM.Generator.Instructions.Objects
{
    [InstructionHandler(Code.Newobj)]
    class EmitNewobj : ICodeEmitter
    {
        /// <summary>
        /// Emits a newobj instruction.
        /// </summary>
        /// <param name="instruction">The instruction.</param>
        /// <param name="context">The context.</param>
        /// <param name="builder">The builder.</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            MethodReference ctor = (MethodReference)instruction.Operand;
            TypeDefinition objType = ctor.DeclaringType.Resolve();
            TypeRef type = TypeHelper.GetTypeRefFromType(ctor.DeclaringType);

            ValueRef objPtr;
            bool ptr = (objType.IsClass && !objType.IsValueType);
            if (ptr)
            {
                // This type is a class, therefor we have a specialised "newobj" method.
                objPtr = LLVM.BuildCall(builder, context.Compiler.Lookup.GetNewobjMethod(ctor.DeclaringType.Resolve()), new ValueRef[0], "newobj");
            }
            else
            {
                // Not a class, allocate on stack.
                objPtr = LLVM.BuildAlloca(builder, type, "newobj");
            }

            // Constructor.
            ValueRef? ctorFunc = context.Compiler.Lookup.GetFunction(NameHelper.CreateMethodName(ctor));
            if (!ctorFunc.HasValue)
                throw new Exception("Constructor not found: " + ctor);

            // Get .ctor parameters.
            int paramCount = 1 + ctor.Parameters.Count;
            ValueRef[] values = new ValueRef[paramCount];
            values[0] = objPtr;
            for (int i = paramCount - 1; i >= 1; i--)
            {
                StackElement element = context.CurrentStack.Pop();
                values[i] = element.Value;

                // Cast needed?
                TypeRef paramType = TypeHelper.GetTypeRefFromType(ctor.Parameters[i - 1].ParameterType);
                if (element.Type != paramType)
                {
                    CastHelper.HelpIntAndPtrCast(builder, ref values[i], ref element.Type, paramType, "ctorcallcast");
                }
            }

            // Call .ctor.
            LLVM.BuildCall(builder, ctorFunc.Value, values, string.Empty);

            // Load and push object on stack.
            ValueRef obj = (ptr) ? objPtr : LLVM.BuildLoad(builder, objPtr, "obj");
            context.CurrentStack.Push(new StackElement(obj, ctor.DeclaringType, type));
        }
    }
}
