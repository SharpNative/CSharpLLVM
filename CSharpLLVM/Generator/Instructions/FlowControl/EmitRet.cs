using Swigged.LLVM;
using Mono.Cecil;
using Mono.Cecil.Cil;
using CSharpLLVM.Compilation;
using CSharpLLVM.Stack;

namespace CSharpLLVM.Generator.Instructions.FlowControl
{
    [InstructionHandler(Code.Ret)]
    class EmitRet : ICodeEmitter
    {
        /// <summary>
        /// Emits a ret instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            if (context.Method.ReturnType.MetadataType == MetadataType.Void)
            {
                LLVM.BuildRetVoid(builder);
            }
            else
            {
                StackElement element = context.CurrentStack.Pop();
                LLVM.BuildRet(builder, element.Value);
            }
        }
    }
}
