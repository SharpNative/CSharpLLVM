using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compiler;
using CSharpLLVM.Stack;

namespace CSharpLLVM.Generator.Instructions.BitwiseOps
{
    [InstructionHandler(Code.And)]
    class EmitAnd : ICodeEmitter
    {
        /// <summary>
        /// Emits an and instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            StackElement value2 = context.CurrentStack.Pop();
            StackElement value1 = context.CurrentStack.Pop();
            context.CurrentStack.Push(LLVM.BuildAnd(builder, value1.Value, value2.Value, "and"));
        }
    }
}
