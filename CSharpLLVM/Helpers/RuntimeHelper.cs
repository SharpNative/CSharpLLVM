using Swigged.LLVM;

namespace CSharpLLVM.Helpers
{
    static class RuntimeHelper
    {
        public static ValueRef Newarr { get; private set; }

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
        }
    }
}
