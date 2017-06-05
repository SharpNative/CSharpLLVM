using Mono.Cecil;
using Swigged.LLVM;
using System;
using System.Collections.Generic;

namespace CSharpLLVM.Lookups
{
    class Lookup
    {
        private Dictionary<string, ValueRef> mFunctionLookup = new Dictionary<string, ValueRef>();
        private Dictionary<FieldReference, ValueRef> mStaticFieldLookup = new Dictionary<FieldReference, ValueRef>();
        private Dictionary<TypeReference, TypeRef> mTypeLookup = new Dictionary<TypeReference, TypeRef>();
        private Dictionary<TypeReference, List<FieldDefinition>> mFieldLookup = new Dictionary<TypeReference, List<FieldDefinition>>();
        private Dictionary<TypeReference, ParentMethodTable> mParentMethodTables = new Dictionary<TypeReference, ParentMethodTable>();
        private Dictionary<TypeReference, VTable> mVTables = new Dictionary<TypeReference, VTable>();

        private List<MethodDefinition> mCctors = new List<MethodDefinition>();

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
        /// Checks if we already have this static field
        /// </summary>
        /// <param name="field">The field</param>
        /// <returns>If we already have this static field</returns>
        public bool HasStaticField(FieldReference field)
        {
            return mStaticFieldLookup.ContainsKey(field);
        }

        /// <summary>
        /// Adds a .cctor
        /// </summary>
        /// <param name="method">The method</param>
        public void AddCctor(MethodDefinition method)
        {
            mCctors.Add(method);
        }

        /// <summary>
        /// Returns the static constructors (.cctor)
        /// </summary>
        /// <returns>An array containing the static constructors</returns>
        public MethodDefinition[] GetStaticConstructors()
        {
            return mCctors.ToArray();
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
        /// Gets the fields of a type including the inherited fields, a null indicates a "barrier"
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>The list of fields</returns>
        public List<FieldDefinition> GetFields(TypeReference type)
        {
            // Cached?
            if (mFieldLookup.ContainsKey(type))
                return mFieldLookup[type];

            List<FieldDefinition> fields = new List<FieldDefinition>();
            TypeDefinition typeDef = type.Resolve();
            if (typeDef.BaseType == null)
            {
                fields.Add(null);
                return fields;
            }

            TypeDefinition parent = typeDef.BaseType.Resolve();

            // First add parent fields, then our own fields
            if (parent.HasFields)
                fields.AddRange(GetFields(parent));

            fields.AddRange(typeDef.Fields);

            // Add barrier to indicate we are now in a different type
            fields.Add(null);

            // Add to cache
            mFieldLookup.Add(type, fields);

            return fields;
        }

        /// <summary>
        /// Gets the field index of a field
        /// </summary>
        /// <param name="field">The field</param>
        /// <returns>The field index</returns>
        public uint GetFieldIndex(FieldReference field)
        {
            List<FieldDefinition> fields = GetFields(field.DeclaringType);

            uint i = 0;
            foreach (FieldDefinition child in fields)
            {
                // Internal
                if (field.FullName[0] == '<')
                    continue;

                // Static fields don't count
                if (child.IsStatic)
                    continue;

                // Found
                if (field == child)
                    return i;

                i++;
            }

            throw new Exception("Could not find field index for: " + field);
        }

        /// <summary>
        /// Adds a parent method table
        /// </summary>
        /// <param name="type">The parent type</param>
        /// <param name="table">The method table</param>
        public void AddParentMethodTable(TypeReference type, ParentMethodTable table)
        {
            mParentMethodTables.Add(type, table);
        }

        /// <summary>
        /// Gets a parent method table of a type
        /// </summary>
        /// <param name="type">The parent type</param>
        /// <returns>The method table</returns>
        public ParentMethodTable GetParentMethodTable(TypeReference type)
        {
            if (!mParentMethodTables.ContainsKey(type))
                throw new InvalidOperationException("Could not get parent method table of type " + type);

            return mParentMethodTables[type];
        }

        /// <summary>
        /// Checks if a parent method table exists for a type
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>The method table</returns>
        public bool HasParentMethodTable(TypeReference type)
        {
            return mParentMethodTables.ContainsKey(type);
        }

        /// <summary>
        /// Adds a vtable
        /// </summary>
        /// <param name="type">The parent type</param>
        /// <param name="table">The method table</param>
        public void AddVTable(TypeReference type, VTable table)
        {
            mVTables.Add(type, table);
        }

        /// <summary>
        /// Gets a vtable
        /// </summary>
        /// <param name="type">The parent type</param>
        /// <returns>The vtable</returns>
        public VTable GetVTable(TypeReference type)
        {
            if (!mVTables.ContainsKey(type))
                throw new InvalidOperationException("Could not get parent method table of type " + type);

            return mVTables[type];
        }

        /// <summary>
        /// Checks if a vtable
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>The vtable</returns>
        public bool HasVTable(TypeReference type)
        {
            return mVTables.ContainsKey(type);
        }
    }
}
