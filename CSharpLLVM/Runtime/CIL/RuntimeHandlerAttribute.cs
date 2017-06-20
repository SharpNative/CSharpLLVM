using System;

namespace CSharpLLVM.Runtime.CIL
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    sealed class RuntimeHandlerAttribute : Attribute
    {
        public string TypeName { get; private set; }

        /// <summary>
        /// Creates a new RuntimeHandlerAttribute.
        /// </summary>
        /// <param name="type">The type that is handled by this class.</param>
        public RuntimeHandlerAttribute(Type type)
        {
            TypeName = type.FullName;
        }
    }
}
