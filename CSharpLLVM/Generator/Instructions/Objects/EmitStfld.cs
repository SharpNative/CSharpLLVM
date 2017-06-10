using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compilation;
using Mono.Cecil;
using CSharpLLVM.Stack;
using CSharpLLVM.Helpers;

namespace CSharpLLVM.Generator.Instructions.Objects
{
    [InstructionHandler(Code.Stfld)]
    class EmitStfld : ICodeEmitter
    {
        /// <summary>
        /// Emits a stfld instruction.
        /// </summary>
        /// <param name="instruction">The instruction.</param>
        /// <param name="context">The context.</param>
        /// <param name="builder">The builder.</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            StackElement value = context.CurrentStack.Pop();
            StackElement obj = context.CurrentStack.Pop();
            FieldReference field = (FieldReference)instruction.Operand;

            uint index = context.Compiler.Lookup.GetFieldIndex(field);
            
            ValueRef ptr = LLVM.BuildInBoundsGEP(builder, obj.Value, new ValueRef[] { LLVM.ConstInt(TypeHelper.Int32, 0, false), LLVM.ConstInt(TypeHelper.Int32, index, false) }, "field");

            // Possible cast needed.
            TypeRef destType = TypeHelper.GetTypeRefFromType(field.FieldType);
            if (value.Type != destType)
                CastHelper.HelpIntAndPtrCast(builder, ref value.Value, value.Type, destType);

            ValueRef store = LLVM.BuildStore(builder, value.Value, ptr);
            if (instruction.HasPrefix(Code.Volatile))
                LLVM.SetVolatile(store, true);
        }
    }
}
