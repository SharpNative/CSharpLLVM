using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compiler;

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
            TypeRef type = LLVM.PointerType(LLVM.Int8Type(), 0);
            ValueRef value = LLVM.ConstPointerCast(strValue, type);

            context.CurrentStack.Push(value);
        }
    }
}
