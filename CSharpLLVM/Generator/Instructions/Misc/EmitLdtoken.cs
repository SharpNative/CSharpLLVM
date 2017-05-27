using Swigged.LLVM;
using CSharpLLVM.Compiler;
using Mono.Cecil.Cil;
using System;
using Mono.Cecil;
using CSharpLLVM.Helpers;

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

                // FIXME: we only support array initialization by "System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray" currently
                if (instruction.Next.OpCode.Code == Code.Call)
                {
                    MethodReference methodRef = (MethodReference)instruction.Next.Operand;
                    if (methodRef.Name == "InitializeArray" && methodRef.DeclaringType.FullName == "System.Runtime.CompilerServices.RuntimeHelpers")
                    {
                        // CIL creates this array as a byte[]
                        ValueRef[] values = new ValueRef[fieldDef.InitialValue.Length];
                        for (int i = 0; i < fieldDef.InitialValue.Length; i++)
                            values[i] = LLVM.ConstInt(TypeHelper.Int8, fieldDef.InitialValue[i], false);

                        ValueRef global = LLVM.AddGlobal(context.Compiler.Module, LLVM.ArrayType(TypeHelper.Int8, (uint)fieldDef.InitialValue.Length), "initarray");
                        LLVM.SetInitializer(global, LLVM.ConstArray(TypeHelper.Int8, values));

                        // Push the reference and the size
                        context.CurrentStack.Push(global);
                        context.CurrentStack.Push(LLVM.ConstInt(TypeHelper.NativeIntType, (ulong)fieldDef.InitialValue.Length, false));
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
