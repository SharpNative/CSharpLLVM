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
            MethodReference methodRef = (MethodReference)instruction.Operand;
            TypeRef returnType = TypeHelper.GetTypeRefFromType(methodRef.ReturnType);

            // Check for special cases
            if (instruction.Previous != null && instruction.Previous.OpCode.Code == Code.Ldtoken)
            {
                emitFromLdtoken(instruction, context, builder);
                return;
            }
            // Call System.Object::.ctor() ?
            else if (methodRef.FullName == "System.Void System.Object::.ctor()")
            {
                // Delete object reference from stack and ignore this
                context.CurrentStack.Pop();
                return;
            }
            
            // Build parameter value and types arrays
            int paramCount = methodRef.Parameters.Count;
            if (methodRef.HasThis)
                paramCount++;

            // Get the method, if it is null, create a new empty one, otherwise reference it
            string methodName = NameHelper.CreateMethodName(methodRef);
            ValueRef? func = context.Compiler.Lookup.GetFunction(methodName);

            // Process arguments
            // Note: backwards for loop because stack is backwards!
            ValueRef[] argVals = new ValueRef[paramCount];
            TypeRef[] paramTypes = new TypeRef[paramCount];
            for (int i = paramCount - 1; i >= 0; i--)
            {
                TypeReference type;
                StackElement element = context.CurrentStack.Pop();
                argVals[i] = element.Value;
                
                // Note: the instance pointer is not included in the parameters explicitely
                if (methodRef.HasThis)
                {
                    if (i == 0)
                        type = methodRef.DeclaringType;
                    else
                        type = methodRef.Parameters[i - 1].ParameterType;
                }
                else
                {
                    type = methodRef.Parameters[i].ParameterType;
                }

                paramTypes[i] = TypeHelper.GetTypeRefFromType(type);
                if (TypeHelper.RequiresExtraPointer(type))
                    paramTypes[i] = LLVM.PointerType(paramTypes[i], 0);

                // Cast needed?
                if (element.Type != paramTypes[i])
                {
                    CastHelper.HelpIntAndPtrCast(builder, ref argVals[i], element.Type, paramTypes[i]);
                }
            }

            // Function does not exist, create a declaration for the function
            if (!func.HasValue)
            {
                TypeRef functionType = LLVM.FunctionType(returnType, paramTypes, false);
                func = LLVM.AddFunction(context.Compiler.Module, methodName, functionType);
                context.Compiler.Lookup.AddFunction(methodName, func.Value);
            }

            // Call
            ValueRef retVal = LLVM.BuildCall(builder, func.Value, argVals, string.Empty);

            // Push return value on stack if it has one
            if (methodRef.ReturnType.MetadataType != MetadataType.Void)
                context.CurrentStack.Push(new StackElement(retVal, TypeHelper.GetTypeFromTypeReference(context.Compiler, methodRef.ReturnType)));
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

                ValueRef arrayTmp = LLVM.BuildPointerCast(builder, array.Value, TypeHelper.VoidPtr, "arraytmp");
                ValueRef initialTmp = LLVM.BuildPointerCast(builder, initialValues.Value, TypeHelper.VoidPtr, "inittmp");
                LLVM.BuildCall(builder, RuntimeHelper.Memcpy, new ValueRef[] { arrayTmp, initialTmp, count.Value }, string.Empty);
            }
            else
            {
                throw new NotImplementedException("Can't handle special case of call: " + methodRef.FullName);
            }
        }
    }
}
