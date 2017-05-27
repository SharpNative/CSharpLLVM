using CSharpLLVM.Stack;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Swigged.LLVM;
using System;
using System.Reflection;

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

                case MetadataType.Void:
                    return Void;

                case MetadataType.Single:
                    return Float;

                case MetadataType.Double:
                    return Double;

                case MetadataType.Array:
                    ArrayType array = (ArrayType)type;
                    return LLVM.PointerType(GetTypeRefFromType(array.ElementType), 0);

                case MetadataType.Pointer:
                    PointerType ptr = (PointerType)type;
                    return LLVM.PointerType(GetTypeRefFromType(ptr.ElementType), 0);

                case MetadataType.ByReference:
                    ByReferenceType byref = (ByReferenceType)type;
                    return LLVM.PointerType(GetTypeRefFromType(byref.ElementType), 0);

                case MetadataType.Pinned:
                    PinnedType pinned = (PinnedType)type;
                    return GetTypeRefFromType(pinned.ElementType);

                default:
                    throw new InvalidOperationException("Invalid meta data type to get type from: " + type.MetadataType);
            }
        }

        /// <summary>
        /// Gets a TypeRef from a Type
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>The type</returns>
        public static TypeRef GetTypeRefFromType(Type type)
        {
            if (type.IsPointer || type.IsArray)
            {
                return LLVM.PointerType(GetTypeRefFromType(type.GetElementType()), 0);
            }

            if (type == typeof(byte))
                return Int8;
            else if (type == typeof(short))
                return Int16;
            else if (type == typeof(int))
                return Int32;
            else if (type == typeof(long))
                return Int64;
            else if (type == typeof(float))
                return Float;
            else if (type == typeof(double))
                return Double;
            else if (type == typeof(string))
                return String;
            else if (type == typeof(bool))
                return Boolean;

            throw new InvalidOperationException("Can't get TypeRef from Type: " + type);
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
        /// Gets a TypeRef from a Code
        /// </summary>
        /// <param name="code">The code</param>
        /// <returns>The type</returns>
        public static TypeRef GetTypeRefFromStind(Code code)
        {
            switch (code)
            {
                case Code.Stind_I:
                    return NativeIntType;

                case Code.Stind_I1:
                    return Int8;

                case Code.Stind_I2:
                    return Int16;

                case Code.Stind_I4:
                    return Int32;

                case Code.Stind_I8:
                    return Int64;

                case Code.Stind_R4:
                    return Float;

                case Code.Stind_R8:
                    return Double;

                default:
                    throw new InvalidOperationException("Invalid code to get type from: " + code);
            }
        }

        /// <summary>
        /// Get basic type from typeref
        /// </summary>
        /// <param name="typeRef">The typeref</param>
        /// <returns>The basic type</returns>
        public static Type GetBasicTypeFromTypeRef(TypeRef typeRef)
        {
            if (typeRef == Int8)
                return typeof(byte);
            else if (typeRef == Int16)
                return typeof(short);
            else if (typeRef == Int32)
                return typeof(int);
            else if (typeRef == Int64)
                return typeof(long);
            else if (typeRef == String)
                return typeof(string);
            else if (typeRef == Float)
                return typeof(float);
            else if (typeRef == Double)
                return typeof(double);
            else if (typeRef == Boolean)
                return typeof(bool);
            
            throw new InvalidOperationException("Could not get basic type from typeref: " + typeRef);
        }

        /// <summary>
        /// Gets a type from a type reference
        /// </summary>
        /// <param name="compiler">The compiler</param>
        /// <param name="typeRef">The type reference</param>
        /// <returns>The type</returns>
        public static Type GetTypeFromTypeReference(Compiler.Compiler compiler, TypeReference typeRef)
        {
            // TODO: make this nicer
            if (typeRef.FullName.StartsWith("System"))
                return Assembly.Load("mscorlib").GetType(typeRef.FullName);
            return compiler.Asm.GetType(typeRef.FullName);
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

        /// <summary>
        /// Checks if a stack element is a pointer
        /// </summary>
        /// <param name="element">The element</param>
        /// <returns>If the stack element is a pointer</returns>
        public static bool IsPointer(StackElement element)
        {
            return (LLVM.GetTypeKind(element.Type) == TypeKind.PointerTypeKind);
        }
    }
}
