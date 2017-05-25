using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compiler;
using CSharpLLVM.Stack;
using CSharpLLVM.Helpers;

namespace CSharpLLVM.Generator.Instructions.Arrays
{
    [InstructionHandler(Code.Ldelem_I, Code.Ldelem_I1, Code.Ldelem_I2, Code.Ldelem_I4, Code.Ldelem_I8, Code.Ldelem_U1, Code.Ldelem_U2, Code.Ldelem_U4, Code.Ldelem_R4, Code.Ldelem_R8, Code.Ldelem_Ref)]
    class EmitLdelem : ICodeEmitter
    {
        /// <summary>
        /// Emits a ldelem instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            StackElement index = context.CurrentStack.Pop();
            StackElement array = context.CurrentStack.Pop();

            ValueRef ptr = LLVM.BuildGEP(builder, array.Value, new ValueRef[] { index.Value }, "arrayptr");
            ValueRef res = LLVM.BuildLoad(builder, ptr, "arrayelem");

            // Some need to be pushed as an int32 on the stack
            Code code = instruction.OpCode.Code;
            if (code == Code.Ldelem_I1 || code == Code.Ldelem_I2 || code == Code.Ldelem_I4 ||
                code == Code.Ldelem_U1 || code == Code.Ldelem_U2 || code == Code.Ldelem_U4)
                res = LLVM.BuildIntCast(builder, res, TypeHelper.Int32, "tmp");

            context.CurrentStack.Push(res);
        }
    }
}
