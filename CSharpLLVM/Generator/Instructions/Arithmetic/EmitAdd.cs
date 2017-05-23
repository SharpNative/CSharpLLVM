using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compiler;
using CSharpLLVM.Helpers;
using CSharpLLVM.Stack;

namespace CSharpLLVM.Generator.Instructions.Arithmetic
{
    [InstructionHandler(Code.Add, Code.Add_Ovf, Code.Add_Ovf_Un)]
    class EmitAdd : ICodeEmitter
    {
        /// <summary>
        /// Emits an add instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            StackElement value2 = context.CurrentStack.Pop();
            StackElement value1 = context.CurrentStack.Pop();

            if (TypeHelper.IsFloatingPoint(value1) || TypeHelper.IsFloatingPoint(value2))
            {
                context.CurrentStack.Push(LLVM.BuildFAdd(builder, value1.Value, value2.Value, "addfp"));
            }
            else
            {
                ValueRef val1 = value1.Value;
                ValueRef val2 = value2.Value;

                if (value1.Type != value2.Type)
                {
                    val1 = LLVM.BuildIntCast(builder, val1, value2.Type, "addcast");
                }

                context.CurrentStack.Push(LLVM.BuildAdd(builder, val1, val2, "addi"));
            }
        }
    }
}
