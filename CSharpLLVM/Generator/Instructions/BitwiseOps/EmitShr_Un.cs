using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compilation;
using CSharpLLVM.Stack;
using CSharpLLVM.Helpers;

namespace CSharpLLVM.Generator.Instructions.BitwiseOps
{
    [InstructionHandler(Code.Shr_Un)]
    class EmitShr_Un : ICodeEmitter
    {
        /// <summary>
        /// Emits an shr_un instruction.
        /// </summary>
        /// <param name="instruction">The instruction.</param>
        /// <param name="context">The context.</param>
        /// <param name="builder">The builder.</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            StackElement value2 = context.CurrentStack.Pop();
            StackElement value1 = context.CurrentStack.Pop();
            CastHelper.HelpIntCast(builder, ref value1, ref value2);
            ValueRef result = LLVM.BuildLShr(builder, value1.Value, value2.Value, "shrun");
            context.CurrentStack.Push(new StackElement(result, value1.ILType, value1.Type));
        }
    }
}
