using CSharpLLVM.Helpers;
using CSharpLLVM.Lookups;
using Mono.Cecil;
using Swigged.LLVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpLLVM.Compiler
{
    class VTableCompiler
    {
        private Compiler mCompiler;

        /// <summary>
        /// Creates a new VTableCompiler
        /// </summary>
        /// <param name="compiler">The compiler</param>
        public VTableCompiler(Compiler compiler)
        {
            mCompiler = compiler;
        }

        public void Compile(List<TypeDefinition> types, List<MethodDefinition> methods)
        {
            foreach (TypeDefinition type in types)
            {
                if (!mCompiler.Lookup.HasVTable(type))
                    continue;

                VTable vtable = mCompiler.Lookup.GetVTable(type);
                Console.WriteLine("---- compile vtable " + vtable.Name +" ----");
                
                foreach(MethodDefinition method in type.Methods)
                {
                    
                }
                vtable.Dump();

                /*TypeRef vtableType = LLVM.ArrayType(TypeHelper.NativeIntType, 2);
                ValueRef global = LLVM.AddGlobal(mCompiler.Module, vtableType, vtable.Name);
                LLVM.SetInitializer(global, LLVM.ConstArray(TypeHelper.NativeIntType, new ValueRef[] { LLVM.ConstInt(TypeHelper.NativeIntType, 1, false), LLVM.ConstInt(TypeHelper.NativeIntType, 69, false) }));*/
            }

            
        }
    }
}
