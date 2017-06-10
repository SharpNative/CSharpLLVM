using Swigged.LLVM;
using Mono.Cecil;
using Mono.Cecil.Cil;
using CSharpLLVM.Compilation;
using CSharpLLVM.Stack;

namespace CSharpLLVM.Generator.Instructions.StoreLoad
{
    [InstructionHandler(Code.Ldarga, Code.Ldarga_S)]
    class EmitLdarga : ICodeEmitter
    {
        /// <summary>
        /// Emits a ldarga instruction.
        /// </summary>
        /// <param name="instruction">The instruction.</param>
        /// <param name="context">The context.</param>
        /// <param name="builder">The builder.</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            ParameterDefinition def = (ParameterDefinition)instruction.Operand;
            int index = def.Index;
            if (def.Method.HasThis)
                index++;

            ValueRef arg = context.ArgumentValues[index];
            context.CurrentStack.Push(new StackElement(arg, context.ArgumentILTypes[index]));
        }
    }
}
