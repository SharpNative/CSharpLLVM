using CSharpLLVM.Helpers;
using Mono.Cecil;
using System;
using System.Collections.Generic;

namespace CSharpLLVM.Compiler
{
    class VTable
    {
        private Compiler mCompiler;
        private TypeDefinition mType;

        // Contains pairs of methods and their indices, must match correct parent types (if any)
        private Dictionary<TypeReference, Dictionary<int, MethodReference>> mTable = new Dictionary<TypeReference, Dictionary<int, MethodReference>>();
        private Dictionary<TypeReference, Dictionary<string, int>> mNameTable = new Dictionary<TypeReference, Dictionary<string, int>>();

        public TypeReference Type { get { return mType; } }

        protected Dictionary<int, MethodReference> MyTable { get { return mTable[mType]; } }
        protected Dictionary<string, int> MyNameTable { get { return mNameTable[mType]; } }

        /// <summary>
        /// Creates a new VTable
        /// </summary>
        /// <param name="compiler">The compiler</param>
        /// <param name="type">The type for which this VTable is</param>
        public VTable(Compiler compiler, TypeDefinition type)
        {
            mCompiler = compiler;
            mType = type;
        }

        /// <summary>
        /// If we should add the method to the VTable
        /// </summary>
        /// <param name="method">The method</param>
        /// <returns>If we should add it to the VTable</returns>
        private bool shouldAddMethod(MethodDefinition method)
        {
            return (method.Name != ".ctor" && !method.IsStatic);
        }

        /// <summary>
        /// Creates the parent VTable
        /// </summary>
        /// <param name="parentType">The parent type</param>
        private void createParentTable(TypeDefinition parentType)
        {
            VTable parentTable = mCompiler.Lookup.GetVTable(parentType);

            // Match method signatures against own
            Dictionary<int, MethodReference> own = new Dictionary<int, MethodReference>();
            foreach (MethodDefinition method in parentType.Methods)
            {
                if (!shouldAddMethod(method))
                    continue;

                string shortName = NameHelper.CreateShortMethodName(method);

                // TODO: check new vs virtual vs override

                // This type overrides the method in the parent type
                if (MyNameTable.ContainsKey(shortName))
                {
                    int nameTableIndex = MyNameTable[shortName];
                    own.Add(parentTable.MyNameTable[shortName], MyTable[nameTableIndex]);
                }
                // Use parent method definition
                else
                {
                    int nameTableIndex = parentTable.MyNameTable[shortName];
                    own.Add(nameTableIndex, method);
                }
            }

            mTable.Add(parentType, own);

            // Generate name table
            Dictionary<string, int> nameTable = new Dictionary<string, int>();
            foreach (KeyValuePair<int, MethodReference> pair in own)
            {
                nameTable.Add(NameHelper.CreateShortMethodName(pair.Value), pair.Key);
            }
            mNameTable.Add(parentType, nameTable);

            // Add other tables that are inside the parent table
            foreach(KeyValuePair<TypeReference, Dictionary<string, int>> pair in parentTable.mNameTable)
            {
                // Skip parent type itself
                if (pair.Key == parentType)
                    continue;

                // Create own copies of this table
                Dictionary<int, MethodReference> mTableCopy = new Dictionary<int, MethodReference>();
                mTable.Add(pair.Key, mTableCopy);
                mNameTable.Add(pair.Key, pair.Value);

                // Create own copy of table with possible modifications
                Dictionary<int, MethodReference> parentLookupTable = parentTable.mTable[pair.Key];
                foreach (KeyValuePair<string, int> methodPair in pair.Value)
                {
                    string shortName = methodPair.Key;

                    // TODO: check new vs virtual vs override

                    // Did we override this method?
                    if (MyNameTable.ContainsKey(shortName))
                    {
                        int nameTableIndex = MyNameTable[shortName];
                        mTableCopy.Add(methodPair.Value, MyTable[nameTableIndex]);
                    }
                    // Use existing method definition
                    else
                    {
                        mTableCopy.Add(methodPair.Value, parentLookupTable[methodPair.Value]);
                    }
                }
            }
        }

        /// <summary>
        /// Creates own tables
        /// </summary>
        private void createOwnTable()
        {
            // Create own tables
            mNameTable.Add(mType, new Dictionary<string, int>());
            mTable.Add(mType, new Dictionary<int, MethodReference>());

            // Add own methods
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
        /// Creates a VTable
        /// </summary>
        public void Create()
        {
            createOwnTable();

            // Parent table
            if (mType.BaseType != null && mType.BaseType.FullName != "System.Object")
                createParentTable(mType.BaseType.Resolve());
        }

        /// <summary>
        /// Dumps the VTable for debugging purposes
        /// </summary>
        public void Dump()
        {
            Console.WriteLine("----- VTable for type " + mType + " -----");
            foreach (KeyValuePair<TypeReference, Dictionary<int, MethodReference>> entry in mTable)
            {
                Console.WriteLine("  Type: " + entry.Key);
                foreach (KeyValuePair<int, MethodReference> methods in entry.Value)
                {
                    Console.WriteLine("\t" + methods.Key + " -> " + methods.Value);
                }
                Console.WriteLine("  -----");
            }
        }
    }
}
