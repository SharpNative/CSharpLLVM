using CSharpLLVM.Helpers;
using CSharpLLVM.Lookups;
using Mono.Cecil;
using Swigged.LLVM;
using System;
using System.Collections.Generic;

namespace CSharpLLVM.Compilation
{
    class TypeCompiler
    {
        private Compiler mCompiler;
        private Lookup mLookup;

        /// <summary>
        /// Creates a new TypeCompiler.
        /// </summary>
        /// <param name="compiler">The compiler.</param>
        /// <param name="lookup">The lookup.</param>
        public TypeCompiler(Compiler compiler)
        {
            mCompiler = compiler;
            mLookup = compiler.Lookup;
        }

        /// <summary>
        /// Compiles a type.
        /// </summary>
        /// <param name="type">The type.</param>
        public void Compile(TypeDefinition type)
        {
            bool isStruct = (!type.IsEnum && type.IsValueType);
            bool isEnum = type.IsEnum;
            bool isInterface = type.IsInterface;
            bool isClass = (!isStruct && !isInterface);

            // Log.
            Console.ForegroundColor = isStruct ? ConsoleColor.DarkCyan : isEnum ? ConsoleColor.DarkGreen : isInterface ? ConsoleColor.DarkMagenta : ConsoleColor.Cyan;
            Console.WriteLine(string.Format("Compiling type {0}", type.FullName));
            Console.ForegroundColor = ConsoleColor.Gray;

            // Enums
            if (isEnum)
            {
                // The fields within the enum indicate its type.
                mLookup.AddType(type, TypeHelper.GetTypeRefFromType(type.Fields[0].FieldType));
            }
            // Structs and classes.
            else
            {
                // VTable
                VTable vtable = null;
                bool hasVTable = ((isClass || isInterface) && mCompiler.Lookup.NeedsVirtualCall(type));
                if (hasVTable)
                {
                    vtable = new VTable(mCompiler, type);
                    mLookup.AddVTable(vtable);
                    vtable.Create();
                    //vtable.Dump();
                }

                // Create struct for this type.
                TypeRef data = LLVM.StructCreateNamed(mCompiler.ModuleContext, NameHelper.CreateTypeName(type));
                mLookup.AddType(type, data);
                List<TypeRef> structData = new List<TypeRef>();
                List<IStructEntry> fields = mLookup.GetStructLayout(type);

                // Fields.
                ulong fieldTotalSize = 0;
                TypeDefinition currentType = type;
                foreach (IStructEntry entry in fields)
                {
                    // Barrier
                    if (entry.IsBarrier)
                    {
                        if (hasVTable)
                        {
                            StructBarrierEntry barrier = (StructBarrierEntry)entry;
                            structData.Add(LLVM.PointerType(vtable.GetEntry(barrier.Type).Item1, 0));
                        }
                        continue;
                    }

                    FieldDefinition field = ((StructFieldEntry)entry).Field;
                    TypeRef fieldType = TypeHelper.GetTypeRefFromType(field.FieldType);
                    currentType = field.DeclaringType;

                    // Static field.
                    if (field.IsStatic)
                    {
                        // Only add it if we don't have it already (is possible when inheriting classes).
                        if (!mLookup.HasStaticField(field))
                        {
                            ValueRef val = LLVM.AddGlobal(mCompiler.Module, fieldType, NameHelper.CreateFieldName(field.FullName));

                            // Note: the initializer may be changed later if the compiler sees that it can be constant.
                            LLVM.SetInitializer(val, LLVM.ConstNull(fieldType));
                            mLookup.AddStaticField(field, val);
                        }
                    }
                    // Field for type instance.
                    else
                    {
                        structData.Add(fieldType);
                        fieldTotalSize += LLVM.SizeOfTypeInBits(mCompiler.TargetData, fieldType) / 8;
                    }
                }

                // Packing?
                bool packed = (type.PackingSize != -1);
                if (type.PackingSize != 1 && type.PackingSize != -1 && type.PackingSize != 0)
                {
                    throw new NotImplementedException("The packing size " + type.PackingSize + " is not implemented");
                }

                // Fixed size?
                if (type.ClassSize > 0 && (int)fieldTotalSize < type.ClassSize)
                {
                    if (!isStruct)
                        throw new InvalidOperationException("Fixed size not on a struct?!");

                    int needed = type.ClassSize - (int)fieldTotalSize;
                    for (int i = 0; i < needed; i++)
                        structData.Add(TypeHelper.Int8);
                }

                // Set struct data.
                LLVM.StructSetBody(data, structData.ToArray(), packed);

                // For classes, generate the "newobj" method.
                if (isClass)
                {
                    ValueRef newobjFunc = createNewobjMethod(type);
                    mCompiler.Lookup.AddNewobjMethod(type, newobjFunc);
                }
            }
        }

        /// <summary>
        /// Creates the "newobj" method for a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The function.</returns>
        private ValueRef createNewobjMethod(TypeDefinition type)
        {
            string name = string.Format("newobj_{0}", type.FullName);
            BuilderRef builder = LLVM.CreateBuilderInContext(mCompiler.ModuleContext);

            // Create method type.
            TypeRef funcType = LLVM.FunctionType(TypeHelper.GetTypeRefFromType(type), new TypeRef[0], false);
            ValueRef func = LLVM.AddFunction(mCompiler.Module, name, funcType);
            LLVM.SetLinkage(func, Linkage.InternalLinkage);

            BasicBlockRef entry = LLVM.AppendBasicBlockInContext(mCompiler.ModuleContext, func, string.Empty);
            LLVM.PositionBuilderAtEnd(builder, entry);

            // Allocate space on the heap for this object.
            TypeRef typeRef = mCompiler.Lookup.GetTypeRef(type);
            ValueRef objPtr = LLVM.BuildMalloc(builder, typeRef, "newobj");

            // Initialize VTables.
            Lookup lookup = mCompiler.Lookup;
            if (lookup.NeedsVirtualCall(type))
            {
                VTable vtable = lookup.GetVTable(type);
                KeyValuePair<TypeDefinition, Tuple<TypeRef, ValueRef>>[] others = vtable.GetAllEntries();
                foreach (KeyValuePair<TypeDefinition, Tuple<TypeRef, ValueRef>> pair in others)
                {
                    uint index = lookup.GetClassVTableIndex(pair.Key);
                    ValueRef vTableGep = LLVM.BuildInBoundsGEP(builder, objPtr, new ValueRef[] { LLVM.ConstInt(TypeHelper.Int32, 0, false), LLVM.ConstInt(TypeHelper.Int32, index, false) }, "vtabledst");
                    LLVM.BuildStore(builder, pair.Value.Item2, vTableGep);
                }
            }

            // Return object pointer.
            LLVM.BuildRet(builder, objPtr);

            LLVM.DisposeBuilder(builder);

            return func;
        }
    }
}
