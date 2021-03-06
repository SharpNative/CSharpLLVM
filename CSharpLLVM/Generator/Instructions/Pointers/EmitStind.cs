﻿using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compilation;
using CSharpLLVM.Stack;
using CSharpLLVM.Helpers;

namespace CSharpLLVM.Generator.Instructions.Arrays
{
    [InstructionHandler(Code.Stind_I, Code.Stind_I1, Code.Stind_I2, Code.Stind_I4, Code.Stind_I8, Code.Stind_R4, Code.Stind_R8)]
    class EmitStind : ICodeEmitter
    {
        /// <summary>
        /// Emits a stind instruction.
        /// </summary>
        /// <param name="instruction">The instruction.</param>
        /// <param name="context">The context.</param>
        /// <param name="builder">The builder.</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            StackElement value = context.CurrentStack.Pop();
            StackElement pointer = context.CurrentStack.Pop();

            ValueRef val = value.Value;
            ValueRef ptr = pointer.Value;
            TypeRef destType = TypeHelper.GetTypeRefFromStOrLdind(instruction.OpCode.Code);
            TypeRef destPtrType = LLVM.PointerType(destType, 0);
            if (destPtrType != pointer.Type)
            {
                // There is no instruction for a Stind to store a boolean, so we need to check manually.
                if (pointer.Type == LLVM.PointerType(TypeHelper.Boolean, 0))
                {
                    destType = TypeHelper.Boolean;
                }
                // We treat char as 8-bit.
                else if (pointer.Type == LLVM.PointerType(TypeHelper.Int8, 0))
                {
                    destType = TypeHelper.Int8;
                }

                val = LLVM.BuildIntCast(builder, val, destType, "stindcast");

                // If the pointer is a native int, convert to a pointer of the appropriate type.
                if (pointer.Type == TypeHelper.NativeIntType)
                    ptr = LLVM.BuildIntToPtr(builder, ptr, destPtrType, "int2ptr");
            }

            LLVM.BuildStore(builder, val, ptr);
        }
    }
}
