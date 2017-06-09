using Mono.Cecil;
using Swigged.LLVM;

namespace CSharpLLVM.Stack
{
    class StackElement
    {
        public TypeRef Type;
        public TypeReference ILType;
        public ValueRef Value;

        /// <summary>
        /// Creates a new StackElement
        /// </summary>
        /// <param name="value">The value</param>
        /// <param name="ilType">The IL type</param>
        public StackElement(ValueRef value, TypeReference ilType)
        {
            Type = LLVM.TypeOf(value);
            ILType = ilType;
            Value = value;
        }

        /// <summary>
        /// Creates a new StackElement
        /// </summary>
        /// <param name="value">The value</param>
        /// <param name="ilType">The IL type</param>
        /// <param name="type">The type</param>
        public StackElement(ValueRef value, TypeReference ilType, TypeRef type)
        {
            Type = type;
            ILType = ilType;
            Value = value;
        }

        /// <summary>
        /// Creates a new StackElement from an existing one
        /// </summary>
        /// <param name="other">The existing one</param>
        public StackElement(StackElement other)
        {
            Type = other.Type;
            ILType = other.ILType;
            Value = other.Value;
        }
    }
}
