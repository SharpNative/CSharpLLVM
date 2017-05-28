using CSharpLLVM.Helpers;
using Mono.Cecil;
using Swigged.LLVM;
using System;
using System.Collections.Generic;

namespace CSharpLLVM.Compiler
{
    class TypeCompiler
    {
        private Compiler mCompiler;
        private Lookup mLookup;

        /// <summary>
        /// Creates a new TypeCompiler
        /// </summary>
        /// <param name="compiler">The compiler</param>
        /// <param name="lookup">The lookup</param>
        public TypeCompiler(Compiler compiler, Lookup lookup)
        {
            mCompiler = compiler;
            mLookup = lookup;
        }

        /// <summary>
        /// Gets the fields of a type including the inherited fields
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>The list of fields</returns>
        private List<FieldDefinition> getFields(TypeDefinition type)
        {
            List<FieldDefinition> fields = new List<FieldDefinition>();
            TypeDefinition parent = type.BaseType.Resolve();

            // First add parent fields, then our own fields
            if(parent.HasFields)
                fields.AddRange(getFields(parent));

            fields.AddRange(type.Fields);

            return fields;
        }

        /// <summary>
        /// Compiles a type
        /// </summary>
        /// <param name="type">The type</param>
        public void Compile(TypeDefinition type)
        {
            // Internal
            if (type.FullName == "<Module>")
                return;
            
            bool isStruct = (!type.IsEnum && type.IsValueType);
            bool isEnum = type.IsEnum;
            bool isClass = (!isStruct && !isStruct);
            ConsoleColor color = isStruct ? ConsoleColor.DarkCyan : isEnum ? ConsoleColor.DarkGreen : ConsoleColor.Cyan;

            // Log
            Console.ForegroundColor = color;
            Console.WriteLine(string.Format("Compiling type {0}", type.FullName));
            Console.ForegroundColor = ConsoleColor.Gray;

            // Enums are treated as 32-bit ints
            if (isEnum)
            {
                mLookup.AddType(type, TypeHelper.Int32);
            }
            // Structs and classes
            else
            {
                // Create struct for this type
                TypeRef data = LLVM.StructCreateNamed(mCompiler.ModuleContext, NameHelper.CreateTypeName(type));
                mLookup.AddType(type, data);
                List<TypeRef> structData = new List<TypeRef>();
                List<FieldDefinition> fields = getFields(type);

                // Fields
                foreach (FieldDefinition field in fields)
                {
                    // Internal
                    if (field.FullName[0] == '<')
                        continue;

                    TypeRef fieldType = TypeHelper.GetTypeRefFromType(field.FieldType);

                    // Static field
                    if (field.IsStatic)
                    {
                        ValueRef val = LLVM.AddGlobal(mCompiler.Module, fieldType, NameHelper.CreateFieldName(field.FullName));

                        // Note: the initializer may be changed later if the compiler sees that it can be constant
                        LLVM.SetInitializer(val, LLVM.ConstNull(fieldType));
                        mLookup.AddStaticField(field, val);
                    }
                    // Field for type instance
                    else
                    {
                        structData.Add(fieldType);
                    }
                }

                // Packing?
                bool packed = (type.PackingSize != -1);
                if (type.PackingSize != 1 && type.PackingSize != -1)
                {
                    throw new NotImplementedException("The packing size " + type.PackingSize + " is not implemented");
                }

                // Set struct data
                LLVM.StructSetBody(data, structData.ToArray(), packed);
            }
        }
    }
}
