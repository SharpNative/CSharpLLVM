using Swigged.LLVM;
using CSharpLLVM.Compilation;
using Mono.Cecil.Cil;
using System;
using Mono.Cecil;
using CSharpLLVM.Helpers;
using CSharpLLVM.Stack;

namespace CSharpLLVM.Generator.Instructions.Misc
{
    [InstructionHandler(Code.Ldtoken)]
    class EmitLdtoken : ICodeEmitter
    {
        /// <summary>
        /// Emits an ldtoken instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            object token = instruction.Operand;

            if (token is FieldReference)
            {
                FieldReference fieldRef = (FieldReference)token;
                FieldDefinition fieldDef = fieldRef.Resolve();

                // We only support array initialization by "System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray" currently
                if (instruction.Next.OpCode.Code == Code.Call)
                {
                    MethodReference methodRef = (MethodReference)instruction.Next.Operand;
                    if (methodRef.Name == "InitializeArray" && methodRef.DeclaringType.FullName == "System.Runtime.CompilerServices.RuntimeHelpers")
                    {
                        // Note that the top value on the stack is currently the destination array
                        // So that means if we have a char[] on the top, we need to interpret the array differently because we treat char as 8-bit...
                        StackElement top = context.CurrentStack.Peek();
                        bool isCharArray = (top.ILType.FullName == "System.Char[]");
                        int dstArrayLength = (isCharArray) ? (fieldDef.InitialValue.Length / 2) : (fieldDef.InitialValue.Length);
                        ValueRef[] values = new ValueRef[dstArrayLength];

                        // CIL creates this array as a byte[]
                        if (isCharArray)
                        {
                            for (int i = 0; i < dstArrayLength; i++)
                                values[i] = LLVM.ConstInt(TypeHelper.Int8, fieldDef.InitialValue[i * 2], false);
                        }
                        else
                        {
                            for (int i = 0; i < dstArrayLength; i++)
                                values[i] = LLVM.ConstInt(TypeHelper.Int8, fieldDef.InitialValue[i], false);
                        }

                        TypeRef globalType = LLVM.ArrayType(TypeHelper.Int8, (uint)dstArrayLength);
                        ValueRef global = LLVM.AddGlobal(context.Compiler.Module, globalType, "initarray");
                        LLVM.SetInitializer(global, LLVM.ConstArray(TypeHelper.Int8, values));

                        // Push the reference and the size
                        context.CurrentStack.Push(new StackElement(global, null, globalType));
                        ValueRef size = LLVM.ConstInt(TypeHelper.NativeIntType, (ulong)dstArrayLength, false);
                        context.CurrentStack.Push(new StackElement(size, null, TypeHelper.NativeIntType));
                    }
                    else
                    {
                        throw new NotImplementedException("Ldtoken: " + token.GetType() + " not implemented");
                    }
                }
                else
                {
                    throw new NotImplementedException("Ldtoken: " + token.GetType() + " not implemented");
                }
            }
            else
            {
                throw new NotImplementedException("Ldtoken: " + token.GetType() + " not implemented");
            }
        }
    }
}
