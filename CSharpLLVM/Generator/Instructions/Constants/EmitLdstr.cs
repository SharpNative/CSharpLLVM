using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compilation;
using CSharpLLVM.Helpers;
using CSharpLLVM.Stack;

namespace CSharpLLVM.Generator.Instructions.Constants
{
    [InstructionHandler(Code.Ldstr)]
    class EmitLdstr : ICodeEmitter
    {
        /// <summary>
        /// Emits a ldstr instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            string str = (string)instruction.Operand;

            // Add as global
            ValueRef strValue = LLVM.BuildGlobalString(builder, str, string.Empty);

            // We need to cast the string
            ValueRef value = LLVM.ConstPointerCast(strValue, TypeHelper.String);

            context.CurrentStack.Push(new StackElement(value, typeof(string).GetTypeReference(), TypeHelper.String));
        }
    }
}
