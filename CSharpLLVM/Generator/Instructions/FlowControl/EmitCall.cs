using Swigged.LLVM;
using Mono.Cecil.Cil;
using Mono.Cecil;
using CSharpLLVM.Compiler;
using CSharpLLVM.Helpers;
using CSharpLLVM.Stack;

namespace CSharpLLVM.Generator.Instructions.FlowControl
{
    [InstructionHandler(Code.Call)]
    class EmitCall : ICodeEmitter
    {
        /// <summary>
        /// Emits a call instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            // TODO: test order of arguments with multiple arguemnts
            // TODO: ability to return struct value

            MethodReference methodRef = (MethodReference)instruction.Operand;
            TypeRef returnType = TypeHelper.GetTypeRefFromType(methodRef.ReturnType);

            // Build parameter value and types arrays
            int paramCount = methodRef.Parameters.Count;
            if (methodRef.HasThis)
                paramCount++;
            
            ValueRef[] argVals = new ValueRef[paramCount];
            TypeRef[] argTypes = new TypeRef[paramCount];
            for (int i = 0; i < paramCount; i++)
            {
                StackElement element = context.CurrentStack.Pop();
                argVals[i] = element.Value;
                argTypes[i] = element.Type;
            }

            // Get the method, if it is null, create a new empty one, otherwise reference it
            string methodName = NameHelper.CreateMethodName(methodRef);
            ValueRef? func = context.Compiler.GetFunction(methodName);

            // Function does not exist, create new empty function
            if (!func.HasValue)
            {
                TypeRef functionType = LLVM.FunctionType(returnType, argTypes, false);
                func = LLVM.AddFunction(context.Compiler.Module, methodName, functionType);
                context.Compiler.AddFunction(methodName, func.Value);
            }

            // Call
            ValueRef retVal = LLVM.BuildCall(builder, func.Value, argVals, string.Empty);

            // Push on stack if return value
            if (methodRef.ReturnType.MetadataType != MetadataType.Void)
                context.CurrentStack.Push(retVal);
        }
    }
}
