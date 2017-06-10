using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compilation;
using CSharpLLVM.Stack;
using CSharpLLVM.Helpers;

namespace CSharpLLVM.Generator.Instructions.Arithmetic
{
    [InstructionHandler(Code.Neg)]
    class EmitNeg : ICodeEmitter
    {
        /// <summary>
        /// Emits a neg instruction.
        /// </summary>
        /// <param name="instruction">The instruction.</param>
        /// <param name="context">The context.</param>
        /// <param name="builder">The builder.</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            StackElement value = context.CurrentStack.Pop();
            ValueRef result;

            if (TypeHelper.IsFloatingPoint(value))
                result = LLVM.BuildFNeg(builder, value.Value, "fneg");
            else
                result = LLVM.BuildNeg(builder, value.Value, "neg");

            context.CurrentStack.Push(new StackElement(result, value.ILType, value.Type));
        }
    }
}
