using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compiler;
using CSharpLLVM.Helpers;
using CSharpLLVM.Stack;

namespace CSharpLLVM.Generator.Instructions.Arithmetic
{
    [InstructionHandler(Code.Sub, Code.Sub_Ovf, Code.Sub_Ovf_Un)]
    class EmitSub : ICodeEmitter
    {
        /// <summary>
        /// Emits a sub instruction
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
                context.CurrentStack.Push(LLVM.BuildFSub(builder, value1.Value, value2.Value, "subfp"));
            }
            else
            {
                bool isPtrVal1, isPtrVal2;
                CastHelper.HelpPossiblePtrCast(builder, ref value1, ref value2, out isPtrVal1, out isPtrVal2);

                // If one of the two values is a pointer, then the result will be a pointer as well
                if (isPtrVal1 || isPtrVal2)
                {
                    ValueRef result = LLVM.BuildSub(builder, value1.Value, value2.Value, "subptr");
                    context.CurrentStack.Push(LLVM.BuildIntToPtr(builder, result, (isPtrVal1 ? value1.Type : value2.Type), "ptr"));
                }
                // Cast to different int size
                else
                {
                    CastHelper.HelpIntCast(builder, ref value1, ref value2);
                    context.CurrentStack.Push(LLVM.BuildSub(builder, value1.Value, value2.Value, "subi"));
                }
            }
        }
    }
}
