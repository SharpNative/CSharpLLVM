using Swigged.LLVM;
using Mono.Cecil;
using Mono.Cecil.Cil;
using CSharpLLVM.Compilation;
using CSharpLLVM.Stack;
using CSharpLLVM.Helpers;

namespace CSharpLLVM.Generator.Instructions.FlowControl
{
    [InstructionHandler(Code.Ret)]
    class EmitRet : ICodeEmitter
    {
        /// <summary>
        /// Emits a ret instruction.
        /// </summary>
        /// <param name="instruction">The instruction.</param>
        /// <param name="context">The context.</param>
        /// <param name="builder">The builder.</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            TypeReference returnType = context.Method.ReturnType;
            if (returnType.MetadataType == MetadataType.Void)
            {
                LLVM.BuildRetVoid(builder);
            }
            else
            {
                StackElement element = context.CurrentStack.Pop();
                TypeRef returnTypeRef = TypeHelper.GetTypeRefFromType(returnType);

                if (element.Type != returnTypeRef)
                {
                    CastHelper.HelpIntAndPtrCast(builder, ref element.Value, element.Type, returnTypeRef);
                }

                LLVM.BuildRet(builder, element.Value);
            }
        }
    }
}
