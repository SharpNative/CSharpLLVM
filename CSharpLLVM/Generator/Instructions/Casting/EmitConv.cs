using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compilation;
using CSharpLLVM.Helpers;
using CSharpLLVM.Stack;

namespace CSharpLLVM.Generator.Instructions.Casting
{
    [InstructionHandler(Code.Conv_I, Code.Conv_Ovf_I, Code.Conv_Ovf_I_Un, Code.Conv_I1, Code.Conv_Ovf_I1, Code.Conv_Ovf_I1_Un, Code.Conv_I2, Code.Conv_Ovf_I2, Code.Conv_Ovf_I2_Un, Code.Conv_I4, Code.Conv_Ovf_I4, Code.Conv_Ovf_I4_Un, Code.Conv_I8, Code.Conv_Ovf_I8, Code.Conv_Ovf_I8_Un, Code.Conv_U, Code.Conv_U1, Code.Conv_U2, Code.Conv_U4, Code.Conv_U8, Code.Conv_R4, Code.Conv_R8)]
    class EmitConv : ICodeEmitter
    {
        /// <summary>
        /// Emits a Conv instruction
        /// </summary>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            StackElement element = context.CurrentStack.Pop();
            ValueRef result;

            TypeRef destType = TypeHelper.GetTypeRefFromConv(instruction.OpCode.Code);
            if (TypeHelper.IsFloatingPoint(element))
            {
                if (instruction.OpCode.Code == Code.Conv_R4 || instruction.OpCode.Code == Code.Conv_R8)
                    result = LLVM.BuildFPCast(builder, element.Value, destType, "fp2fp");
                else
                    result = LLVM.BuildFPToSI(builder, element.Value, destType, "fp2int");
            }
            else if (TypeHelper.IsPointer(element))
            {
                result = LLVM.BuildPtrToInt(builder, element.Value, destType, "int2ptr");
            }
            else
            {
                if (instruction.OpCode.Code == Code.Conv_R4 || instruction.OpCode.Code == Code.Conv_R8)
                    result = LLVM.BuildSIToFP(builder, element.Value, destType, "int2fp");
                else
                    result = LLVM.BuildIntCast(builder, element.Value, destType, "int2int");
            }

            context.CurrentStack.Push(new StackElement(result, TypeHelper.GetBasicTypeFromTypeRef(context.Compiler, destType), destType));
        }
    }
}
