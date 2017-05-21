using CSharpLLVM.Compiler;
using Swigged.LLVM;
using Mono.Cecil.Cil;

namespace CSharpLLVM.Generator
{
    interface ICodeEmitter
    {
        /// <summary>
        /// Emits an instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        void Emit(Instruction instruction, MethodContext context, BuilderRef builder);
    }
}
