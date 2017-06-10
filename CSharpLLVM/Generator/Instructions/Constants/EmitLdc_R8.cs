using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compilation;
using CSharpLLVM.Stack;
using CSharpLLVM.Helpers;

namespace CSharpLLVM.Generator.Instructions.Constants
{
    [InstructionHandler(Code.Ldc_R8)]
    class EmitLdc_R8 : ICodeEmitter
    {
        /// <summary>
        /// Emits a Ldc_R8 instruction
        /// </summary>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            ValueRef result = LLVM.ConstReal(TypeHelper.Double, (double)instruction.Operand);
            context.CurrentStack.Push(new StackElement(result, typeof(double).GetTypeReference(), TypeHelper.Double));
        }
    }
}
