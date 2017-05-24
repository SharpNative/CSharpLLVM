using CSharpLLVM.Stack;
using Swigged.LLVM;

namespace CSharpLLVM.Helpers
{
    static class CastHelper
    {
        /// <summary>
        /// Helps with the int casting so that both values are of the same type
        /// </summary>
        /// <param name="builder">The builder</param>
        /// <param name="value1">The first value</param>
        /// <param name="value2">The second value</param>
        public static void HelpIntCast(BuilderRef builder, ref StackElement value1, ref StackElement value2)
        {
            if (value1.Type != value2.Type)
            {
                value2.Value = LLVM.BuildIntCast(builder, value2.Value, value1.Type, "tmp");
            }
        }
    }
}
