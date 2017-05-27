using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compiler;
using System;
using Mono.Cecil;

namespace CSharpLLVM.Generator.Instructions.StoreLoad
{
    [InstructionHandler(Code.Ldsfld)]
    class EmitLdsfld : ICodeEmitter
    {
        /// <summary>
        /// Emits a ldsfld instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            FieldReference field = (FieldReference)instruction.Operand;
            ValueRef? fieldValue = context.Compiler.GetStaticField(field);
            if (fieldValue == null)
                throw new InvalidOperationException("Unknown static field: " + field);
            
            ValueRef value = LLVM.BuildLoad(builder, fieldValue.Value, "ldsfld");
            context.CurrentStack.Push(value);
        }
    }
}
