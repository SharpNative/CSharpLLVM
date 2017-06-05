using Swigged.LLVM;
using CSharpLLVM.Compilation;
using Mono.Cecil.Cil;

namespace CSharpLLVM.Generator.Instructions.Misc
{
    [InstructionHandler(Code.Nop)]
    class EmitNop : ICodeEmitter
    {
        /// <summary>
        /// Emits a nop instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            // Ignore
        }
    }
}
