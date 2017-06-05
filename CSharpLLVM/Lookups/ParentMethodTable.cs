using CSharpLLVM.Helpers;
using Mono.Cecil;
using Mono.Collections.Generic;
using Swigged.LLVM;
using System;
using System.Collections.Generic;

namespace CSharpLLVM.Lookups
{
    class ParentMethodTable : IMethodTable
    {
        private TypeDefinition mType;
        private Lookup mLookup;
        private Dictionary<string, int> mTable = new Dictionary<string, int>();

        public TypeRef Type { get; private set; }
        public TypeReference ContainingType { get { return mType; } }
        public int MethodCount { get { return mTable.Count; } }

        /// <summary>
        /// Creates a new ParentMethodTable
        /// </summary>
        /// <param name="type">The type</param>
        /// <param name="lookup">The lookup</param>
        public ParentMethodTable(TypeDefinition type, Lookup lookup)
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
            // Gather methods of this interface and the parents
            Collection<MethodDefinition> methods = mType.Methods;
            int index = 0;

            // First add the parent methods with their indices
            for (int i = 0; i < methods.Count; i++)
            {
                // A constructor cannot be called virtually
                if (methods[i].Name == ".ctor")
                    continue;

                string shortName = NameHelper.CreateMethodShortName(methods[i]);
                mTable.Add(shortName, index++);
            }

            Type = LLVM.ArrayType(TypeHelper.NativeIntType, (uint)MethodCount);
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
            if (!mTable.ContainsKey(methodName))
                throw new InvalidOperationException("Could not get method index of: " + methodName + " (method not found)");

            return mTable[methodName];
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
            return mTable.ContainsKey(methodName);
        }

        /// <summary>
        /// Used to show the table (for debug)
        /// </summary>
        public void Dump()
        {
            Console.WriteLine("---");
            foreach (KeyValuePair<string, int> pair in mTable)
            {
                Console.WriteLine(string.Format(" - {0} -> {1}", pair.Key, pair.Value));
            }
            Console.WriteLine("---");
        }
    }
}
