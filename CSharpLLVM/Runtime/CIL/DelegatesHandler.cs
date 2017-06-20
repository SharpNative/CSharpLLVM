using Swigged.LLVM;
using System;

namespace CSharpLLVM.Runtime.CIL
{
    [RuntimeHandler(typeof(MulticastDelegate))]
    class DelegatesHandler : IRuntimeHandler
    {
        [MethodHandler(".ctor")]
        public void compileCtor(BuilderRef builder)
        {

        }

        [MethodHandler("Invoke")]
        public void compileInvoke(BuilderRef builder)
        {

        }
    }
}
