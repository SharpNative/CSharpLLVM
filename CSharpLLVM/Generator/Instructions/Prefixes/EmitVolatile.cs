using Swigged.LLVM;
using CSharpLLVM.Compilation;
using Mono.Cecil.Cil;

namespace CSharpLLVM.Generator.Instructions.Prefixes
{
    [InstructionHandler(Code.Volatile)]
    class EmitVolatile : ICodeEmitter
    {
        /// <summary>
        /// Emits a volatile instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            // This is actually handled in the opcodes for store and load
        }
    }
}
