using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compiler;
using CSharpLLVM.Stack;
using CSharpLLVM.Helpers;

namespace CSharpLLVM.Generator.Instructions.Arrays
{
    [InstructionHandler(Code.Localloc)]
    class EmitLocalloc : ICodeEmitter
    {
        /// <summary>
        /// Emits a localloc instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            StackElement size = context.CurrentStack.Pop();
            ValueRef result = LLVM.BuildArrayAlloca(builder, TypeHelper.Int8, size.Value, "stackalloc");
            context.CurrentStack.Push(new StackElement(result, typeof(byte[])));
        }
    }
}
