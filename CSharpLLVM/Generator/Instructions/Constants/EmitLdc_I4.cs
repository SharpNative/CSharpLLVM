using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compilation;
using CSharpLLVM.Helpers;
using CSharpLLVM.Stack;

namespace CSharpLLVM.Generator.Instructions.Constants
{
    [InstructionHandler(Code.Ldc_I4, Code.Ldc_I4_S, Code.Ldc_I4_0, Code.Ldc_I4_1, Code.Ldc_I4_2, Code.Ldc_I4_3, Code.Ldc_I4_4, Code.Ldc_I4_5, Code.Ldc_I4_6, Code.Ldc_I4_7, Code.Ldc_I4_8, Code.Ldc_I4_M1)]
    class EmitLdc_I4 : ICodeEmitter
    {
        /// <summary>
        /// Emits a Ldc_I4 instruction.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="builder">The builder.</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            ValueRef result;

            Code code = instruction.OpCode.Code;
            if (code >= Code.Ldc_I4_0 && code <= Code.Ldc_I4_8)
            {
                result = LLVM.ConstInt(TypeHelper.Int32, (ulong)(instruction.OpCode.Code - Code.Ldc_I4_0), true);
            }
            else if (code == Code.Ldc_I4_M1)
            {
                unchecked
                {
                    result = LLVM.ConstInt(TypeHelper.Int32, (uint)-1, true);
                }
            }
            else
            {
                if (instruction.Operand is sbyte)
                {
                    result = LLVM.ConstInt(TypeHelper.Int32, (ulong)(sbyte)instruction.Operand, true);
                }
                else
                {
                    unchecked
                    {
                        result = LLVM.ConstInt(TypeHelper.Int32, (uint)(int)instruction.Operand, true);
                    }
                }
            }

            context.CurrentStack.Push(new StackElement(result, typeof(int).GetTypeReference(), TypeHelper.Int32));
        }
    }
}
