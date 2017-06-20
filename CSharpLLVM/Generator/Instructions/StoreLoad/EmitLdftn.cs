using Swigged.LLVM;
using Mono.Cecil;
using Mono.Cecil.Cil;
using CSharpLLVM.Compilation;
using CSharpLLVM.Stack;
using CSharpLLVM.Helpers;
using System;

namespace CSharpLLVM.Generator.Instructions.StoreLoad
{
    [InstructionHandler(Code.Ldftn)]
    class EmitLdftn : ICodeEmitter
    {
        /// <summary>
        /// Emits a ldftn instruction.
        /// </summary>
        /// <param name="instruction">The instruction.</param>
        /// <param name="context">The context.</param>
        /// <param name="builder">The builder.</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            MethodDefinition method = (MethodDefinition)instruction.Operand;
            ValueRef result = LLVM.BuildIntToPtr(builder, context.Compiler.Lookup.GetFunction(NameHelper.CreateMethodName(method)).Value, TypeHelper.NativeIntType, "ldftn");
            StackElement element = new StackElement(result, typeof(IntPtr).GetTypeReference(), TypeHelper.NativeIntType);
            context.CurrentStack.Push(element);
        }
    }
}
