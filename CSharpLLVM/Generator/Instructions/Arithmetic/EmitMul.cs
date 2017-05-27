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
                ValueRef result = LLVM.BuildFMul(builder, value1.Value, value2.Value, "mulfp");
                context.CurrentStack.Push(new StackElement(result, value1.ILType, value1.Type));
            }
            else
            {
                CastHelper.HelpIntCast(builder, ref value1, ref value2);
                ValueRef result = LLVM.BuildMul(builder, value1.Value, value2.Value, "muli");
                context.CurrentStack.Push(new StackElement(result, value1.ILType, value1.Type));
            }
        }
    }
}
