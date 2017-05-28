using CSharpLLVM.Helpers;
using Swigged.LLVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpLLVM.Compiler
{
    class BuiltinRuntimeFunctions
    {
        private Compiler mCompiler;

        /// <summary>
        /// Creates a new BuiltinRuntimeFunctions
        /// </summary>
        /// <param name="compiler">The compiler</param>
        public BuiltinRuntimeFunctions(Compiler compiler)
        {
            mCompiler = compiler;
        }

        /// <summary>
        /// Compiles the builtin runtime functions
        /// </summary>
        public void Compile()
        {
            BuilderRef builder = LLVM.CreateBuilderInContext(mCompiler.ModuleContext);
            CreateStringGetCharsFunction(mCompiler.Module, builder);
            LLVM.DisposeBuilder(builder);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="module"></param>
        /// <param name="builder"></param>
        public void CreateStringGetCharsFunction(ModuleRef module, BuilderRef builder)
        {
            const string funcName = "System_Char_System_String_get_Chars_System_Int32";

            // Create function and set linkage
            TypeRef type = LLVM.FunctionType(TypeHelper.Int8, new TypeRef[] { TypeHelper.String, TypeHelper.Int32 }, false);
            ValueRef func = LLVM.AddFunction(module, funcName, type);
            LLVM.SetLinkage(func, Linkage.InternalLinkage);

            // Arguments
            ValueRef str = LLVM.GetParam(func, 0);
            ValueRef index = LLVM.GetParam(func, 1);

            // Create function body
            BasicBlockRef entry = LLVM.AppendBasicBlock(func, string.Empty);
            LLVM.PositionBuilderAtEnd(builder, entry);
            ValueRef ch = LLVM.BuildGEP(builder, str, new ValueRef[] { index }, "charptr");
            ValueRef returnVal = LLVM.BuildLoad(builder, ch, "character");
            LLVM.BuildRet(builder, returnVal);

            mCompiler.Lookup.AddFunction(funcName, func);
        }
    }
}
