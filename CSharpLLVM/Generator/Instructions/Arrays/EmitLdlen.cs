using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compiler;
using CSharpLLVM.Stack;
using CSharpLLVM.Helpers;

namespace CSharpLLVM.Generator.Instructions.Arrays
{
    [InstructionHandler(Code.Ldlen)]
    class EmitLdlen : ICodeEmitter
    {
        /// <summary>
        /// Emits a ldlen instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            StackElement array = context.CurrentStack.Pop();
            
            // Note: an array length in CIL is an 32-bit int
            ValueRef ptrAddress = LLVM.BuildPtrToInt(builder, array.Value, TypeHelper.NativeIntType, "ptraddress");
            ValueRef offset = LLVM.BuildSub(builder, ptrAddress, LLVM.ConstInt(TypeHelper.NativeIntType, 4, false), "lengthoffset");
            ValueRef ptrOffset = LLVM.BuildIntToPtr(builder, offset, LLVM.PointerType(TypeHelper.Int32, 0), "lengthoffsetptr");
            ValueRef gep = LLVM.BuildGEP(builder, ptrOffset, new ValueRef[] { LLVM.ConstInt(TypeHelper.NativeIntType, 0, false) }, "lengthptr");
            context.CurrentStack.Push(LLVM.BuildLoad(builder, gep, "length"));
        }
    }
}
