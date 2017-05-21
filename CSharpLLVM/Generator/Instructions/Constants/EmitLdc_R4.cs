using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compiler;
using CSharpLLVM.Stack;

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
            TypeRef type = LLVM.FloatType();
            context.CurrentStack.Push(new StackElement(LLVM.ConstReal(type, (float)instruction.Operand)));
        }
    }
}
