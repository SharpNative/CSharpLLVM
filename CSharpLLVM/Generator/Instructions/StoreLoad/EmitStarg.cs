using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compilation;
using CSharpLLVM.Stack;
using Mono.Cecil;
using CSharpLLVM.Helpers;

namespace CSharpLLVM.Generator.Instructions.StoreLoad
{
    [InstructionHandler(Code.Starg, Code.Starg_S)]
    class EmitStarg : ICodeEmitter
    {
        /// <summary>
        /// Emits a starg instruction.
        /// </summary>
        /// <param name="instruction">The instruction.</param>
        /// <param name="context">The context.</param>
        /// <param name="builder">The builder.</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            StackElement value = context.CurrentStack.Pop();

            ParameterDefinition def = (ParameterDefinition)instruction.Operand;
            int index = def.Index;
            
            ValueRef arg = context.ArgumentValues[index];
            CastHelper.HelpIntAndPtrCast(builder, ref value.Value, ref value.Type, TypeHelper.GetTypeRefFromType(context.ArgumentILTypes[index]), "stargcast");
            LLVM.BuildStore(builder, value.Value, arg);
        }
    }
}
