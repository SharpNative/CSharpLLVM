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
                CastHelper.HelpIntCast(builder, ref value1, ref value2);
                context.CurrentStack.Push(LLVM.BuildAdd(builder, value1.Value, value2.Value, "addi"));
            }
        }
    }
}
