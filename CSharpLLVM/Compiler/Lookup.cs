using Mono.Cecil;
using Swigged.LLVM;
using System.Collections.Generic;

namespace CSharpLLVM.Compiler
{
    class Lookup
    {
        private Dictionary<string, ValueRef> m_functionLookup = new Dictionary<string, ValueRef>();
        private Dictionary<FieldReference, ValueRef> m_staticFieldLookup = new Dictionary<FieldReference, ValueRef>();
        private List<MethodDefinition> m_cctors = new List<MethodDefinition>();

        /// <summary>
        /// Gets a function
        /// </summary>
        /// <param name="name">The name</param>
        /// <returns>The function</returns>
        public ValueRef? GetFunction(string name)
        {
            if (m_functionLookup.ContainsKey(name))
                return m_functionLookup[name];

            return null;
        }

        /// <summary>
        /// Adds a function
        /// </summary>
        /// <param name="name">The name</param>
        /// <param name="function">The function</param>
        public void AddFunction(string name, ValueRef function)
        {
            m_functionLookup.Add(name, function);
        }

        /// <summary>
        /// Gets a static field
        /// </summary>
        /// <param name="field">The static field</param>
        /// <returns>The field</returns>
        public ValueRef? GetStaticField(FieldReference field)
        {
            if (m_staticFieldLookup.ContainsKey(field))
                return m_staticFieldLookup[field];

            return null;
        }

        /// <summary>
        /// Adds a static field
        /// </summary>
        /// <param name="field">The field</param>
        /// <param name="val">The value</param>
        public void AddStaticField(FieldReference field, ValueRef val)
        {
            m_staticFieldLookup.Add(field, val);
        }

        /// <summary>
        /// Adds a .cctor
        /// </summary>
        /// <param name="method">The method</param>
        public void AddCctor(MethodDefinition method)
        {
            m_cctors.Add(method);
        }

        /// <summary>
        /// Returns the static constructors (.cctor)
        /// </summary>
        /// <returns>An array containing the static constructors</returns>
        public MethodDefinition[] GetStaticConstructors()
        {
            return m_cctors.ToArray();
        }
    }
}
