using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compilation;
using CSharpLLVM.Stack;
using CSharpLLVM.Helpers;

namespace CSharpLLVM.Generator.Instructions.StoreLoad
{
    [InstructionHandler(Code.Stloc, Code.Stloc_S, Code.Stloc_0, Code.Stloc_1, Code.Stloc_2, Code.Stloc_3)]
    class EmitStloc : ICodeEmitter
    {
        /// <summary>
        /// Emits a stloc instruction
        /// </summary>
        /// <param name="instruction">The instruction.</param>
        /// <param name="context">The context.</param>
        /// <param name="builder">The builder.</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            Code code = instruction.OpCode.Code;

            int index;
            if (code >= Code.Stloc_0 && code <= Code.Stloc_3)
            {
                index = instruction.OpCode.Code - Code.Stloc_0;
            }
            else
            {
                VariableDefinition def = (VariableDefinition)instruction.Operand;
                index = def.Index;
            }

            StackElement element = context.CurrentStack.Pop();
            ValueRef data = element.Value;
            TypeRef destType = context.LocalTypes[index];

            // Possible cast needed.
            if (element.Type != destType)
                CastHelper.HelpIntAndPtrCast(builder, ref data, element.Type, destType);

            LLVM.BuildStore(builder, data, context.LocalValues[index]);
        }
    }
}
