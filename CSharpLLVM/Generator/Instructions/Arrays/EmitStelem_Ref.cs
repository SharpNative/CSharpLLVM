using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compiler;
using CSharpLLVM.Stack;
using CSharpLLVM.Helpers;

namespace CSharpLLVM.Generator.Instructions.Arrays
{
    [InstructionHandler(Code.Stelem_Ref)]
    class EmitStelemRef : ICodeEmitter
    {
        /// <summary>
        /// Emits a stelemref instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            StackElement value = context.CurrentStack.Pop();
            StackElement index = context.CurrentStack.Pop();
            StackElement array = context.CurrentStack.Pop();
            
            TypeRef destType = LLVM.PointerType(value.Type, 0);

            // Convert to "pointer to value type" type
            if (array.Type == TypeHelper.VoidPtr || destType != array.Type)
            {
                array.Value = LLVM.BuildPointerCast(builder, array.Value, destType, "tmp");
            }
            
            ValueRef ptr = LLVM.BuildGEP(builder, array.Value, new ValueRef[] { index.Value }, "arrayptr");
            LLVM.BuildStore(builder, value.Value, ptr);
        }
    }
}
