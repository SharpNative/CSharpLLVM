using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compilation;
using CSharpLLVM.Stack;
using CSharpLLVM.Helpers;
using Mono.Cecil;

namespace CSharpLLVM.Generator.Instructions.Objects
{
    [InstructionHandler(Code.Initobj)]
    class EmitInitobj : ICodeEmitter
    {
        /// <summary>
        /// Emits an initobj instruction.
        /// </summary>
        /// <param name="instruction">The instruction.</param>
        /// <param name="context">The context.</param>
        /// <param name="builder">The builder.</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            StackElement valueTypeAddress = context.CurrentStack.Pop();
            TypeReference type = (TypeReference)instruction.Operand;
            TypeRef typeRef = context.Compiler.Lookup.GetTypeRef(type);

            // We clear this using memset.
            CastHelper.HelpIntAndPtrCast(builder, ref valueTypeAddress.Value, ref valueTypeAddress.Type, TypeHelper.VoidPtr, "initobjcast");
            LLVM.BuildCall(builder, RuntimeHelper.Memset, new ValueRef[] { valueTypeAddress.Value, LLVM.ConstNull(TypeHelper.Int32), LLVM.SizeOf(typeRef) }, string.Empty);
        }
    }
}
