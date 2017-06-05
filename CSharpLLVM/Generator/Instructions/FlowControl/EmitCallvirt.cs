using Swigged.LLVM;
using Mono.Cecil.Cil;
using Mono.Cecil;
using CSharpLLVM.Compiler;
using CSharpLLVM.Helpers;
using CSharpLLVM.Stack;
using System;

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
            for (int i = paramCount - 1; i >= 0; i--)
            {
                TypeReference type;
                StackElement element = context.CurrentStack.Pop();
                argVals[i] = element.Value;
                
                // Get type of parameter
                if (i == 0)
                    type = methodRef.DeclaringType;
                else
                    type = methodRef.Parameters[i - 1].ParameterType;

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
            TypeRef functionType = LLVM.FunctionType(returnType, paramTypes, false); //TODO:cleanup
            if (!func.HasValue)
            {
                //TypeRef functionType = LLVM.FunctionType(returnType, paramTypes, false);
                func = LLVM.AddFunction(context.Compiler.Module, methodName, functionType);
                context.Compiler.Lookup.AddFunction(methodName, func.Value);
            }

            // Call
            uint index = context.Compiler.Lookup.GetVTableIndex(methodRef.DeclaringType);
            VTable vTable = context.Compiler.Lookup.GetVTable(methodRef.DeclaringType);
            TypeRef vTableType = LLVM.PointerType(functionType, 0);
            
            ValueRef gepVTable = LLVM.BuildGEP(builder, argVals[0], new ValueRef[] { LLVM.ConstInt(TypeHelper.Int32, index, false) }, "vtableoff");
            ValueRef vTablePtr = LLVM.BuildPointerCast(builder, gepVTable, LLVM.PointerType(vTableType, 0), "vtableptr");
            ValueRef methodPtr = LLVM.BuildGEP(builder, vTablePtr, new ValueRef[] { LLVM.ConstInt(TypeHelper.Int32, (uint)vTable.GetMethodIndex(methodRef.DeclaringType, methodRef), false) }, "methodptr");
            ValueRef method = LLVM.BuildLoad(builder, methodPtr, "method");

            ValueRef retVal = LLVM.BuildCall(builder, method, argVals, string.Empty);

            // Push return value on stack if it has one
            if (methodRef.ReturnType.MetadataType != MetadataType.Void)
                context.CurrentStack.Push(new StackElement(retVal, TypeHelper.GetTypeFromTypeReference(context.Compiler, methodRef.ReturnType)));
        }
    }
}
