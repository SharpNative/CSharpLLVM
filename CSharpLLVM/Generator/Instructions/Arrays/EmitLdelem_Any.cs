using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compilation;
using CSharpLLVM.Stack;
using Mono.Cecil;
using CSharpLLVM.Helpers;

namespace CSharpLLVM.Generator.Instructions.Arrays
{
    [InstructionHandler(Code.Ldelem_Any)]
    class EmitLdelem_Any : ICodeEmitter
    {
        /// <summary>
        /// Emits a ldelem_any instruction.
        /// </summary>
        /// <param name="instruction">The instruction.</param>
        /// <param name="context">The context.</param>
        /// <param name="builder">The builder.</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            StackElement index = context.CurrentStack.Pop();
            StackElement array = context.CurrentStack.Pop();

            ValueRef ptr = LLVM.BuildGEP(builder, array.Value, new ValueRef[] { index.Value }, "arrayptr");
            ValueRef res = LLVM.BuildLoad(builder, ptr, "arrayelem");

            TypeReference dstType = (TypeReference)instruction.Operand;
            TypeRef type = TypeHelper.GetTypeRefFromType(dstType);
            ArrayType arrayType = (ArrayType)array.ILType;

            context.CurrentStack.Push(new StackElement(res, arrayType.ElementType, type));
        }
    }
}
