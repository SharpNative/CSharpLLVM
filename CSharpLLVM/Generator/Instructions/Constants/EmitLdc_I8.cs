using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compiler;
using CSharpLLVM.Helpers;

namespace CSharpLLVM.Generator.Instructions.Constants
{
    [InstructionHandler(Code.Ldc_I8)]
    class EmitLdc_I8 : ICodeEmitter
    {
        /// <summary>
        /// Emits a Ldc_I8 instruction
        /// </summary>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            TypeRef type = TypeHelper.Int64;
            unchecked
            {
                context.CurrentStack.Push(LLVM.ConstInt(type, (ulong)(long)instruction.Operand, true));
            }
        }
    }
}
