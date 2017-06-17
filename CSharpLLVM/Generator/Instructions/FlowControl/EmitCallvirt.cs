using Swigged.LLVM;
using Mono.Cecil.Cil;
using Mono.Cecil;
using CSharpLLVM.Helpers;
using CSharpLLVM.Stack;
using CSharpLLVM.Compilation;
using CSharpLLVM.Lookups;
using System;

namespace CSharpLLVM.Generator.Instructions.FlowControl
{
    [InstructionHandler(Code.Callvirt)]
    class EmitCallvirt : ICodeEmitter
    {
        /// <summary>
        /// Emits a callvirt instruction.
        /// </summary>
        /// <param name="instruction">The instruction.</param>
        /// <param name="context">The context.</param>
        /// <param name="builder">The builder.</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            MethodReference methodRef = (MethodReference)instruction.Operand;
            TypeRef returnType = TypeHelper.GetTypeRefFromType(methodRef.ReturnType);
            bool needsVirtualCall = context.Compiler.Lookup.NeedsVirtualCall(methodRef.DeclaringType);
            Lookup lookup = context.Compiler.Lookup;

            // Build parameter value and types arrays.
            int paramCount = 1 + methodRef.Parameters.Count;

            // Get the method, if it is null, create a new empty one, otherwise reference it.
            string methodName = NameHelper.CreateMethodName(methodRef);
            ValueRef? func = lookup.GetFunction(methodName);

            // Process arguments.
            // Note: backwards for loop because stack is backwards!
            ValueRef[] argVals = new ValueRef[paramCount];
            TypeRef[] paramTypes = new TypeRef[paramCount];
            for (int i = paramCount - 1; i >= 0; i--)
            {
                TypeReference type;
                StackElement element = context.CurrentStack.Pop();
                argVals[i] = element.Value;

                // Get type of parameter.
                if (i == 0)
                    type = methodRef.DeclaringType;
                else
                    type = methodRef.Parameters[i - 1].ParameterType;

                paramTypes[i] = TypeHelper.GetTypeRefFromType(type);
                if (type.IsValueType && i == 0)
                    paramTypes[i] = LLVM.PointerType(paramTypes[i], 0);

                // Cast needed?
                if (element.Type != paramTypes[i])
                {
                    CastHelper.HelpIntAndPtrCast(builder, ref argVals[i], ref element.Type, paramTypes[i], "callvirtcast");
                }
            }

            // Function does not exist, create a declaration for the function.
            TypeRef functionType = LLVM.FunctionType(returnType, paramTypes, false);

            // Call.
            ValueRef method;
            if (needsVirtualCall && !lookup.IsMethodUnique(methodRef))
            {
                // We need a virtual call.
                TypeDefinition methodParent = methodRef.DeclaringType.Resolve();
                TypeRef funcPtrType = LLVM.PointerType(functionType, 0);
                VTable vTable = lookup.GetVTable(methodParent);
                ValueRef methodGep;

                // Two cases:
                // 1) The parent of the method is an interface.
                //    In this case, we need to first get the VTable from the indirection table.
                // 2) The parent of the method is a class.
                //    In this case, we can directly now the offset to the VTable from the object pointer.

                if (!methodParent.IsInterface)
                {
                    uint index = lookup.GetClassVTableIndex(methodParent);
                    ValueRef vTableGep = LLVM.BuildInBoundsGEP(builder, argVals[0], new ValueRef[] { LLVM.ConstInt(TypeHelper.Int32, 0, false), LLVM.ConstInt(TypeHelper.Int32, index, false) }, "vtablegep");
                    ValueRef vTableInstance = LLVM.BuildLoad(builder, vTableGep, "vtable");
                    methodGep = LLVM.BuildInBoundsGEP(builder, vTableInstance, new ValueRef[] { LLVM.ConstInt(TypeHelper.Int32, 0, false), LLVM.ConstInt(TypeHelper.Int32, (uint)vTable.GetMethodIndex(methodParent, methodRef), false) }, "methodgep");
                }
                else
                {
                    uint index = lookup.GetInterfaceID(methodParent);

                    ValueRef indirectionGep = LLVM.BuildInBoundsGEP(builder, argVals[0], new ValueRef[] { LLVM.ConstInt(TypeHelper.Int32, 0, false) }, "indirectiongep");
                    indirectionGep = LLVM.BuildPointerCast(builder, indirectionGep, LLVM.PointerType(LLVM.PointerType(LLVM.PointerType(TypeHelper.VoidPtr, 0), 0), 0), string.Empty);
                    ValueRef indirectionTable = LLVM.BuildLoad(builder, indirectionGep, "indirectiontable");
                    ValueRef vTableGep = LLVM.BuildInBoundsGEP(builder, indirectionTable, new ValueRef[] { LLVM.ConstInt(TypeHelper.Int32, index, false) }, "vtablegep");
                    ValueRef vTableInstance = LLVM.BuildLoad(builder, vTableGep, "vtable");
                    methodGep = LLVM.BuildInBoundsGEP(builder, vTableInstance, new ValueRef[] { LLVM.ConstInt(TypeHelper.Int32, (uint)vTable.GetMethodIndex(methodParent, methodRef), false) }, "methodgep");
                }

                // Indirect call.
                ValueRef methodPtr = LLVM.BuildLoad(builder, methodGep, "methodptr");
                method = LLVM.BuildPointerCast(builder, methodPtr, funcPtrType, "method");
            }
            else
            {
                // We can call it directly without VTable lookup.
                method = func.Value;
            }

            ValueRef retVal = LLVM.BuildCall(builder, method, argVals, string.Empty);
            if (instruction.HasPrefix(Code.Tail))
                LLVM.SetTailCall(retVal, true);

            // Push return value on stack if it has one.
            if (methodRef.ReturnType.MetadataType != MetadataType.Void)
                context.CurrentStack.Push(new StackElement(retVal, methodRef.ReturnType));
        }
    }
}
