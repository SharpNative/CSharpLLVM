using CSharpLLVM.Stack;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Swigged.LLVM;
using System;

namespace CSharpLLVM.Helpers
{
    static class TypeHelper
    {
        /// <summary>
        /// Gets a TypeRef from a TypeReference
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>The type</returns>
        public static TypeRef GetTypeRefFromType(TypeReference type)
        {
            switch (type.MetadataType)
            {
                case MetadataType.UInt64:
                case MetadataType.Int64:
                    return LLVM.Int64Type();

                case MetadataType.UInt32:
                case MetadataType.Int32:
                    return LLVM.Int32Type();

                case MetadataType.UInt16:
                case MetadataType.Int16:
                    return LLVM.Int16Type();

                case MetadataType.Byte:
                case MetadataType.SByte:
                    return LLVM.Int8Type();

                case MetadataType.String:
                    return LLVM.PointerType(LLVM.Int8Type(), 0);

                case MetadataType.Boolean:
                    return LLVM.Int1Type();

                case MetadataType.Array:
                    return LLVM.PointerType(GetTypeRefFromType(type.GetElementType()), 0);

                case MetadataType.Void:
                    return LLVM.VoidType();

                case MetadataType.Single:
                    return LLVM.FloatType();

                case MetadataType.Double:
                    return LLVM.DoubleType();

                default:
                    throw new InvalidOperationException("Invalid meta data type to get type from: " + type.MetadataType);
            }
        }

        /// <summary>
        /// Gets a TypeRef from a Code
        /// </summary>
        /// <param name="code">The code</param>
        /// <returns>The type</returns>
        public static TypeRef GetTypeRefFromConv(Code code)
        {
            switch(code)
            {
                case Code.Conv_I1:
                case Code.Conv_Ovf_I1:
                case Code.Conv_Ovf_I1_Un:
                    return LLVM.Int8Type();

                case Code.Conv_I2:
                case Code.Conv_Ovf_I2:
                case Code.Conv_Ovf_I2_Un:
                    return LLVM.Int16Type();

                case Code.Conv_I4:
                case Code.Conv_Ovf_I4:
                case Code.Conv_Ovf_I4_Un:
                    return LLVM.Int32Type();

                case Code.Conv_I8:
                case Code.Conv_Ovf_I8:
                case Code.Conv_Ovf_I8_Un:
                    return LLVM.Int64Type();

                case Code.Conv_R4:
                    return LLVM.FloatType();

                case Code.Conv_R8:
                    return LLVM.DoubleType();

                default:
                    throw new InvalidOperationException("Invalid code to get type from: " + code);
            }
        }

        /// <summary>
        /// Checks if a stack element is a floating point number (float or double)
        /// </summary>
        /// <param name="element">The element</param>
        /// <returns>If the stack element is floating point</returns>
        public static bool IsFloatingPoint(StackElement element)
        {
            return (element.Type == LLVM.FloatType() || element.Type == LLVM.DoubleType());
        }
    }
}
