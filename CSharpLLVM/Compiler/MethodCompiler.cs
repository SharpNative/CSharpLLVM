using CSharpLLVM.Helpers;
using Mono.Cecil;
using Swigged.LLVM;
using System;

namespace CSharpLLVM.Compiler
{
    class MethodCompiler
    {
        private Compiler m_compiler;

        /// <summary>
        /// Creates a new InstructionCompiler
        /// </summary>
        /// <param name="compiler">The compiler</param>
        public MethodCompiler(Compiler compiler)
        {
            m_compiler = compiler;
        }

        /// <summary>
        /// Compiles a method
        /// </summary>
        /// <param name="methodDef">The method</param>
        public void Compile(MethodDefinition methodDef)
        {
            // TODO: fixme
            if (methodDef.Name.Contains(".ctor"))
            {
                Console.WriteLine("SKIPPED .ctor");
                return;
            }

            // Do we need to create a new function for this, or is there already been a reference to this function?
            // If there is already a reference, use that empty function instead of creating a new one
            string methodName = NameHelper.CreateMethodName(methodDef);
            ValueRef? function = m_compiler.GetFunction(methodName);
            if (!function.HasValue)
            {
                int paramCount = methodDef.Parameters.Count;
                TypeRef[] argTypes = new TypeRef[paramCount];
                Console.Write(methodDef.Name+": ");
                for (int i = 0; i < paramCount; i++)
                {
                    Console.Write(methodDef.Parameters[i].ParameterType+", ");
                    argTypes[i] = TypeHelper.GetTypeRefFromType(methodDef.Parameters[i].ParameterType);
                }
                Console.WriteLine("");
                TypeRef functionType = LLVM.FunctionType(TypeHelper.GetTypeRefFromType(methodDef.ReturnType), argTypes, false);
                function = LLVM.AddFunction(m_compiler.Module, methodName, functionType);
                m_compiler.AddFunction(methodName, function.Value);
            }

            // Only generate if it has a body
            if (methodDef.Body == null)
            {
                LLVM.SetLinkage(function.Value, Linkage.ExternalLinkage);
                return;
            }

            // Compile instructions
            MethodContext ctx = new MethodContext(m_compiler, methodDef, function.Value);
            InstructionEmitter emitter = new InstructionEmitter(ctx);
            emitter.EmitInstructions(m_compiler.CodeGen);

            // Verify & optimize
            m_compiler.VerifyAndOptimizeFunction(function.Value);
        }
    }
}
