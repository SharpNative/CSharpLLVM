using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compiler;
using CSharpLLVM.Stack;
using CSharpLLVM.Helpers;

namespace CSharpLLVM.Generator.Instructions.Arrays
{
    [InstructionHandler(Code.Stelem_Ref)]
    class EmitStelem_Ref : ICodeEmitter
    {
        /// <summary>
        /// Emits a stelem_ref instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            StackElement value = context.CurrentStack.Pop();
            StackElement index = context.CurrentStack.Pop();
            StackElement array = context.CurrentStack.Pop();

            ValueRef val = value.Value;
            TypeRef destType = LLVM.GetElementType(array.Type);
            if (destType != array.Type)
            {
                val = LLVM.BuildPointerCast(builder, val, destType, "ptrcast");
            }

            ValueRef ptr = LLVM.BuildGEP(builder, array.Value, new ValueRef[] { index.Value }, "arrayptr");
            LLVM.BuildStore(builder, val, ptr);
        }
    }
}
