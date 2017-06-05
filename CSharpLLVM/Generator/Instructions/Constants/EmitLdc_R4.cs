using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compilation;
using CSharpLLVM.Stack;
using CSharpLLVM.Helpers;

namespace CSharpLLVM.Generator.Instructions.Constants
{
    [InstructionHandler(Code.Ldc_R4)]
    class EmitLdc_R4 : ICodeEmitter
    {
        /// <summary>
        /// Emits a Ldc_R4 instruction
        /// </summary>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            ValueRef result = LLVM.ConstReal(TypeHelper.Float, (float)instruction.Operand);
            context.CurrentStack.Push(new StackElement(result, typeof(float), TypeHelper.Float));
        }
    }
}
