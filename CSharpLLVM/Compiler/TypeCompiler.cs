using CSharpLLVM.Helpers;
using Mono.Cecil;
using Swigged.LLVM;
using System;

namespace CSharpLLVM.Compiler
{
    class TypeCompiler
    {
        private Compiler mcompiler;
        private Lookup mlookup;

        /// <summary>
        /// Creates a new TypeCompiler
        /// </summary>
        /// <param name="compiler">The compiler</param>
        /// <param name="lookup">The lookup</param>
        public TypeCompiler(Compiler compiler, Lookup lookup)
        {
            mcompiler = compiler;
            mlookup = lookup;
        }

        /// <summary>
        /// Compiles a type
        /// </summary>
        /// <param name="type">The type</param>
        public void Compile(TypeDefinition type)
        {
            bool isStruct = (!type.IsEnum && type.IsValueType);
            bool isEnum = type.IsEnum;
            bool isClass = (!isStruct && !isStruct);

            ConsoleColor color = isStruct ? ConsoleColor.Cyan : isEnum ? ConsoleColor.DarkGreen : ConsoleColor.DarkCyan;

            // Log
            Console.ForegroundColor = color;
            Console.WriteLine(string.Format("Compiling type {0}", type.FullName));
            Console.ForegroundColor = ConsoleColor.Gray;

            // Enums are treated as 32-bit ints
            if (isEnum)
            {
                mlookup.AddType(type, TypeHelper.Int32);
            }
            // Structs and classes
            else
            {
                // Fields
                foreach (FieldDefinition field in type.Fields)
                {
                    if (field.FullName[0] == '<')
                        continue;

                    if (field.IsStatic)
                    {
                        TypeRef fieldType = TypeHelper.GetTypeRefFromType(field.FieldType);
                        ValueRef val = LLVM.AddGlobal(mcompiler.Module, fieldType, NameHelper.CreateFieldName(field.FullName));

                        // Note: the initializer may be changed later if the compiler sees that it can be constant
                        LLVM.SetInitializer(val, LLVM.ConstNull(fieldType));
                        mlookup.AddStaticField(field, val);
                    }
                }
            }
        }
    }
}
