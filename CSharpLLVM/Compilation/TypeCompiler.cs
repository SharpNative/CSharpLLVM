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
        enum TypeKind
        {
            Struct,
            Enum,
            Class,
            Interface
        }

        private static readonly ConsoleColor[] ColorLookup =
        {
            ConsoleColor.DarkCyan,
            ConsoleColor.DarkGreen,
            ConsoleColor.Cyan,
            ConsoleColor.DarkMagenta
        };

        private Compiler mCompiler;
        private Lookup mLookup;

        /// <summary>
        /// Creates a new TypeCompiler.
        /// </summary>
        /// <param name="compiler">The compiler.</param>
        public TypeCompiler(Compiler compiler)
        {
            mCompiler = compiler;
            mLookup = compiler.Lookup;
        }

        /// <summary>
        /// Gets the type kind of a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The type kind.</returns>
        private TypeKind getTypeKind(TypeDefinition type)
        {
            bool isStruct = (!type.IsEnum && type.IsValueType);
            bool isEnum = type.IsEnum;
            bool isInterface = type.IsInterface;
            bool isClass = (!isStruct && !isInterface);

            if (isStruct)
                return TypeKind.Struct;
            else if (isEnum)
                return TypeKind.Enum;
            else if (isInterface)
                return TypeKind.Interface;
            else /*if (isClass)*/
                return TypeKind.Class;
        }

        /// <summary>
        /// Creates a type declaration.
        /// </summary>
        /// <param name="type">The type.</param>
        public void CreateDeclaration(TypeDefinition type)
        {
            TypeKind typeKind = getTypeKind(type);

            if (typeKind == TypeKind.Enum)
            {
                // The fields within the enum indicate its type.
                mLookup.AddType(type, TypeHelper.GetTypeRefFromType(type.Fields[0].FieldType));
            }
            else
            {
                // The content will be generated later.
                TypeRef data = LLVM.StructCreateNamed(mCompiler.ModuleContext, NameHelper.CreateTypeName(type));
                mLookup.AddType(type, data);
            }
        }

        /// <summary>
        /// Compiles a type.
        /// </summary>
        /// <param name="type">The type.</param>
        public void Compile(TypeDefinition type)
        {
            TypeKind typeKind = getTypeKind(type);

            // Log.
            if (mCompiler.Options.Verbose)
            {
                Console.ForegroundColor = ColorLookup[(int)typeKind];
                Console.WriteLine(string.Format("Compiling type {0}", type.FullName));
                Console.ForegroundColor = ConsoleColor.Gray;
            }

            // Enums can be fully generated during the declaration pass. Nothing to do.
            if (typeKind == TypeKind.Enum)
                return;

            // VTable.
            VTable vtable = null;
            bool hasVTable = ((typeKind == TypeKind.Class || typeKind == TypeKind.Interface) && mCompiler.Lookup.NeedsVirtualCall(type));
            if (hasVTable)
            {
                vtable = new VTable(mCompiler, type);
                mLookup.AddVTable(vtable);
                vtable.Create();
            }

            // Create struct for this type.
            TypeRef data = mLookup.GetTypeRef(type);
            List<TypeRef> structData = new List<TypeRef>();
            List<IStructEntry> fields = mLookup.GetStructLayout(type);

            // Fields.
            ulong fieldTotalSize = 0;
            TypeDefinition currentType = type;
            foreach (IStructEntry entry in fields)
            {
                // VTable for a class?
                if (entry.EntryType == StructEntryType.ClassVTable)
                {
                    // Only if there are virtual calls on this type.
                    if (hasVTable)
                    {
                        VTableEntry barrier = (VTableEntry)entry;
                        structData.Add(LLVM.PointerType(vtable.GetEntry(barrier.Type).Item1, 0));
                    }
                }
                // Entry that points to a table of VTables for interfaces
                else if (entry.EntryType == StructEntryType.InterfaceVTablesTable)
                {
                    // TODO
                    structData.Add(TypeHelper.VoidPtr);
                }
                // Field entry
                else /*if(entry.EntryType == StructEntryType.Field)*/
                {
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
                if (typeKind != TypeKind.Struct)
                    throw new InvalidOperationException("Fixed size not on a struct?!");

                int needed = type.ClassSize - (int)fieldTotalSize;
                for (int i = 0; i < needed; i++)
                    structData.Add(TypeHelper.Int8);
            }

            // Set struct data.
            LLVM.StructSetBody(data, structData.ToArray(), packed);

            // For classes, generate the "newobj" method.
            if (typeKind == TypeKind.Class)
            {
                ValueRef newobjFunc = createNewobjMethod(type);
                mCompiler.Lookup.AddNewobjMethod(type, newobjFunc);
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
                KeyValuePair<TypeDefinition, Tuple<TypeRef, ValueRef>>[] others = vtable.GetAllClassEntries();
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
