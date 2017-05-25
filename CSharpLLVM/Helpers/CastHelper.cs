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

        /// <summary>
        /// Helps with the pointer casting so that both values are of the same type
        /// </summary>
        /// <param name="builder">The builder</param>
        /// <param name="value1">The first value</param>
        /// <param name="value2">The second value</param>
        /// <param name="isPtrVal1">If the first value was a pointer</param>
        /// <param name="isPtrVal2">If the second value was a pointer</param>
        public static void HelpPossiblePtrCast(BuilderRef builder, ref StackElement value1, ref StackElement value2, out bool isPtrVal1, out bool isPtrVal2)
        {
            isPtrVal1 = TypeHelper.IsPointer(value1);
            isPtrVal2 = TypeHelper.IsPointer(value2);

            if (isPtrVal1)
                value1.Value = LLVM.BuildPtrToInt(builder, value1.Value, TypeHelper.NativeIntType, "tmp");
            else
                value1.Value = LLVM.BuildIntCast(builder, value1.Value, TypeHelper.NativeIntType, "tmp");

            if (isPtrVal2)
                value2.Value = LLVM.BuildPtrToInt(builder, value2.Value, TypeHelper.NativeIntType, "tmp");
            else
                value2.Value = LLVM.BuildIntCast(builder, value2.Value, TypeHelper.NativeIntType, "tmp");
        }

        /// <summary>
        /// Helps int and ptr casting to a destination type
        /// </summary>
        /// <param name="builder">The builder</param>
        /// <param name="data">The data (int or pointer)</param>
        /// <param name="dataType">The data type</param>
        /// <param name="destType">The destination type</param>
        public static void HelpIntAndPtrCast(BuilderRef builder, ref ValueRef data, TypeRef dataType, TypeRef destType)
        {
            // Two cases: int to different size, or int to pointer
            TypeKind kind = LLVM.GetTypeKind(destType);

            // Convert to pointer
            if (kind == TypeKind.PointerTypeKind)
            {
                // Two cases: pointer to pointer, or int to int
                if (LLVM.GetTypeKind(dataType) == TypeKind.PointerTypeKind)
                    data = LLVM.BuildPointerCast(builder, data, destType, "tmpptr");
                else
                    data = LLVM.BuildIntToPtr(builder, data, destType, "tmpptr");
            }
            // Convert to int of different size
            else
            {
                data = LLVM.BuildIntCast(builder, data, destType, "tmpint");
            }
        }
    }
}
