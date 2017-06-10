using System;
using Mono.Cecil.Cil;

namespace CSharpLLVM.Generator.Instructions
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false,Inherited = false)]
    sealed class InstructionHandlerAttribute : Attribute
    {
        public Code[] Codes { get; private set; }

        /// <summary>
        /// Creates a new InstructionHandlerAttribute.
        /// </summary>
        /// <param name="codes">Which codes this handler handles.</param>
        public InstructionHandlerAttribute(params Code[] codes)
        {
            Codes = codes;
        }
    }
}
