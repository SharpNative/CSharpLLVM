using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compiler;
using CSharpLLVM.Stack;

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
            TypeRef type = LLVM.DoubleType();
            context.CurrentStack.Push(new StackElement(LLVM.ConstReal(type, (double)instruction.Operand)));
        }
    }
}
