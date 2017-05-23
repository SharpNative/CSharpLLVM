using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compiler;
using CSharpLLVM.Helpers;
using CSharpLLVM.Stack;

namespace CSharpLLVM.Generator.Instructions.FlowControl
{
    [InstructionHandler(Code.Brtrue_S, Code.Brtrue, Code.Brfalse_S, Code.Brfalse)]
    class EmitBrCondition : ICodeEmitter
    {
        /// <summary>
        /// Emits a br on condition instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            StackElement element = context.CurrentStack.Pop();
            ValueRef ret = LLVM.BuildICmp(builder, PredicateHelper.GetIntPredicateFromCode(instruction.OpCode.Code), element.Value, TypeHelper.True, "brcond");
            Instruction onFalse = instruction.Next;
            Instruction onTrue = (Instruction)instruction.Operand;
            LLVM.BuildCondBr(builder, ret, context.GetBlockOf(onTrue), context.GetBlockOf(onFalse));
        }
    }
}
