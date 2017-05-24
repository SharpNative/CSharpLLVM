using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compiler;
using CSharpLLVM.Stack;

namespace CSharpLLVM.Generator.Instructions.StoreLoad
{
    [InstructionHandler(Code.Stloc, Code.Stloc_S, Code.Stloc_0, Code.Stloc_1, Code.Stloc_2, Code.Stloc_3)]
    class EmitStloc : ICodeEmitter
    {
        /// <summary>
        /// Emits a stloc instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
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

            // Cast if not the same type
            if (element.Type != destType)
            {
                // Two cases: int to different size, or int to pointer
                TypeKind kind = LLVM.GetTypeKind(destType);
                
                // Convert to pointer
                if (kind == TypeKind.PointerTypeKind)
                    data = LLVM.BuildIntToPtr(builder, data, destType, "tmpptr");
                // Convert to int of different size
                else
                    data = LLVM.BuildIntCast(builder, data, destType, "tmpint");
            }

            LLVM.BuildStore(builder, data, context.LocalValues[index]);
        }
    }
}
