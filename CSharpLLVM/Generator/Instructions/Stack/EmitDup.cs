using Swigged.LLVM;
using CSharpLLVM.Compiler;
using Mono.Cecil.Cil;
using CSharpLLVM.Stack;

namespace CSharpLLVM.Generator.Instructions.Stack
{
    [InstructionHandler(Code.Dup)]
    class EmitDup : ICodeEmitter
    {
        /// <summary>
        /// Emits a dup instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            StackElement top = context.CurrentStack.Peek();
            context.CurrentStack.Push(new StackElement(top.Value));
        }
    }
}
