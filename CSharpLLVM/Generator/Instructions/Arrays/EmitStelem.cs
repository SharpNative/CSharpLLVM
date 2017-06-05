using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compilation;
using CSharpLLVM.Stack;
using CSharpLLVM.Helpers;

namespace CSharpLLVM.Generator.Instructions.Arrays
{
    [InstructionHandler(Code.Stelem_I, Code.Stelem_I1, Code.Stelem_I2, Code.Stelem_I4, Code.Stelem_I8, Code.Stelem_R4, Code.Stelem_R8)]
    class EmitStelem : ICodeEmitter
    {
        /// <summary>
        /// Emits a stelem instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            StackElement value = context.CurrentStack.Pop();
            StackElement index = context.CurrentStack.Pop();
            StackElement array = context.CurrentStack.Pop();
            
            // Convert to "pointer to value type" type
            if (array.Type == TypeHelper.VoidPtr)
            {
                TypeRef destType = TypeHelper.GetTypeRefFromStelem(instruction.OpCode.Code);
                TypeRef ptrType = LLVM.PointerType(destType, 0);
                array.Value = LLVM.BuildPointerCast(builder, array.Value, ptrType, "tmp");
                array.Type = ptrType;
            }

            TypeRef elementType = TypeHelper.GetTypeRefFromType(array.ILType.GetElementType());

            ValueRef ptr = LLVM.BuildGEP(builder, array.Value, new ValueRef[] { index.Value }, "arrayptr");
            CastHelper.HelpIntAndPtrCast(builder, ref value.Value, value.Type, elementType);
            LLVM.BuildStore(builder, value.Value, ptr);
        }
    }
}
