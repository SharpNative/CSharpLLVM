﻿using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compilation;
using System;
using Mono.Cecil;
using CSharpLLVM.Stack;
using CSharpLLVM.Helpers;

namespace CSharpLLVM.Generator.Instructions.StoreLoad
{
    [InstructionHandler(Code.Ldsfld)]
    class EmitLdsfld : ICodeEmitter
    {
        /// <summary>
        /// Emits a ldsfld instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            FieldReference field = (FieldReference)instruction.Operand;
            ValueRef? fieldValue = context.Compiler.Lookup.GetStaticField(field);
            if (fieldValue == null)
                throw new InvalidOperationException("Unknown static field: " + field);
            
            ValueRef result = LLVM.BuildLoad(builder, fieldValue.Value, "ldsfld");
            if (instruction.HasPrefix(Code.Volatile))
                LLVM.SetVolatile(result, true);

            TypeRef resultType = LLVM.TypeOf(result);
            context.CurrentStack.Push(new StackElement(result, TypeHelper.GetTypeFromTypeReference(context.Compiler, field.FieldType), resultType));
        }
    }
}
