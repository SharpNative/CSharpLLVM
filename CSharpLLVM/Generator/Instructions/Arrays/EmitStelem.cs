using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compiler;
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

            ValueRef val = value.Value;
            TypeRef destType = TypeHelper.GetTypeRefFromStelem(instruction.OpCode.Code);
            if (LLVM.PointerType(destType, 0) != array.Type)
            {
                // There is no instruction for a stelem to store a boolean, so we need to check manually
                if (array.Type == LLVM.PointerType(TypeHelper.Boolean, 0))
                {
                    destType = TypeHelper.Boolean;
                }
                // We treat char as 8-bit
                if (array.Type == LLVM.PointerType(TypeHelper.Int8, 0))
                {
                    destType = TypeHelper.Int8;
                }

                val = LLVM.BuildIntCast(builder, val, destType, "stelemcast");
            }

            ValueRef ptr = LLVM.BuildGEP(builder, array.Value, new ValueRef[] { index.Value }, "arrayptr");
            LLVM.BuildStore(builder, val, ptr);
        }
    }
}
