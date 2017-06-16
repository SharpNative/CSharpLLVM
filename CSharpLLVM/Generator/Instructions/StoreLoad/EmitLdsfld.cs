using Swigged.LLVM;
using Mono.Cecil.Cil;
using Mono.Cecil;
using CSharpLLVM.Compilation;
using CSharpLLVM.Stack;
using CSharpLLVM.Helpers;
using System;

namespace CSharpLLVM.Generator.Instructions.StoreLoad
{
    [InstructionHandler(Code.Ldsfld)]
    class EmitLdsfld : ICodeEmitter
    {
        /// <summary>
        /// Emits a ldsfld instruction.
        /// </summary>
        /// <param name="instruction">The instruction.</param>
        /// <param name="context">The context.</param>
        /// <param name="builder">The builder.</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            FieldReference field = (FieldReference)instruction.Operand;
            ValueRef fieldValue = context.Compiler.Lookup.GetStaticField(field);
            
            ValueRef result = LLVM.BuildLoad(builder, fieldValue, "ldsfld");
            if (instruction.HasPrefix(Code.Volatile))
                LLVM.SetVolatile(result, true);

            TypeRef resultType = LLVM.TypeOf(result);
            context.CurrentStack.Push(new StackElement(result, field.FieldType, resultType));
        }
    }
}
