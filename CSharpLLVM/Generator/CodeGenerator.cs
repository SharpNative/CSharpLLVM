using CSharpLLVM.Compilation;
using CSharpLLVM.Generator.Instructions;
using Mono.Cecil.Cil;
using Swigged.LLVM;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace CSharpLLVM.Generator
{
    class CodeGenerator
    {
        private Dictionary<Code, ICodeEmitter> mEmitters = new Dictionary<Code, ICodeEmitter>();

        /// <summary>
        /// Creates a new CodeGenerator.
        /// </summary>
        public CodeGenerator()
        {
            // Load code emitters.
            Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (Type type in types)
            {
                // Check and get handler.
                InstructionHandlerAttribute attrib = (InstructionHandlerAttribute)type.GetCustomAttribute(typeof(InstructionHandlerAttribute), false);
                if (attrib == null)
                    continue;

                // Register emitter.
                ICodeEmitter emitter = (ICodeEmitter)Activator.CreateInstance(type);
                foreach (Code code in attrib.Codes)
                    mEmitters.Add(code, emitter);
            }
            
            Logger.LogDetailVerbose("{0} code emitters registered", mEmitters.Count);
        }

        /// <summary>
        /// Emits an instruction.
        /// </summary>
        /// <param name="instruction">The instruction.</param>
        /// <param name="context">The context.</param>
        /// <param name="builder">The builder.</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            ICodeEmitter emitter = null;
            if (mEmitters.TryGetValue(instruction.OpCode.Code, out emitter))
            {
                emitter.Emit(instruction, context, builder);
            }
            else
            {
                throw new NotImplementedException("Instruction with opcode " + instruction.OpCode.Code + " is not implemented");
            }
        }
    }
}
