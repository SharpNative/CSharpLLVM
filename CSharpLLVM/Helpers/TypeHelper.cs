using CSharpLLVM.Compilation;
using CSharpLLVM.Lookups;
using CSharpLLVM.Stack;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Swigged.LLVM;
using System;
using System.Collections.Generic;

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

        private static Compiler mCompiler;
        private static Lookup mLookup;

        /// <summary>
        /// Initializes common types.
        /// </summary>
        /// <param name="targetData">Target data.</param>
        /// <param name="compiler">The compiler.</param>
        public static void Init(TargetDataRef targetData, Compiler compiler)
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

            mCompiler = compiler;
            mLookup = compiler.Lookup;
        }

        /// <summary>
        /// Gets a TypeRef from a TypeReference.
        /// </summary>
        /// <param name="type">The TypeReference.</param>
        /// <returns>The TypeRef.</returns>
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

                case MetadataType.IntPtr:
                case MetadataType.UIntPtr:
                    return NativeIntType;

                case MetadataType.Array:
                    ArrayType array = (ArrayType)type;
                    return LLVM.PointerType(GetTypeRefFromType(array.ElementType), 0);

                case MetadataType.Pointer:
                    PointerType ptr = (PointerType)type;
                    return LLVM.PointerType(GetTypeRefFromType(ptr.ElementType), 0);

                case MetadataType.ByReference:
                    ByReferenceType byRef = (ByReferenceType)type;
                    return LLVM.PointerType(GetTypeRefFromType(byRef.ElementType), 0);

                case MetadataType.Pinned:
                    PinnedType pinned = (PinnedType)type;
                    return GetTypeRefFromType(pinned.ElementType);

                case MetadataType.Class:
                    return LLVM.PointerType(mLookup.GetTypeRef(type), 0);

                case MetadataType.ValueType:
                    return mLookup.GetTypeRef(type);

                case MetadataType.GenericInstance:
                case MetadataType.Var:
                case MetadataType.Object:
                    return VoidPtr;

                case MetadataType.RequiredModifier:
                    RequiredModifierType requiredModifier = (RequiredModifierType)type;
                    return GetTypeRefFromType(requiredModifier.ElementType);

                case MetadataType.OptionalModifier:
                    OptionalModifierType optionalModifier = (OptionalModifierType)type;
                    return GetTypeRefFromType(optionalModifier.ElementType);

                default:
                    throw new InvalidOperationException("Invalid meta data type to get type from: " + type.MetadataType);
            }
        }

        /// <summary>
        /// Gets a TypeRef from a Type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The TypeRef.</returns>
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
        /// Gets a TypeRef from a Code.Conv*.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <returns>The TypeRef.</returns>
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
        /// Gets a TypeRef from a Code.Stelem*.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <returns>The TypeRef.</returns>
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
        /// Gets a TypeRef from a Code.Stind* or Code.Ldind*.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <returns>The TypeRef.</returns>
        public static TypeRef GetTypeRefFromStOrLdind(Code code)
        {
            switch (code)
            {
                case Code.Stind_I:
                case Code.Ldind_I:
                    return NativeIntType;

                case Code.Stind_I1:
                case Code.Ldind_I1:
                case Code.Ldind_U1:
                    return Int8;

                case Code.Stind_I2:
                case Code.Ldind_I2:
                case Code.Ldind_U2:
                    return Int16;

                case Code.Stind_I4:
                case Code.Ldind_I4:
                case Code.Ldind_U4:
                    return Int32;

                case Code.Stind_I8:
                case Code.Ldind_I8:
                    return Int64;

                case Code.Stind_R4:
                case Code.Ldind_R4:
                    return Float;

                case Code.Stind_R8:
                case Code.Ldind_R8:
                    return Double;

                default:
                    throw new InvalidOperationException("Invalid code to get type from: " + code);
            }
        }

        /// <summary>
        /// Get basic type from TypeRef.
        /// </summary>
        /// <param name="compiler">The compiler instance</param>
        /// <param name="typeRef">The TypeRef.</param>
        /// <returns>The basic TypeReference.</returns>
        public static TypeReference GetBasicTypeFromTypeRef(TypeRef typeRef)
        {
            Type type = null;
            if (typeRef == Int8)
                type = typeof(byte);
            else if (typeRef == Int16)
                type = typeof(short);
            else if (typeRef == Int32)
                type = typeof(int);
            else if (typeRef == Int64)
                type = typeof(long);
            else if (typeRef == String)
                type = typeof(string);
            else if (typeRef == Float)
                type = typeof(float);
            else if (typeRef == Double)
                type = typeof(double);
            else if (typeRef == Boolean)
                type = typeof(bool);
            else
                throw new InvalidOperationException("Could not get basic type from typeref: " + typeRef);

            return type.GetTypeReference();
        }

        /// <summary>
        /// Returns true if a stack element is a floating point number (float or double).
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>If the stack element is floating point.</returns>
        public static bool IsFloatingPoint(StackElement element)
        {
            return (element.Type == Float || element.Type == Double);
        }

        /// <summary>
        /// Returns true if a stack element is a pointer.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>If the stack element is a pointer.</returns>
        public static bool IsPointer(StackElement element)
        {
            return (LLVM.GetTypeKind(element.Type) == TypeKind.PointerTypeKind);
        }

        /// <summary>
        /// Returns true if a type inherits the other type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="baseType">The base type.</param>
        /// <returns>If the type inherits from the base type.</returns>
        public static bool InheritsFrom(TypeDefinition type, TypeDefinition baseType)
        {
            if (baseType.IsInterface)
                return type.Interfaces.Contains(baseType);

            TypeDefinition current = type;
            while (current != null)
            {
                if (current.BaseType == baseType)
                    return true;
                else if (current.BaseType == null)
                    return false;

                current = current.BaseType.Resolve();
            }

            return false;
        }

        /// <summary>
        /// Returns an array of all the inherited interfaces, also the ones from the base types.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The array of all inherited interfaces.</returns>
        public static TypeDefinition[] GetAllInterfaces(TypeDefinition type)
        {
            List<TypeDefinition> list = new List<TypeDefinition>();
            
            // Add interfaces.
            TypeDefinition current = type;
            while (current != null)
            {
                if (current.HasInterfaces)
                {
                    foreach (TypeDefinition interfaceType in current.Interfaces)
                    {
                        list.Add(interfaceType);
                    }
                }

                if (current.BaseType == null)
                    break;

                current = current.BaseType.Resolve();
            }

            return list.ToArray();
        }

        /// <summary>
        /// Gets a type reference from a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The TypeReference.</returns>
        public static TypeReference GetTypeReference(this Type type)
        {
            return mCompiler.AssemblyDef.MainModule.Import(type);
        }
    }
}
