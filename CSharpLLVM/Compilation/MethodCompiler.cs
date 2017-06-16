using CSharpLLVM.Generator;
using CSharpLLVM.Helpers;
using Mono.Cecil;
using Swigged.LLVM;
using System;

namespace CSharpLLVM.Compilation
{
    class MethodCompiler
    {
        private Compiler mCompiler;

        /// <summary>
        /// Creates a new MethodCompiler.
        /// </summary>
        /// <param name="compiler">The compiler.</param>
        public MethodCompiler(Compiler compiler)
        {
            mCompiler = compiler;
        }

        /// <summary>
        /// Creates the declaration for a method.
        /// </summary>
        /// <param name="methodDef">The method definition.</param>
        public void CreateDeclaration(MethodDefinition methodDef)
        {
            string methodName = NameHelper.CreateMethodName(methodDef);
            int paramCount = methodDef.Parameters.Count;

            // If we expect an instance reference as first argument, then we need to make sure our for loop has an offset.
            int offset = 0;
            if (methodDef.HasThis)
            {
                paramCount++;
                offset = 1;
            }

            // Fill in arguments.
            TypeRef[] argTypes = new TypeRef[paramCount];
            for (int i = offset; i < paramCount; i++)
            {
                TypeReference type = methodDef.Parameters[i - offset].ParameterType;
                argTypes[i] = TypeHelper.GetTypeRefFromType(type);
            }

            // If needed, fill in the instance reference.
            if (methodDef.HasThis)
            {
                argTypes[0] = TypeHelper.GetTypeRefFromType(methodDef.DeclaringType);
                if (methodDef.DeclaringType.IsValueType)
                    argTypes[0] = LLVM.PointerType(argTypes[0], 0);
            }

            // Create LLVM function type and add function to the lookup table.
            TypeRef functionType = LLVM.FunctionType(TypeHelper.GetTypeRefFromType(methodDef.ReturnType), argTypes, false);
            ValueRef function = LLVM.AddFunction(mCompiler.Module, methodName, functionType);
            mCompiler.Lookup.AddFunction(methodName, function);
        }

        /// <summary>
        /// Compiles a method.
        /// </summary>
        /// <param name="methodDef">The method definition.</param>
        public void Compile(MethodDefinition methodDef)
        {
            string methodName = NameHelper.CreateMethodName(methodDef);
            ValueRef? function = mCompiler.Lookup.GetFunction(methodName);
            if (!function.HasValue)
            {
                throw new InvalidOperationException("The method has no declaration yet: " + methodDef);
            }

            // Private methods have internal linkage.
            if (methodDef.IsPrivate)
                LLVM.SetLinkage(function.Value, Linkage.InternalLinkage);

            // Only generate if it has a body.
            if (!methodDef.HasBody || methodDef.Body.CodeSize == 0)
                LLVM.SetLinkage(function.Value, Linkage.ExternalLinkage);

            // Compile instructions.
            MethodContext ctx = new MethodContext(mCompiler, methodDef, function.Value);
            InstructionEmitter emitter = new InstructionEmitter(ctx);
            //try
            {
                emitter.EmitInstructions(mCompiler.CodeGen);

                // Verify & optimize.
                mCompiler.VerifyAndOptimizeFunction(function.Value);
            }
            /*catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Exception inside method " + methodDef);
                Console.WriteLine(e.Message);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(e.StackTrace);
                Console.ForegroundColor = ConsoleColor.Gray;
            }*/
        }
    }
}
