using CSharpLLVM.Helpers;
using Mono.Cecil;
using Swigged.LLVM;
using System;

namespace CSharpLLVM.Compiler
{
    class TypeCompiler
    {
        private Compiler m_compiler;
        private Lookup m_lookup;

        /// <summary>
        /// Creates a new TypeCompiler
        /// </summary>
        /// <param name="compiler">The compiler</param>
        /// <param name="lookup">The lookup</param>
        public TypeCompiler(Compiler compiler, Lookup lookup)
        {
            m_compiler = compiler;
            m_lookup = lookup;
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

            // Fields
            foreach (FieldDefinition field in type.Fields)
            {
                if (field.FullName[0] == '<')
                    continue;

                if (field.IsStatic)
                {
                    TypeRef fieldType = TypeHelper.GetTypeRefFromType(field.FieldType);
                    ValueRef val = LLVM.AddGlobal(m_compiler.Module, fieldType, NameHelper.CreateFieldName(field.FullName));

                    // Note: the initializer may be changed later if the compiler sees that it can be constant
                    LLVM.SetInitializer(val, LLVM.ConstNull(fieldType));
                    m_lookup.AddStaticField(field, val);
                }
            }
        }
    }
}
