using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compiler;
using CSharpLLVM.Stack;
using CSharpLLVM.Helpers;
using Mono.Cecil;
using System;

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
            MethodReference ctor = (MethodReference)instruction.Operand;
            TypeRef type = context.Compiler.Lookup.GetTypeRef(ctor.DeclaringType);

            bool ptr = TypeHelper.RequiresExtraPointer(ctor.DeclaringType);
            ValueRef objPtr = (ptr) ? LLVM.BuildMalloc(builder, type, "newobj") : LLVM.BuildAlloca(builder, type, "newobj");
            
            // Call .ctor
            int paramCount = 1 + ctor.Parameters.Count;
            ValueRef[] values = new ValueRef[paramCount];
            values[0] = objPtr;
            for (int i = paramCount - 1; i >= 1; i--)
            {
                StackElement element = context.CurrentStack.Pop();
                values[i] = element.Value;
            }

            LLVM.BuildCall(builder, context.Compiler.Lookup.GetFunction(NameHelper.CreateMethodName(ctor)).Value, values, string.Empty);

            // Initialize VTables
            TypeDefinition myType = ctor.DeclaringType.Resolve();
            createVTableInitCode(context, builder, objPtr, myType, myType);//TODO: move to .ctor!

            // Load and push object on stack
            ValueRef obj = (ptr) ? objPtr : LLVM.BuildLoad(builder, objPtr, "obj");
            context.CurrentStack.Push(new StackElement(obj, TypeHelper.GetTypeFromTypeReference(context.Compiler, ctor.DeclaringType), type));
        }

        private void createVTableInitCode(MethodContext context, BuilderRef builder, ValueRef objPtr, TypeDefinition baseType, TypeDefinition myType)
        {
            if (baseType == null || baseType.FullName == "System.Object")
                return;

            Lookup lookup = context.Compiler.Lookup;
            Tuple<TypeRef, ValueRef> tuple = lookup.GetVTable(myType).GetEntry(baseType);

            uint index = lookup.GetVTableIndex(baseType);
            ValueRef ptr = LLVM.BuildGEP(builder, objPtr, new ValueRef[] { LLVM.ConstInt(TypeHelper.Int32, index, false) }, "vtabledst");
            ValueRef castedPtr = LLVM.BuildPointerCast(builder, ptr, LLVM.PointerType(tuple.Item1, 0), "ptrcast");
            LLVM.BuildStore(builder, LLVM.BuildLoad(builder, tuple.Item2, "abc"), LLVM.BuildPointerCast(builder,ptr,LLVM.PointerType(tuple.Item1,0),"jiji"));
            //LLVM.BuildStore(builder, tuple.Item2/*valPtr*/, castedPtr);

            createVTableInitCode(context, builder, objPtr, baseType.BaseType.Resolve(), myType);
        }
    }
}
