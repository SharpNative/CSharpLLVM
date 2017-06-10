using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compilation;
using CSharpLLVM.Stack;
using CSharpLLVM.Helpers;

namespace CSharpLLVM.Generator.Instructions.Arrays
{
    [InstructionHandler(Code.Ldlen)]
    class EmitLdlen : ICodeEmitter
    {
        /// <summary>
        /// Emits a ldlen instruction.
        /// </summary>
        /// <param name="instruction">The instruction.</param>
        /// <param name="context">The context.</param>
        /// <param name="builder">The builder.</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            StackElement array = context.CurrentStack.Pop();
            
            // Note: An array length in CIL is an 32-bit int, but the array is aligned, so we need to substract.
            //       the size of the native int from the address.
            ValueRef ptrAddress = LLVM.BuildPtrToInt(builder, array.Value, TypeHelper.NativeIntType, "ptraddress");
            ValueRef offset = LLVM.BuildSub(builder, ptrAddress, LLVM.ConstInt(TypeHelper.NativeIntType, LLVM.SizeOfTypeInBits(context.Compiler.TargetData, TypeHelper.NativeIntType) / 8, false), "lengthoffset");
            ValueRef ptrOffset = LLVM.BuildIntToPtr(builder, offset, LLVM.PointerType(TypeHelper.Int32, 0), "lengthoffsetptr");
            ValueRef gep = LLVM.BuildGEP(builder, ptrOffset, new ValueRef[] { LLVM.ConstInt(TypeHelper.NativeIntType, 0, false) }, "lengthptr");
            ValueRef result = LLVM.BuildLoad(builder, gep, "length");
            
            context.CurrentStack.Push(new StackElement(result, typeof(int).GetTypeReference(), TypeHelper.Int32));
        }
    }
}
