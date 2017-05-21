using Swigged.LLVM;

namespace CSharpLLVM.Stack
{
    struct StackElement
    {
        public TypeRef Type;
        public ValueRef Value;

        /// <summary>
        /// Creates a new StackElement
        /// </summary>
        /// <param name="value">The value</param>
        public StackElement(ValueRef value)
        {
            Type = LLVM.TypeOf(value);
            Value = value;
        }
    }
}
