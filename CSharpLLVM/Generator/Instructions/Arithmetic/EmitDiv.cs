using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compiler;
using CSharpLLVM.Helpers;
using CSharpLLVM.Stack;

namespace CSharpLLVM.Generator.Instructions.Arithmetic
{
    [InstructionHandler(Code.Div, Code.Div_Un)]
    class EmitDiv : ICodeEmitter
    {
        /// <summary>
        /// Emits a div instruction
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
                context.CurrentStack.Push(LLVM.BuildFDiv(builder, value1.Value, value2.Value, "divfp"));
            }
            else
            {
                ValueRef val1 = value1.Value;
                ValueRef val2 = value2.Value;

                if (value1.Type != value2.Type)
                {
                    val1 = LLVM.BuildIntCast(builder, val1, value2.Type, "divcast");
                }

                if (instruction.OpCode.Code == Code.Div)
                    context.CurrentStack.Push(LLVM.BuildSDiv(builder, val1, val2, "divsi"));
                else /* Div_Un */
                    context.CurrentStack.Push(LLVM.BuildUDiv(builder, val1, val2, "divui"));
            }
        }
    }
}
