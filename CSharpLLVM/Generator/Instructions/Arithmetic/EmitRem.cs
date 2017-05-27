using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compiler;
using CSharpLLVM.Helpers;
using CSharpLLVM.Stack;

namespace CSharpLLVM.Generator.Instructions.Arithmetic
{
    [InstructionHandler(Code.Rem, Code.Rem_Un)]
    class EmitRem : ICodeEmitter
    {
        /// <summary>
        /// Emits a rem instruction
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
                ValueRef result = LLVM.BuildFRem(builder, value1.Value, value2.Value, "remfp");
                context.CurrentStack.Push(new StackElement(result, value1.ILType, value1.Type));
            }
            else
            {
                CastHelper.HelpIntCast(builder, ref value1, ref value2);
                ValueRef result;

                if (instruction.OpCode.Code == Code.Rem)
                    result = LLVM.BuildSRem(builder, value1.Value, value2.Value, "remsi");
                else /* Div_Un */
                    result = LLVM.BuildURem(builder, value1.Value, value2.Value, "remui");

                context.CurrentStack.Push(new StackElement(result, value1.ILType, value1.Type));
            }
        }
    }
}
