using CSharpLLVM.Helpers;
using Mono.Cecil;
using System;
using System.Collections.Generic;

namespace CSharpLLVM.Lookups
{
    class VTable : IMethodTable
    {
        private TypeDefinition mType;
        private Lookup mLookup;
        public Dictionary<TypeReference, Dictionary<string, int>> mTable = new Dictionary<TypeReference, Dictionary<string, int>>(); //TODO
        
        public string Name { get { return string.Format("vtable_{0}", NameHelper.CreateTypeName(mType)); } }
        public TypeReference[] Parents { get; private set; }
        
        /// <summary>
        /// Creates a new VTable
        /// </summary>
        /// <param name="type">The type</param>
        /// <param name="lookup">The lookup</param>
        public VTable(TypeDefinition type, Lookup lookup)
        {
            mType = type;
            mLookup = lookup;
            create();
        }
        
        /// <summary>
        /// Creates the table
        /// </summary>
        private void create()
        {
            List<ParentMethodTable> tables = new List<ParentMethodTable>();
            Parents = new TypeReference[mType.Interfaces.Count];

            int i = 0;
            foreach (TypeDefinition parent in mType.Interfaces)
            {
                tables.Add(mLookup.GetParentMethodTable(parent));
                Parents[i++] = parent;
            }

            foreach (MethodDefinition method in mType.Methods)
            {
                if (method.Name == ".ctor")
                    continue;

                Console.WriteLine("  vtable debug: (method): " + method);

                string methodName = NameHelper.CreateMethodShortName(method);
                ParentMethodTable table = null;
                foreach (ParentMethodTable tbl in tables)
                {
                    if (tbl.HasMethod(methodName))
                    {
                        table = tbl;
                        break;
                    }
                }

                // Method is not in a table
                if (table == null)
                    continue;

                Console.WriteLine("  vtable debug: (table): " + table.GetMethodIndex(methodName));
                addMethodIndex(table.ContainingType, methodName, table.GetMethodIndex(methodName));
            }
        }

        private void addMethodIndex(TypeReference type, string methodName, int index)
        {
            Dictionary<string, int> dict;
            if (!mTable.TryGetValue(type, out dict))
            {
                dict = new Dictionary<string, int>();
                mTable.Add(type, dict);
            }

            dict.Add(methodName, index);
        }

        /// <summary>
        /// Gets the method index of a method
        /// </summary>
        /// <param name="method">The method</param>
        /// <returns>The index</returns>
        public int GetMethodIndex(MethodReference method)
        {
            string key = NameHelper.CreateMethodShortName(method);
            return GetMethodIndex(key);
        }

        /// <summary>
        /// Gets the method index of a method
        /// </summary>
        /// <param name="methodName">The method name</param>
        /// <returns>The index</returns>
        public int GetMethodIndex(string methodName)
        {
            //if (!mTable.ContainsKey(methodName))
                throw new InvalidOperationException("Could not get method index of: " + methodName + " (method not found)");

            //return mTable[methodName];
        }

        /// <summary>
        /// Checks if this table has an entry for a method
        /// </summary>
        /// <param name="methodName">The method</param>
        /// <returns>If the table has an entry for that method</returns>
        public bool HasMethod(MethodReference method)
        {
            string key = NameHelper.CreateMethodShortName(method);
            return HasMethod(key);
        }

        /// <summary>
        /// Checks if this table has an entry for a method
        /// </summary>
        /// <param name="methodName">The method name</param>
        /// <returns>If the table has an entry for that method</returns>
        public bool HasMethod(string methodName)
        {
            return false;
            //return mTable.ContainsKey(methodName);
        }

        /// <summary>
        /// Used to show the table (for debug)
        /// </summary>
        public void Dump()
        {
            Console.WriteLine("---");

            foreach (TypeReference key in mTable.Keys)
            {
                Console.WriteLine("  - vtable for: " + key);
                foreach (KeyValuePair<string, int> methodPair in mTable[key])
                {
                    Console.WriteLine(string.Format("    - {0} -> {1}", methodPair.Value, methodPair.Key));
                }
            }

            Console.WriteLine("---");
        }
    }
}
