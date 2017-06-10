using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compilation;
using CSharpLLVM.Helpers;
using CSharpLLVM.Stack;

namespace CSharpLLVM.Generator.Instructions.Comparison
{
    [InstructionHandler(Code.Clt, Code.Clt_Un, Code.Ceq, Code.Cgt, Code.Cgt_Un)]
    class EmitCompare : ICodeEmitter
    {
        /// <summary>
        /// Emits a compare instruction
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
                ret = LLVM.BuildFCmp(builder, PredicateHelper.GetRealPredicateFromCode(instruction.OpCode.Code), value1.Value, value2.Value, "fcmp");
            }
            else
            {
                if (value2.Type != value1.Type)
                {
                    ValueRef tmp = LLVM.BuildIntCast(builder, value2.Value, value1.Type, "tmp");
                    ret = LLVM.BuildICmp(builder, PredicateHelper.GetIntPredicateFromCode(instruction.OpCode.Code), value1.Value, tmp, "icmp");
                }
                else
                {
                    ret = LLVM.BuildICmp(builder, PredicateHelper.GetIntPredicateFromCode(instruction.OpCode.Code), value1.Value, value2.Value, "icmp");
                }
            }
            
            context.CurrentStack.Push(new StackElement(ret, typeof(bool).GetTypeReference(), TypeHelper.Boolean));
        }
    }
}
