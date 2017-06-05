using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compiler;
using CSharpLLVM.Stack;
using CSharpLLVM.Helpers;
using Mono.Cecil;
using System;
using System.Collections.Generic;

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

            // Initialize VTables, TODO: move newobj stuff to seperate (internal) method
            Lookup lookup = context.Compiler.Lookup;
            //createVTableInitCode(context, builder, objPtr, myType, myType);//TODO: move to .ctor!
            VTable vtable = lookup.GetVTable(ctor.DeclaringType);
            KeyValuePair<TypeReference, Tuple<TypeRef, ValueRef>>[] others = vtable.GetOtherEntries();
            foreach(KeyValuePair<TypeReference, Tuple<TypeRef, ValueRef>> pair in others)
            {
                uint index = lookup.GetVTableIndex(pair.Key);
                ValueRef vTableGep = LLVM.BuildInBoundsGEP(builder, objPtr, new ValueRef[] { LLVM.ConstInt(TypeHelper.Int32, 0, false), LLVM.ConstInt(TypeHelper.Int32, index, false) }, "vtabledst");
                LLVM.BuildStore(builder, pair.Value.Item2, vTableGep);
            }
            
            vtable.Dump();

            // Load and push object on stack
            ValueRef obj = (ptr) ? objPtr : LLVM.BuildLoad(builder, objPtr, "obj");
            context.CurrentStack.Push(new StackElement(obj, TypeHelper.GetTypeFromTypeReference(context.Compiler, ctor.DeclaringType), type));
        }
    }
}
