using Swigged.LLVM;
using CSharpLLVM.Compiler;
using Mono.Cecil.Cil;

namespace CSharpLLVM.Generator.Instructions.Stack
{
    [InstructionHandler(Code.Pop)]
    class EmitPop : ICodeEmitter
    {
        /// <summary>
        /// Emits a pop instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            context.CurrentStack.Pop();
        }
    }
}
