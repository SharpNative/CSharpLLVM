using Swigged.LLVM;

namespace CSharpLLVM.Helpers
{
    static class RuntimeHelper
    {
        public static ValueRef Newarr { get; private set; }
        public static ValueRef Memcpy { get; private set; }
        public static ValueRef Memset { get; private set; }

        /// <summary>
        /// Initializes runtime functions
        /// </summary>
        /// <param name="module">The module</param>
        public static void ImportFunctions(ModuleRef module)
        {
            // Newarr
            TypeRef newarrType = LLVM.FunctionType(TypeHelper.VoidPtr, new TypeRef[] { TypeHelper.Int32, TypeHelper.NativeIntType }, false);
            Newarr = LLVM.AddFunction(module, "newarr", newarrType);
            LLVM.SetLinkage(Newarr, Linkage.ExternalLinkage);

            // Memcpy
            TypeRef memcpyType = LLVM.FunctionType(TypeHelper.VoidPtr, new TypeRef[] { TypeHelper.VoidPtr, TypeHelper.VoidPtr, TypeHelper.NativeIntType }, false);
            Memcpy = LLVM.AddFunction(module, "memcpy", memcpyType);
            LLVM.SetLinkage(Memcpy, Linkage.ExternalLinkage);

            // Memcpy
            TypeRef memsetType = LLVM.FunctionType(TypeHelper.VoidPtr, new TypeRef[] { TypeHelper.VoidPtr, TypeHelper.Int32, TypeHelper.NativeIntType }, false);
            Memset = LLVM.AddFunction(module, "memset", memsetType);
            LLVM.SetLinkage(Memset, Linkage.ExternalLinkage);
        }
    }
}
