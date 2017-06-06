using Swigged.LLVM;
using CSharpLLVM.Compilation;
using Mono.Cecil.Cil;
using CSharpLLVM.Stack;
using CSharpLLVM.Helpers;

namespace CSharpLLVM.Generator.Instructions.Stack
{
    [InstructionHandler(Code.Ldnull)]
    class EmitLdnull : ICodeEmitter
    {
        /// <summary>
        /// Emits a ldnull instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            context.CurrentStack.Push(new StackElement(LLVM.ConstNull(TypeHelper.VoidPtr), typeof(object).GetTypeReference(context.Compiler), TypeHelper.VoidPtr));
        }
    }
}
