using Swigged.LLVM;
using CSharpLLVM.Compilation;
using Mono.Cecil.Cil;

namespace CSharpLLVM.Generator.Instructions.FlowControl
{
    [InstructionHandler(Code.Br, Code.Br_S)]
    class EmitBr : ICodeEmitter
    {
        /// <summary>
        /// Emits a br instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            Instruction dest = (Instruction)instruction.Operand;
            LLVM.BuildBr(builder, context.GetBlockOf(dest));
        }
    }
}
