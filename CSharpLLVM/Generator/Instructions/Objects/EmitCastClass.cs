using Swigged.LLVM;
using Mono.Cecil;
using Mono.Cecil.Cil;
using CSharpLLVM.Compilation;
using CSharpLLVM.Stack;
using CSharpLLVM.Helpers;

namespace CSharpLLVM.Generator.Instructions.Objects
{
    [InstructionHandler(Code.Castclass)]
    class EmitCastclass : ICodeEmitter
    {
        /// <summary>
        /// Emits a castclass instruction.
        /// </summary>
        /// <param name="instruction">The instruction.</param>
        /// <param name="context">The context.</param>
        /// <param name="builder">The builder.</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            // Change type of top element.
            TypeDefinition dstType = (TypeDefinition)instruction.Operand;
            StackElement top = context.CurrentStack.Pop();

            TypeRef dstTypeRef = LLVM.PointerType(TypeHelper.GetTypeRefFromType(dstType), 0);
            top.ILType = new PointerType(dstType);
            if(TypeHelper.IsPointer(top))
            {
                top.Value = LLVM.BuildPointerCast(builder, top.Value, top.Type, "castclass");
            }
            else
            {
                top.Value = LLVM.BuildIntToPtr(builder, top.Value, top.Type, "castclass");
            }
            top.Type = dstTypeRef;

            context.CurrentStack.Push(top);
        }
    }
}
