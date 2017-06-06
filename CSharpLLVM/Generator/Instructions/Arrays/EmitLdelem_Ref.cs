using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compilation;
using CSharpLLVM.Stack;
using Mono.Cecil;

namespace CSharpLLVM.Generator.Instructions.Arrays
{
    [InstructionHandler(Code.Ldelem_Ref)]
    class EmitLdelem_Ref : ICodeEmitter
    {
        /// <summary>
        /// Emits a ldelem_ref instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            StackElement index = context.CurrentStack.Pop();
            StackElement array = context.CurrentStack.Pop();

            ValueRef ptr = LLVM.BuildGEP(builder, array.Value, new ValueRef[] { index.Value }, "arrayptr");
            ValueRef res = LLVM.BuildLoad(builder, ptr, "arrayelem");

            TypeRef type = LLVM.TypeOf(res);
            TypeReference elementType = null;
            if (array.ILType.IsPointer)
            {
                elementType = ((PointerType)array.ILType).ElementType;
            }
            else
            {
                elementType = ((ArrayType)array.ILType).ElementType;
            }

            context.CurrentStack.Push(new StackElement(res, elementType, type));
        }
    }
}
