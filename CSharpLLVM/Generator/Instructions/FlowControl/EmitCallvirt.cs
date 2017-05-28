using Swigged.LLVM;
using Mono.Cecil.Cil;
using Mono.Cecil;
using CSharpLLVM.Compiler;
using CSharpLLVM.Helpers;
using CSharpLLVM.Stack;

namespace CSharpLLVM.Generator.Instructions.FlowControl
{
    [InstructionHandler(Code.Callvirt)]
    class EmitCallvirt : ICodeEmitter
    {
        /// <summary>
        /// Emits a callvirt instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            MethodReference methodRef = (MethodReference)instruction.Operand;
            TypeRef returnType = TypeHelper.GetTypeRefFromType(methodRef.ReturnType);
            
            // Build parameter value and types arrays
            int paramCount = 1 + methodRef.Parameters.Count;

            // Get the method, if it is null, create a new empty one, otherwise reference it
            string methodName = NameHelper.CreateMethodName(methodRef);
            ValueRef? func = context.Compiler.Lookup.GetFunction(methodName);

            // Process arguments
            // Note: backwards for loop because stack is backwards!
            ValueRef[] argVals = new ValueRef[paramCount];
            TypeRef[] paramTypes = new TypeRef[paramCount];
            for (int i = paramCount - 1; i >= 1; i--)
            {
                StackElement element = context.CurrentStack.Pop();
                TypeReference type = methodRef.Parameters[i - 1].ParameterType;
                argVals[i] = element.Value;

                paramTypes[i] = TypeHelper.GetTypeRefFromType(type);
                if (TypeHelper.IsClass(type))
                    paramTypes[i] = LLVM.PointerType(paramTypes[i], 0);
                
                // Cast needed?
                if (element.Type != paramTypes[i])
                {
                    CastHelper.HelpIntAndPtrCast(builder, ref argVals[i], element.Type, paramTypes[i]);
                }
            }

            // Instance pointer was pushed first
            StackElement instance = context.CurrentStack.Pop();
            paramTypes[0] = instance.Type;
            argVals[0] = instance.Value;

            // Call
            ValueRef retVal = LLVM.BuildCall(builder, func.Value, argVals, string.Empty);

            // Push return value on stack if it has one
            if (methodRef.ReturnType.MetadataType != MetadataType.Void)
                context.CurrentStack.Push(new StackElement(retVal, TypeHelper.GetTypeFromTypeReference(context.Compiler, methodRef.ReturnType)));
        }
    }
}
