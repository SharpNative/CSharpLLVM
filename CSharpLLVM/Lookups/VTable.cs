using CSharpLLVM.Compilation;
using CSharpLLVM.Helpers;
using Mono.Cecil;
using Swigged.LLVM;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CSharpLLVM.Lookups
{
    class VTable
    {
        private Compiler mCompiler;
        private Lookup mLookup;
        private TypeDefinition mType;

        // Contains pairs of methods and their indices, must match correct parent types (if any).
        private Dictionary<TypeDefinition, Dictionary<int, MethodDefinition>> mTable = new Dictionary<TypeDefinition, Dictionary<int, MethodDefinition>>();
        private Dictionary<TypeDefinition, Dictionary<string, int>> mNameTable = new Dictionary<TypeDefinition, Dictionary<string, int>>();

        // Lookup for generated code of VTable.
        private Dictionary<TypeDefinition, Tuple<TypeRef, ValueRef>> mGeneratedTable = new Dictionary<TypeDefinition, Tuple<TypeRef, ValueRef>>();

        public TypeDefinition Type { get { return mType; } }

        protected Dictionary<int, MethodDefinition> MyTable { get { return mTable[mType]; } }
        protected Dictionary<string, int> MyNameTable { get { return mNameTable[mType]; } }

        /// <summary>
        /// Creates a new VTable.
        /// </summary>
        /// <param name="compiler">The compiler.</param>
        /// <param name="type">The type for which this VTable is.</param>
        public VTable(Compiler compiler, TypeDefinition type)
        {
            mCompiler = compiler;
            mLookup = compiler.Lookup;
            mType = type;
        }

        /// <summary>
        /// If we should add the method to the VTable.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns>If we should add it to the VTable.</returns>
        private bool shouldAddMethod(MethodDefinition method)
        {
            return (!method.IsStatic && method.Name != ".ctor");
        }

        /// <summary>
        /// Creates the parent VTable.
        /// </summary>
        /// <param name="parentType">The parent type.</param>
        private void createParentTable(TypeDefinition parentType)
        {
            VTable parentTable = mLookup.GetVTable(parentType);

            // Match method signatures against own.
            Dictionary<int, MethodDefinition> own = new Dictionary<int, MethodDefinition>();
            foreach (MethodDefinition method in parentType.Methods)
            {
                if (!shouldAddMethod(method))
                    continue;

                string shortName = NameHelper.CreateShortMethodName(method);

                mLookup.SetMethodNotUnique(method);

                // This type overrides the method in the parent type.
                if (MyNameTable.ContainsKey(shortName) && MyTable[MyNameTable[shortName]].IsVirtual)
                {
                    int nameTableIndex = MyNameTable[shortName];
                    own.Add(parentTable.MyNameTable[shortName], MyTable[nameTableIndex]);
                }
                // Use parent method definition.
                else
                {
                    int nameTableIndex = parentTable.MyNameTable[shortName];
                    own.Add(nameTableIndex, method);
                }
            }

            mTable.Add(parentType, own);

            // Generate name table.
            Dictionary<string, int> nameTable = new Dictionary<string, int>();
            foreach (KeyValuePair<int, MethodDefinition> pair in own)
            {
                nameTable.Add(NameHelper.CreateShortMethodName(pair.Value), pair.Key);
            }
            mNameTable.Add(parentType, nameTable);

            // Add other tables that are inside the parent table.
            foreach (KeyValuePair<TypeDefinition, Dictionary<string, int>> pair in parentTable.mNameTable)
            {
                // Skip parent type itself.
                if (pair.Key == parentType)
                    continue;

                // Create own copies of this table.
                Dictionary<int, MethodDefinition> tableCopy = new Dictionary<int, MethodDefinition>();
                mTable.Add(pair.Key, tableCopy);
                mNameTable.Add(pair.Key, pair.Value);

                // Create own copy of table with possible modifications.
                Dictionary<int, MethodDefinition> parentLookupTable = parentTable.mTable[pair.Key];
                foreach (KeyValuePair<string, int> methodPair in pair.Value)
                {
                    string shortName = methodPair.Key;

                    // Did we override this method?
                    if (MyNameTable.ContainsKey(shortName) && (MyTable[MyNameTable[shortName]].IsVirtual))
                    {
                        MethodDefinition method = MyTable[MyNameTable[shortName]];
                        mLookup.SetMethodNotUnique(method);
                        int nameTableIndex = MyNameTable[shortName];
                        tableCopy.Add(methodPair.Value, MyTable[nameTableIndex]);
                    }
                    // Use existing method definition.
                    else
                    {
                        tableCopy.Add(methodPair.Value, parentLookupTable[methodPair.Value]);
                    }
                }
            }
        }

        /// <summary>
        /// Creates own tables.
        /// </summary>
        private void createOwnTable()
        {
            // Create own tables.
            mNameTable.Add(mType, new Dictionary<string, int>());
            mTable.Add(mType, new Dictionary<int, MethodDefinition>());

            // Add own methods.
            int index = 0;
            foreach (MethodDefinition method in mType.Methods)
            {
                if (!shouldAddMethod(method))
                    continue;

                mNameTable[mType].Add(NameHelper.CreateShortMethodName(method), index);
                mTable[mType].Add(index, method);
                index++;
            }
        }

        /// <summary>
        /// Creates the LLVM types.
        /// </summary>
        private void createTypes()
        {
            string typeName = NameHelper.CreateTypeName(mType);
            foreach (KeyValuePair<TypeDefinition, Dictionary<string, int>> names in mNameTable)
            {
                string name = string.Format("vtable_{0}_part_{1}", typeName, NameHelper.CreateTypeName(names.Key));
                Dictionary<string, int> idDict = names.Value;

                // Initialize to pointers.
                TypeRef[] types = new TypeRef[idDict.Count];
                for (int i = 0; i < names.Value.Count; i++)
                {
                    types[i] = TypeHelper.VoidPtr;
                }

                TypeRef type = LLVM.StructTypeInContext(mCompiler.ModuleContext, types, false);
                ValueRef global = LLVM.AddGlobal(mCompiler.Module, type, name);
                LLVM.SetLinkage(global, Linkage.PrivateLinkage);

                mGeneratedTable.Add(names.Key, new Tuple<TypeRef, ValueRef>(type, global));
            }
        }

        /// <summary>
        /// Compiles the code for the VTable.
        /// </summary>
        public void Compile()
        {
            // Create the VTable structs for this type containing the method pointers.
            foreach (KeyValuePair<TypeDefinition, Tuple<TypeRef, ValueRef>> pair in mGeneratedTable)
            {
                Dictionary<int, MethodDefinition> lookup = mTable[pair.Key];

                int i = 0;
                ValueRef[] values = new ValueRef[lookup.Count];
                foreach (KeyValuePair<int, MethodDefinition> entry in lookup)
                {
                    ValueRef? function = mLookup.GetFunction(NameHelper.CreateMethodName(entry.Value));
                    if (!function.HasValue)
                        throw new InvalidOperationException("Could not find function for: " + entry.Value);

                    values[i++] = LLVM.ConstPointerCast(function.Value, TypeHelper.VoidPtr);
                }

                ValueRef initialValues = LLVM.ConstStruct(values, false);
                LLVM.SetInitializer(pair.Value.Item2, initialValues);
            }

            // For each interface type, we want to create a table that links to the corresponding VTables.
            if (Type.HasInterfaces)
            {
                // Create arrays for the element and their types.
                // We know the values and types for the tables that are generated for this type.
                // However, there are spots that will be blank, so we need to fill these with "null".
                int count = mLookup.MaxInterfaceID;
                ValueRef[] elementValues = new ValueRef[count];
                TypeRef[] elementTypes = new TypeRef[count];

                // Fill default value for blank spots.
                ValueRef nullValue = LLVM.ConstNull(TypeHelper.VoidPtr);
                for (int i = 0; i < count; i++)
                {
                    elementValues[i] = nullValue;
                    elementTypes[i] = TypeHelper.VoidPtr;
                }

                // Fill in existing values
                TypeDefinition[] allInterfaces = TypeHelper.GetAllInterfaces(Type);
                foreach (TypeDefinition type in allInterfaces)
                {
                    uint id = mLookup.GetInterfaceID(type);
                    Tuple<TypeRef, ValueRef> tuple = mGeneratedTable[type];
                    elementValues[id] = tuple.Item2;
                    elementTypes[id] = LLVM.PointerType(tuple.Item1, 0);
                }

                // Add a global for this table.
                TypeRef tableType = LLVM.StructTypeInContext(mCompiler.ModuleContext, elementTypes, false);
                ValueRef table = LLVM.ConstStruct(elementValues, false);
                ValueRef global = LLVM.AddGlobal(mCompiler.Module, tableType, string.Format("interface_vtable_table_{0}", Type.FullName));
                LLVM.SetInitializer(global, table);
                LLVM.SetLinkage(global, Linkage.PrivateLinkage);

                // Add indirection table to lookup.
                Tuple<TypeRef, ValueRef> interfaceIndirectionTable = new Tuple<TypeRef, ValueRef>(tableType, global);
                mLookup.AddInterfaceIndirectionTable(Type, interfaceIndirectionTable);
            }
        }

        /// <summary>
        /// Gets the index of a method in the corresponding VTable.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="method">The method.</param>
        /// <returns>The index inside the table of the type.</returns>
        public int GetMethodIndex(TypeDefinition type, MethodReference method)
        {
            string name = NameHelper.CreateShortMethodName(method);
            return mNameTable[type][name];
        }

        /// <summary>
        /// Gets an entry for a VTable type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The VTable.</returns>
        public Tuple<TypeRef, ValueRef> GetEntry(TypeDefinition type)
        {
            if (!mGeneratedTable.ContainsKey(type))
                throw new InvalidOperationException("Cannot find the created VTable for type: " + type);

            return mGeneratedTable[type];
        }

        /// <summary>
        /// Gets the class entries of the VTable structs.
        /// </summary>
        /// <returns>An array of entries for VTable structs.</returns>
        public KeyValuePair<TypeDefinition, Tuple<TypeRef, ValueRef>>[] GetAllClassEntries()
        {
            return mGeneratedTable.Where(f => !f.Key.IsInterface).ToArray();
        }

        /// <summary>
        /// Creates a VTable.
        /// </summary>
        public void Create()
        {
            createOwnTable();

            // Parent table for base type.
            if (mType.BaseType != null && mType.BaseType.FullName != "System.Object")
                createParentTable(mType.BaseType.Resolve());

            // Parent tables for interfaces (if any).
            if (mType.HasInterfaces)
            {
                foreach (TypeDefinition type in mType.Interfaces)
                {
                    createParentTable(type);
                }
            }

            // Only create the LLVM types if we're not an interface.
            // Because we need the data of the VTable, but we don't want the LLVM types.
            if (!Type.IsInterface)
                createTypes();

#if DEBUG
            Dump();
#endif
        }

        /// <summary>
        /// Dumps the VTable for debugging purposes.
        /// </summary>
        public void Dump()
        {
            Console.WriteLine("----- VTable for type " + mType + " -----");
            foreach (KeyValuePair<TypeDefinition, Dictionary<int, MethodDefinition>> entry in mTable)
            {
                Console.WriteLine("  Type: " + entry.Key);
                foreach (KeyValuePair<int, MethodDefinition> methods in entry.Value)
                {
                    Console.WriteLine("\t" + methods.Key + " -> " + methods.Value);
                }
                Console.WriteLine("  -----");
            }
        }
    }
}
