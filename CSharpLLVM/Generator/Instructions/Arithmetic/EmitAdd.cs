using Swigged.LLVM;
using Mono.Cecil;
using Mono.Cecil.Cil;
using CSharpLLVM.Helpers;
using CSharpLLVM.Stack;
using CSharpLLVM.Compilation;

namespace CSharpLLVM.Generator.Instructions.Arithmetic
{
    [InstructionHandler(Code.Add, Code.Add_Ovf, Code.Add_Ovf_Un)]
    class EmitAdd : ICodeEmitter
    {
        /// <summary>
        /// Emits an add instruction.
        /// </summary>
        /// <param name="instruction">The instruction.</param>
        /// <param name="context">The context.</param>
        /// <param name="builder">The builder.</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            StackElement value2 = context.CurrentStack.Pop();
            StackElement value1 = context.CurrentStack.Pop();
            
            if (TypeHelper.IsFloatingPoint(value1) || TypeHelper.IsFloatingPoint(value2))
            {
                ValueRef result = LLVM.BuildFAdd(builder, value1.Value, value2.Value, "addfp");
                context.CurrentStack.Push(new StackElement(result, value1.ILType, value1.Type));
            }
            else
            {
                bool isPtrVal1, isPtrVal2;
                CastHelper.HelpPossiblePtrCast(builder, ref value1, ref value2, out isPtrVal1, out isPtrVal2, "addcast");

                // If one of the two values is a pointer, then the result will be a pointer as well.
                if (isPtrVal1 || isPtrVal2)
                {
                    ValueRef result = LLVM.BuildAdd(builder, value1.Value, value2.Value, "addptr");
                    TypeRef resultingType = (isPtrVal1 ? value1.Type : value2.Type);
                    TypeReference resultingILType = (isPtrVal1 ? value1.ILType : value2.ILType);
                    ValueRef ptr = LLVM.BuildIntToPtr(builder, result, resultingType, "ptr");
                    context.CurrentStack.Push(new StackElement(ptr, resultingILType, resultingType));
                }
                // Cast to different int size.
                else
                {
                    CastHelper.HelpIntCast(builder, ref value1, ref value2);
                    ValueRef result = LLVM.BuildAdd(builder, value1.Value, value2.Value, "addi");
                    context.CurrentStack.Push(new StackElement(result, value1.ILType, value1.Type));
                }
            }
        }
    }
}
