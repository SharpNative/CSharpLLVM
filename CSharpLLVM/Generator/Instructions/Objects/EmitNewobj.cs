using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compiler;
using CSharpLLVM.Stack;
using CSharpLLVM.Helpers;
using Mono.Cecil;

namespace CSharpLLVM.Generator.Instructions.Objects
{
    [InstructionHandler(Code.Newobj)]
    class EmitNewobj : ICodeEmitter
    {
        /// <summary>
        /// Emits an newobj instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            // todo: currently only supports structs

            MethodReference ctor = (MethodReference)instruction.Operand;
            TypeRef type = context.Compiler.Lookup.GetTypeRef(ctor.DeclaringType);

            ValueRef objPtr = LLVM.BuildAlloca(builder, type, "newobj");

            // Call .ctor
            int paramCount = 1 + ctor.Parameters.Count;
            ValueRef[] values = new ValueRef[paramCount];
            for (int i = paramCount - 1; i >= 1; i--)
            {
                StackElement element = context.CurrentStack.Pop();
                values[i] = element.Value;
            }
            values[0] = objPtr;

            LLVM.BuildCall(builder, context.Compiler.Lookup.GetFunction(NameHelper.CreateMethodName(ctor)).Value, values, string.Empty);

            // Load and push object on stack
            ValueRef obj = LLVM.BuildLoad(builder, objPtr, "obj");
            context.CurrentStack.Push(new StackElement(obj, TypeHelper.GetTypeFromTypeReference(context.Compiler, ctor.DeclaringType), type));
        }
    }
}
