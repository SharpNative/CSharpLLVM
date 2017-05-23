using CSharpLLVM.Stack;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Swigged.LLVM;
using System;

namespace CSharpLLVM.Helpers
{
    static class TypeHelper
    {
        public static TypeRef Int64 { get; private set; }
        public static TypeRef Int32 { get; private set; }
        public static TypeRef Int16 { get; private set; }
        public static TypeRef Int8 { get; private set; }
        public static TypeRef Boolean { get; private set; }
        public static TypeRef String { get; private set; }
        public static TypeRef Void { get; private set; }
        public static TypeRef Float { get; private set; }
        public static TypeRef Double { get; private set; }
        public static TypeRef VoidPtr { get; private set; }
        public static TypeRef NativeIntType { get; private set; }

        public static ValueRef True { get; private set; }
        public static ValueRef False { get; private set; }

        public static uint IntPtrSize { get; private set; }

        /// <summary>
        /// Initializes common types
        /// </summary>
        /// <param name="targetData">Target data</param>
        public static void Init(TargetDataRef targetData)
        {
            Int64 = LLVM.Int64Type();
            Int32 = LLVM.Int32Type();
            Int16 = LLVM.Int16Type();
            Int8 = LLVM.Int8Type();
            Boolean = LLVM.Int1Type();
            String = LLVM.PointerType(LLVM.Int8Type(), 0);
            Void = LLVM.VoidType();
            Float = LLVM.FloatType();
            Double = LLVM.DoubleType();

            True = LLVM.ConstInt(Boolean, 1, false);
            False = LLVM.ConstInt(Boolean, 0, false);

            VoidPtr = LLVM.PointerType(LLVM.VoidType(), 0);
            IntPtrSize = (uint)LLVM.ABISizeOfType(targetData, VoidPtr);
            NativeIntType = LLVM.IntType(IntPtrSize * 8);
        }

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
                    return Int64;

                case MetadataType.UInt32:
                case MetadataType.Int32:
                    return Int32;

                case MetadataType.UInt16:
                case MetadataType.Int16:
                    return Int16;

                case MetadataType.Byte:
                case MetadataType.SByte:
                    return Int8;

                case MetadataType.Char:
                    return Int8;

                case MetadataType.String:
                    return String;

                case MetadataType.Boolean:
                    return Boolean;

                case MetadataType.Array:
                {
                    ArrayType array = (ArrayType)type;
                    return LLVM.PointerType(GetTypeRefFromType(array.ElementType), 0);
                }

                case MetadataType.Void:
                    return Void;

                case MetadataType.Single:
                    return Float;

                case MetadataType.Double:
                    return Double;

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
            switch (code)
            {
                case Code.Conv_U:
                case Code.Conv_I:
                case Code.Conv_Ovf_I:
                case Code.Conv_Ovf_I_Un:
                    return NativeIntType;

                case Code.Conv_U1:
                case Code.Conv_I1:
                case Code.Conv_Ovf_I1:
                case Code.Conv_Ovf_I1_Un:
                    return Int8;

                case Code.Conv_U2:
                case Code.Conv_I2:
                case Code.Conv_Ovf_I2:
                case Code.Conv_Ovf_I2_Un:
                    return Int16;

                case Code.Conv_U4:
                case Code.Conv_I4:
                case Code.Conv_Ovf_I4:
                case Code.Conv_Ovf_I4_Un:
                    return Int32;

                case Code.Conv_U8:
                case Code.Conv_I8:
                case Code.Conv_Ovf_I8:
                case Code.Conv_Ovf_I8_Un:
                    return Int64;

                case Code.Conv_R4:
                    return Float;

                case Code.Conv_R8:
                    return Double;

                default:
                    throw new InvalidOperationException("Invalid code to get type from: " + code);
            }
        }

        /// <summary>
        /// Gets a TypeRef from a Code
        /// </summary>
        /// <param name="code">The code</param>
        /// <returns>The type</returns>
        public static TypeRef GetTypeRefFromStelem(Code code)
        {
            switch (code)
            {
                case Code.Stelem_I:
                    return NativeIntType;

                case Code.Stelem_I1:
                    return Int8;

                case Code.Stelem_I2:
                    return Int16;

                case Code.Stelem_I4:
                    return Int32;

                case Code.Stelem_I8:
                    return Int64;

                case Code.Stelem_R4:
                    return Float;

                case Code.Stelem_R8:
                    return Double;

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
            return (element.Type == Float || element.Type == Double);
        }
    }
}
