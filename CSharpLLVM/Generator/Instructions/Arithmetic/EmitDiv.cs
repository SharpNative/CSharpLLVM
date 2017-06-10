using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compilation;
using CSharpLLVM.Helpers;
using CSharpLLVM.Stack;

namespace CSharpLLVM.Generator.Instructions.Arithmetic
{
    [InstructionHandler(Code.Div, Code.Div_Un)]
    class EmitDiv : ICodeEmitter
    {
        /// <summary>
        /// Emits a div instruction.
        /// </summary>
        /// <param name="instruction">The instruction.</param>
        /// <param name="context">The context.</param>
        /// <param name="builder">The builder.</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            StackElement value2 = context.CurrentStack.Pop();
            StackElement value1 = context.CurrentStack.Pop();

            if (TypeHelper.IsFloatingPoint(value1) || TypeHelper.IsFloatingPoint(value2))
            {
                ValueRef result = LLVM.BuildFDiv(builder, value1.Value, value2.Value, "divfp");
                context.CurrentStack.Push(new StackElement(result, value1.ILType, value1.Type));
            }
            else
            {
                CastHelper.HelpIntCast(builder, ref value1, ref value2);
                ValueRef result;

                if (instruction.OpCode.Code == Code.Div)
                    result = LLVM.BuildSDiv(builder, value1.Value, value2.Value, "divsi");
                else /* Div_Un */
                    result = LLVM.BuildUDiv(builder, value1.Value, value2.Value, "divui");

                context.CurrentStack.Push(new StackElement(result, value1.ILType, value1.Type));
            }
        }
    }
}
