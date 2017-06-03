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
            bool isInterface = type.IsInterface;
            bool isClass = (!isStruct && !isInterface);
            
            // Log
            Console.ForegroundColor = isStruct ? ConsoleColor.DarkCyan : isEnum ? ConsoleColor.DarkGreen : isInterface ? ConsoleColor.DarkMagenta : ConsoleColor.Cyan; ;
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
                // VTable
                VTable vtable = new VTable(mCompiler, type);
                mLookup.AddVTable(vtable);
                vtable.Create();
                vtable.Dump();

                // Create struct for this type
                TypeRef data = LLVM.StructCreateNamed(mCompiler.ModuleContext, NameHelper.CreateTypeName(type));
                mLookup.AddType(type, data);
                List<TypeRef> structData = new List<TypeRef>();
                List<FieldDefinition> fields = mLookup.GetFields(type);

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
                        // Only add it if we don't have it already (is possible when inheriting classes)
                        if (!mLookup.HasStaticField(field))
                        {
                            ValueRef val = LLVM.AddGlobal(mCompiler.Module, fieldType, NameHelper.CreateFieldName(field.FullName));

                            // Note: the initializer may be changed later if the compiler sees that it can be constant
                            LLVM.SetInitializer(val, LLVM.ConstNull(fieldType));
                            mLookup.AddStaticField(field, val);
                        }
                    }
                    // Field for type instance
                    else
                    {
                        structData.Add(fieldType);
                    }
                }

                // Packing?
                bool packed = (type.PackingSize != -1);
                if (type.PackingSize != 1 && type.PackingSize != -1 && type.PackingSize != 0)
                {
                    throw new NotImplementedException("The packing size " + type.PackingSize + " is not implemented");
                }

                // Set struct data
                LLVM.StructSetBody(data, structData.ToArray(), packed);
            }
        }
    }
}
