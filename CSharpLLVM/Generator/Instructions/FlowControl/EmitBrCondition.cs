using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compilation;
using CSharpLLVM.Helpers;
using CSharpLLVM.Stack;

namespace CSharpLLVM.Generator.Instructions.FlowControl
{
    [InstructionHandler(Code.Brtrue_S, Code.Brtrue, Code.Brfalse_S, Code.Brfalse)]
    class EmitBrCondition : ICodeEmitter
    {
        /// <summary>
        /// Emits a branch on condition instruction.
        /// </summary>
        /// <param name="instruction">The instruction.</param>
        /// <param name="context">The context.</param>
        /// <param name="builder">The builder.</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            StackElement element = context.CurrentStack.Pop();

            // Note: a zero corresponds to false, but every other value corresponds to true.
            Code code = instruction.OpCode.Code;
            IntPredicate predicate = ((code == Code.Brtrue || code == Code.Brtrue_S) ? IntPredicate.IntNE : IntPredicate.IntEQ);
            ValueRef ret;

            if (TypeHelper.IsPointer(element))
            {
                ValueRef tmp = LLVM.BuildPtrToInt(builder, element.Value, TypeHelper.NativeIntType, "ptr2int");
                ret = LLVM.BuildICmp(builder, predicate, tmp, LLVM.ConstInt(TypeHelper.NativeIntType, 0, false), "brcond");
            }
            else
            {
                ret = LLVM.BuildICmp(builder, predicate, element.Value, LLVM.ConstInt(element.Type, 0, false), "brcond");
            }

            Instruction onFalse = instruction.Next;
            Instruction onTrue = (Instruction)instruction.Operand;
            LLVM.BuildCondBr(builder, ret, context.GetBlockOf(onTrue), context.GetBlockOf(onFalse));
        }
    }
}
