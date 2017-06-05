using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compilation;
using CSharpLLVM.Stack;
using CSharpLLVM.Helpers;

namespace CSharpLLVM.Generator.Instructions.StoreLoad
{
    [InstructionHandler(Code.Ldind_I, Code.Ldind_I1, Code.Ldind_I2, Code.Ldind_I4, Code.Ldind_I8, Code.Ldind_R4, Code.Ldind_R8, Code.Ldind_Ref, Code.Ldind_U1, Code.Ldind_U2, Code.Ldind_U4)]
    class EmitLdind : ICodeEmitter
    {
        /// <summary>
        /// Emits a Ldind instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            StackElement pointer = context.CurrentStack.Pop();
            
            ValueRef res = LLVM.BuildLoad(builder, pointer.Value, "elem");

            // Some need to be pushed as an int32 on the stack
            Code code = instruction.OpCode.Code;
            if (code == Code.Ldind_I1 || code == Code.Ldind_I2 || code == Code.Ldind_I4 ||
                code == Code.Ldind_U1 || code == Code.Ldind_U2 || code == Code.Ldind_U4)
                res = LLVM.BuildIntCast(builder, res, TypeHelper.Int32, "tmp");

            TypeRef type = LLVM.TypeOf(res);
            context.CurrentStack.Push(new StackElement(res, TypeHelper.GetBasicTypeFromTypeRef(context.Compiler, type), type));
        }
    }
}
