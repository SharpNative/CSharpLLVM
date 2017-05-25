using Swigged.LLVM;
using Mono.Cecil.Cil;
using Mono.Cecil;
using CSharpLLVM.Compiler;
using CSharpLLVM.Helpers;
using CSharpLLVM.Stack;
using System;

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
            // Check for special cases
            if(instruction.Previous.OpCode.Code == Code.Ldtoken)
            {
                emitFromLdtoken(instruction, context, builder);
                return;
            }
            
            MethodReference methodRef = (MethodReference)instruction.Operand;
            TypeRef returnType = TypeHelper.GetTypeRefFromType(methodRef.ReturnType);

            // Build parameter value and types arrays
            int paramCount = methodRef.Parameters.Count;
            if (methodRef.HasThis)
                paramCount++;

            // Get the method, if it is null, create a new empty one, otherwise reference it
            string methodName = NameHelper.CreateMethodName(methodRef);
            ValueRef? func = context.Compiler.GetFunction(methodName);

            // Process arguments
            // Note: backwards for loop because stack is backwards!
            ValueRef[] argVals = new ValueRef[paramCount];
            TypeRef[] paramTypes = new TypeRef[paramCount];
            for (int i = paramCount - 1; i >= 0; i--)
            {
                StackElement element = context.CurrentStack.Pop();
                argVals[i] = element.Value;

                // Note: the instance pointer is not included in the parameters explicitely
                if (methodRef.HasThis)
                {
                    if (i == 0)
                        paramTypes[i] = element.Type;
                    else
                        paramTypes[i] = TypeHelper.GetTypeRefFromType(methodRef.Parameters[i - 1].ParameterType);
                }
                else
                {
                    paramTypes[i] = TypeHelper.GetTypeRefFromType(methodRef.Parameters[i].ParameterType);
                }

                // Cast needed?
                if (element.Type != paramTypes[i])
                {
                    argVals[i] = LLVM.BuildIntCast(builder, argVals[i], paramTypes[i], "callcast");
                }
            }

            // Function does not exist, create new empty function
            if (!func.HasValue)
            {
                TypeRef functionType = LLVM.FunctionType(returnType, paramTypes, false);
                func = LLVM.AddFunction(context.Compiler.Module, methodName, functionType);
                context.Compiler.AddFunction(methodName, func.Value);
            }

            // Call
            ValueRef retVal = LLVM.BuildCall(builder, func.Value, argVals, string.Empty);

            // Push return value on stack if it has one
            if (methodRef.ReturnType.MetadataType != MetadataType.Void)
                context.CurrentStack.Push(retVal);
        }

        /// <summary>
        /// Emits a call instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void emitFromLdtoken(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            MethodReference methodRef = (MethodReference)instruction.Operand;
            if (methodRef.Name == "InitializeArray" && methodRef.DeclaringType.FullName == "System.Runtime.CompilerServices.RuntimeHelpers")
            {
                StackElement count = context.CurrentStack.Pop();
                StackElement initialValues = context.CurrentStack.Pop();
                StackElement array = context.CurrentStack.Pop();

                ValueRef tmp = LLVM.BuildPointerCast(builder, initialValues.Value, TypeHelper.VoidPtr, "callcast");
                LLVM.BuildCall(builder, RuntimeHelper.Memcpy, new ValueRef[] { array.Value, tmp, count.Value }, string.Empty);
            }
            else
            {
                throw new NotImplementedException("Can't handle special case of call: " + methodRef.FullName);
            }
        }
    }
}
