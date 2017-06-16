using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compilation;

namespace CSharpLLVM.Generator.Instructions.StoreLoad
{
    [InstructionHandler(Code.Ldftn)]
    class EmitLdftn : ICodeEmitter
    {
        /// <summary>
        /// Emits a ldftn instruction.
        /// </summary>
        /// <param name="instruction">The instruction.</param>
        /// <param name="context">The context.</param>
        /// <param name="builder">The builder.</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            // TODO.
        }
    }
}
