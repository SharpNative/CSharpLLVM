using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compiler;
using CSharpLLVM.Stack;
using CSharpLLVM.Helpers;
using Mono.Cecil;

namespace CSharpLLVM.Generator.Instructions.Arrays
{
    [InstructionHandler(Code.Newarr)]
    class EmitNewArr : ICodeEmitter
    {
        /// <summary>
        /// Emits a newarr instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            StackElement countElement = context.CurrentStack.Pop();
            TypeReference type = (TypeReference)instruction.Operand;
            TypeRef typeRef = TypeHelper.GetTypeRefFromType(type);

            // Might need to cast the count (amount of elements)
            ValueRef count = countElement.Value;
            if (countElement.Type != TypeHelper.Int32)
            {
                count = LLVM.BuildIntCast(builder, count, TypeHelper.Int32, "count");
            }

            // Size of one element
            ulong size = LLVM.SizeOfTypeInBits(context.Compiler.TargetData, typeRef) / 8;
            ValueRef sizeValue = LLVM.ConstInt(TypeHelper.NativeIntType, size, false);

            context.CurrentStack.Push(LLVM.BuildCall(builder, RuntimeHelper.Newarr, new ValueRef[] { count, sizeValue }, "array"));
        }
    }
}
