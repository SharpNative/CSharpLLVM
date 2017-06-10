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
            mType = type;
        }

        /// <summary>
        /// If we should add the method to the VTable.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns>If we should add it to the VTable.</returns>
        private bool shouldAddMethod(MethodDefinition method)
        {
            return (method.Name != ".ctor" && !method.IsStatic);
        }

        /// <summary>
        /// Creates the parent VTable.
        /// </summary>
        /// <param name="parentType">The parent type.</param>
        private void createParentTable(TypeDefinition parentType)
        {
            VTable parentTable = mCompiler.Lookup.GetVTable(parentType);

            // Match method signatures against own.
            Dictionary<int, MethodDefinition> own = new Dictionary<int, MethodDefinition>();
            foreach (MethodDefinition method in parentType.Methods)
            {
                if (!shouldAddMethod(method))
                    continue;

                string shortName = NameHelper.CreateShortMethodName(method);
                
                mCompiler.Lookup.SetMethodNotUnique(method);

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
                Dictionary<int, MethodDefinition> mTableCopy = new Dictionary<int, MethodDefinition>();
                mTable.Add(pair.Key, mTableCopy);
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
                        mCompiler.Lookup.SetMethodNotUnique(method);
                        int nameTableIndex = MyNameTable[shortName];
                        mTableCopy.Add(methodPair.Value, MyTable[nameTableIndex]);
                    }
                    // Use existing method definition.
                    else
                    {
                        mTableCopy.Add(methodPair.Value, parentLookupTable[methodPair.Value]);
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
                // Don't generate types for an interface please.
                if (names.Key.IsInterface)
                    continue;

                string name = string.Format("vtable_{0}_part_{1}", typeName, NameHelper.CreateTypeName(names.Key));

                // Initialize to pointers.
                TypeRef[] types = new TypeRef[names.Value.Count];
                for (int i = 0; i < names.Value.Count; i++)
                {
                    types[i] = TypeHelper.VoidPtr;
                }

                TypeRef type = LLVM.StructType(types, false);
                ValueRef global = LLVM.AddGlobal(mCompiler.Module, type, name);
                LLVM.SetLinkage(global, Linkage.InternalLinkage);

                mGeneratedTable.Add(names.Key, new Tuple<TypeRef, ValueRef>(type, global));
            }
        }

        /// <summary>
        /// Compiles the code for the VTable.
        /// </summary>
        public void Compile()
        {
            foreach (KeyValuePair<TypeDefinition, Tuple<TypeRef, ValueRef>> pair in mGeneratedTable)
            {
                Dictionary<int, MethodDefinition> lookup = mTable[pair.Key];

                int i = 0;
                ValueRef[] values = new ValueRef[lookup.Count];
                foreach (KeyValuePair<int, MethodDefinition> entry in lookup)
                {
                    ValueRef? function = mCompiler.Lookup.GetFunction(NameHelper.CreateMethodName(entry.Value));
                    if (!function.HasValue)
                        throw new InvalidOperationException("Could not find function for: " + entry.Key);

                    values[i++] = LLVM.ConstPointerCast(function.Value, TypeHelper.VoidPtr);
                }

                ValueRef initialValues = LLVM.ConstStruct(values, false);
                LLVM.SetInitializer(pair.Value.Item2, initialValues);
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
        /// Gets entries of the VTable structs other than the owning type.
        /// </summary>
        /// <returns>An array of entries for VTable structs.</returns>
        public KeyValuePair<TypeDefinition, Tuple<TypeRef, ValueRef>>[] GetAllEntries()
        {
            return mGeneratedTable.ToArray();
        }
        
        /// <summary>
        /// Creates a VTable.
        /// </summary>
        public void Create()
        {
            createOwnTable();

            // Parent table.
            if (mType.BaseType != null && mType.BaseType.FullName != "System.Object")
                createParentTable(mType.BaseType.Resolve());

            createTypes();
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
