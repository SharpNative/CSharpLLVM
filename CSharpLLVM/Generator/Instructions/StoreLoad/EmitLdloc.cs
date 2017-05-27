using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compiler;
using CSharpLLVM.Stack;
using CSharpLLVM.Helpers;

namespace CSharpLLVM.Generator.Instructions.StoreLoad
{
    [InstructionHandler(Code.Ldloc, Code.Ldloc_S, Code.Ldloc_0, Code.Ldloc_1, Code.Ldloc_2, Code.Ldloc_3)]
    class EmitLdloc : ICodeEmitter
    {
        /// <summary>
        /// Emits a ldloc instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            Code code = instruction.OpCode.Code;

            int index;
            if (code >= Code.Ldloc_0 && code <= Code.Ldloc_3)
            {
                index = instruction.OpCode.Code - Code.Ldloc_0;
            }
            else
            {
                VariableDefinition def = (VariableDefinition)instruction.Operand;
                index = def.Index;
            }

            ValueRef value = LLVM.BuildLoad(builder, context.LocalValues[index], "ldloc");
            context.CurrentStack.Push(new StackElement(value, TypeHelper.GetTypeFromTypeReference(context.Compiler, context.LocalILTypes[index])));
        }
    }
}
