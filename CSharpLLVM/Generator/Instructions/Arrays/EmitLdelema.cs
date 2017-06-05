using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compilation;
using CSharpLLVM.Stack;

namespace CSharpLLVM.Generator.Instructions.Arrays
{
    [InstructionHandler(Code.Ldelema)]
    class EmitLdelema : ICodeEmitter
    {
        /// <summary>
        /// Emits a ldelema instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            StackElement index = context.CurrentStack.Pop();
            StackElement array = context.CurrentStack.Pop();

            ValueRef gep = LLVM.BuildGEP(builder, array.Value, new ValueRef[] { index.Value }, "arrayelemptr");
            context.CurrentStack.Push(new StackElement(gep, array.ILType, array.Type));
        }
    }
}
