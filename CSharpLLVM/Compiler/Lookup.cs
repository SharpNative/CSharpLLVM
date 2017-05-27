using Mono.Cecil;
using Swigged.LLVM;
using System;
using System.Collections.Generic;

namespace CSharpLLVM.Compiler
{
    class Lookup
    {
        private Dictionary<string, ValueRef> mFunctionLookup = new Dictionary<string, ValueRef>();
        private Dictionary<FieldReference, ValueRef> mStaticFieldLookup = new Dictionary<FieldReference, ValueRef>();
        private Dictionary<TypeReference, TypeRef> mTypeLookup = new Dictionary<TypeReference, TypeRef>();

        private List<MethodDefinition> mcctors = new List<MethodDefinition>();

        /// <summary>
        /// Gets a function
        /// </summary>
        /// <param name="name">The name</param>
        /// <returns>The function</returns>
        public ValueRef? GetFunction(string name)
        {
            if (mFunctionLookup.ContainsKey(name))
                return mFunctionLookup[name];

            return null;
        }

        /// <summary>
        /// Adds a function
        /// </summary>
        /// <param name="name">The name</param>
        /// <param name="function">The function</param>
        public void AddFunction(string name, ValueRef function)
        {
            mFunctionLookup.Add(name, function);
        }

        /// <summary>
        /// Gets a static field
        /// </summary>
        /// <param name="field">The static field</param>
        /// <returns>The field</returns>
        public ValueRef? GetStaticField(FieldReference field)
        {
            if (mStaticFieldLookup.ContainsKey(field))
                return mStaticFieldLookup[field];

            return null;
        }

        /// <summary>
        /// Adds a static field
        /// </summary>
        /// <param name="field">The field</param>
        /// <param name="val">The value</param>
        public void AddStaticField(FieldReference field, ValueRef val)
        {
            mStaticFieldLookup.Add(field, val);
        }

        /// <summary>
        /// Adds a .cctor
        /// </summary>
        /// <param name="method">The method</param>
        public void AddCctor(MethodDefinition method)
        {
            mcctors.Add(method);
        }

        /// <summary>
        /// Adds a type
        /// </summary>
        /// <param name="type">The type</param>
        /// <param name="typeRef">The type</param>
        public void AddType(TypeReference type, TypeRef typeRef)
        {
            mTypeLookup.Add(type, typeRef);
        }

        /// <summary>
        /// Gets a type
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>The type</returns>
        public TypeRef GetTypeRef(TypeReference type)
        {
            if (!mTypeLookup.ContainsKey(type))
                throw new InvalidOperationException("Type " + type + " not found");

            return mTypeLookup[type];
        }

        /// <summary>
        /// Returns the static constructors (.cctor)
        /// </summary>
        /// <returns>An array containing the static constructors</returns>
        public MethodDefinition[] GetStaticConstructors()
        {
            return mcctors.ToArray();
        }
    }
}
