using System;

namespace CSharpLLVM.Runtime.CIL
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    sealed class MethodHandlerAttribute : Attribute
    {
        public string MethodName { get; private set; }

        /// <summary>
        /// Creates a new MethodHandlerAttribute.
        /// </summary>
        /// <param name="name">The method name.</param>
        public MethodHandlerAttribute(string name)
        {
            MethodName = name;
        }
    }
}
