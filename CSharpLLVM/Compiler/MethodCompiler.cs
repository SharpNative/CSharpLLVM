using CSharpLLVM.Generator;
using CSharpLLVM.Helpers;
using Mono.Cecil;
using Swigged.LLVM;

namespace CSharpLLVM.Compiler
{
    class MethodCompiler
    {
        private Compiler mCompiler;

        /// <summary>
        /// Creates a new MethodCompiler
        /// </summary>
        /// <param name="compiler">The compiler</param>
        public MethodCompiler(Compiler compiler)
        {
            mCompiler = compiler;
        }

        /// <summary>
        /// Compiles a method
        /// </summary>
        /// <param name="methodDef">The method</param>
        /// <returns>The function</returns>
        public ValueRef? Compile(MethodDefinition methodDef)
        {
            // Do we need to create a new function for this, or is there already been a reference to this function?
            // If there is already a reference, use that empty function instead of creating a new one
            string methodName = NameHelper.CreateMethodName(methodDef);
            ValueRef? function = mCompiler.Lookup.GetFunction(methodName);
            if (!function.HasValue)
            {
                // If we expect an instance reference as first argument, then we need to make sure our for loop has an offset
                int paramCount = methodDef.Parameters.Count;
                int offset = 0;
                if (methodDef.HasThis)
                {
                    paramCount++;
                    offset = 1;
                }

                // Fill in arguments
                TypeRef[] argTypes = new TypeRef[paramCount];
                for (int i = offset; i < paramCount; i++)
                {
                    TypeReference type = methodDef.Parameters[i - offset].ParameterType;
                    argTypes[i] = TypeHelper.GetTypeRefFromType(type);
                    if (TypeHelper.RequiresExtraPointer(type.Resolve()))
                        argTypes[i] = LLVM.PointerType(argTypes[i], 0);
                }

                // If needed, fill in the instance reference
                if (methodDef.HasThis)
                {
                    argTypes[0] = LLVM.PointerType(TypeHelper.GetTypeRefFromType(methodDef.DeclaringType), 0);
                }

                // Create LLVM function type and add function to lookup table
                TypeRef functionType = LLVM.FunctionType(TypeHelper.GetTypeRefFromType(methodDef.ReturnType), argTypes, false);
                function = LLVM.AddFunction(mCompiler.Module, methodName, functionType);
                mCompiler.Lookup.AddFunction(methodName, function.Value);
            }

            // Private only visible for us
            if (methodDef.IsPrivate)
                LLVM.SetLinkage(function.Value, Linkage.InternalLinkage);

            // Only generate if it has a body
            if (!methodDef.HasBody || methodDef.Body.CodeSize == 0)
            {
                LLVM.SetLinkage(function.Value, Linkage.ExternalLinkage);
                return /*null*/function;
            }
            
            // Compile instructions
            MethodContext ctx = new MethodContext(mCompiler, methodDef, function.Value);
            InstructionEmitter emitter = new InstructionEmitter(ctx);
            emitter.EmitInstructions(mCompiler.CodeGen);

            // Verify & optimize
            mCompiler.VerifyAndOptimizeFunction(function.Value);

            return function;
        }
    }
}
