using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compiler;
using CSharpLLVM.Helpers;
using CSharpLLVM.Stack;

namespace CSharpLLVM.Generator.Instructions.Arithmetic
{
    [InstructionHandler(Code.Mul, Code.Mul_Ovf, Code.Mul_Ovf_Un)]
    class EmitMul : ICodeEmitter
    {
        /// <summary>
        /// Emits a mul instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            StackElement value2 = context.CurrentStack.Pop();
            StackElement value1 = context.CurrentStack.Pop();

            if (TypeHelper.IsFloatingPoint(value1) || TypeHelper.IsFloatingPoint(value2))
            {
                context.CurrentStack.Push(LLVM.BuildFMul(builder, value1.Value, value2.Value, "mulfp"));
            }
            else
            {
                ValueRef val1 = value1.Value;
                ValueRef val2 = value2.Value;

                if (value1.Type != value2.Type)
                {
                    val1 = LLVM.BuildIntCast(builder, val1, value2.Type, "mulcast");
                }

                context.CurrentStack.Push(LLVM.BuildMul(builder, val1, val2, "muli"));
            }
        }
    }
}
