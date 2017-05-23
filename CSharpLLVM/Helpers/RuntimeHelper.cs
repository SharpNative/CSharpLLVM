using Swigged.LLVM;

namespace CSharpLLVM.Helpers
{
    static class RuntimeHelper
    {
        public static ValueRef Calloc { get; private set; }

        /// <summary>
        /// Initializes runtime functions
        /// </summary>
        /// <param name="module">The module</param>
        public static void Init(ModuleRef module)
        {
            TypeRef callocType = LLVM.FunctionType(TypeHelper.VoidPtr, new TypeRef[] { TypeHelper.NativeIntType, TypeHelper.NativeIntType }, false);
            Calloc = LLVM.AddFunction(module, "calloc", callocType);
            LLVM.SetLinkage(Calloc, Linkage.ExternalLinkage);
        }
    }
}
