using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compilation;
using Mono.Cecil;
using CSharpLLVM.Stack;
using CSharpLLVM.Helpers;

namespace CSharpLLVM.Generator.Instructions.Objects
{
    [InstructionHandler(Code.Ldfld)]
    class EmitLdfld : ICodeEmitter
    {
        /// <summary>
        /// Emits a ldfld instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            StackElement obj = context.CurrentStack.Pop();
            FieldReference field = (FieldReference)instruction.Operand;

            uint index = context.Compiler.Lookup.GetFieldIndex(field);

            // Create pointer if not yet a pointer
            if (obj.ILType.IsValueType && !obj.ILType.IsPointer)
            {
                ValueRef objPtr = LLVM.BuildAlloca(builder, obj.Type, "objptr");
                LLVM.BuildStore(builder, obj.Value, objPtr);
                obj.Value = objPtr;
            }

            ValueRef ptr = LLVM.BuildInBoundsGEP(builder, obj.Value, new ValueRef[] { LLVM.ConstInt(TypeHelper.Int32, 0, false), LLVM.ConstInt(TypeHelper.Int32, index, false) }, "field");
            ValueRef result = LLVM.BuildLoad(builder, ptr, "field");
            if (instruction.HasPrefix(Code.Volatile))
                LLVM.SetVolatile(result, true);

            context.CurrentStack.Push(new StackElement(result, field.FieldType));
        }
    }
}
