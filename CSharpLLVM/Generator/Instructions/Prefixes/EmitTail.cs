using Swigged.LLVM;
using CSharpLLVM.Compilation;
using Mono.Cecil.Cil;

namespace CSharpLLVM.Generator.Instructions.Prefixes
{
    [InstructionHandler(Code.Tail)]
    class EmitTail : ICodeEmitter
    {
        /// <summary>
        /// Emits a tail instruction.
        /// </summary>
        /// <param name="instruction">The instruction.</param>
        /// <param name="context">The context.</param>
        /// <param name="builder">The builder.</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            // This is actually handled in the opcodes call and callvirt.
        }
    }
}
