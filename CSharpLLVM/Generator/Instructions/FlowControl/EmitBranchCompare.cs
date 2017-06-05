using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compilation;
using CSharpLLVM.Helpers;
using CSharpLLVM.Stack;

namespace CSharpLLVM.Generator.Instructions.FlowControl
{
    [InstructionHandler(Code.Beq, Code.Beq_S, Code.Bge, Code.Bge_S, Code.Bge_Un, Code.Bge_Un_S, Code.Bgt, Code.Bgt_S, Code.Bgt_Un, Code.Bgt_Un_S, Code.Ble, Code.Ble_S, Code.Ble_Un, Code.Ble_Un_S, Code.Blt, Code.Blt_S, Code.Blt_Un, Code.Blt_Un_S, Code.Bne_Un, Code.Bne_Un_S)]
    class EmitBranchCompare : ICodeEmitter
    {
        /// <summary>
        /// Emits a branch on compare instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            StackElement value2 = context.CurrentStack.Pop();
            StackElement value1 = context.CurrentStack.Pop();
            ValueRef ret;

            if (TypeHelper.IsFloatingPoint(value2) || TypeHelper.IsFloatingPoint(value1))
            {
                ret = LLVM.BuildFCmp(builder, PredicateHelper.GetRealPredicateFromCode(instruction.OpCode.Code), value1.Value, value2.Value, "fbrcmp");
            }
            else
            {
                ret = LLVM.BuildICmp(builder, PredicateHelper.GetIntPredicateFromCode(instruction.OpCode.Code), value1.Value, value2.Value, "ibrcmp");
            }

            Instruction onFalse = instruction.Next;
            Instruction onTrue = (Instruction)instruction.Operand;
            LLVM.BuildCondBr(builder, ret, context.GetBlockOf(onTrue), context.GetBlockOf(onFalse));
        }
    }
}
