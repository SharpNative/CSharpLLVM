using CSharpLLVM.Generator;
using CSharpLLVM.Helpers;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using Swigged.LLVM;

namespace CSharpLLVM.Compiler
{
    class InstructionEmitter
    {
        private MethodContext mcontext;
        private BuilderRef mbuilder;

        /// <summary>
        /// Creates a new InstructionEmitter
        /// </summary>
        /// <param name="context">The method context</param>
        public InstructionEmitter(MethodContext context)
        {
            mcontext = context;
            mbuilder = LLVM.CreateBuilderInContext(context.Compiler.ModuleContext);
        }

        /// <summary>
        /// Cleanup
        /// </summary>
        ~InstructionEmitter()
        {
            LLVM.DisposeBuilder(mbuilder);
        }

        /// <summary>
        /// Creates locals
        /// </summary>
        private void createLocals()
        {
            MethodBody body = mcontext.Method.Body;
            mcontext.LocalValues = new ValueRef[body.Variables.Count];
            mcontext.LocalTypes = new TypeRef[body.Variables.Count];
            mcontext.LocalILTypes = new TypeReference[body.Variables.Count];

            // Set to start
            LLVM.PositionBuilderAtEnd(mbuilder, mcontext.GetBlockOf(body.Instructions[0]));

            foreach (VariableDefinition varDef in body.Variables)
            {
                TypeRef type = TypeHelper.GetTypeRefFromType(varDef.VariableType);
                mcontext.LocalValues[varDef.Index] = LLVM.BuildAlloca(mbuilder, type, string.Format("local{0}", varDef.Index));
                mcontext.LocalTypes[varDef.Index] = type;
                mcontext.LocalILTypes[varDef.Index] = varDef.VariableType;
            }
        }

        /// <summary>
        /// Emits the instructions of this method
        /// </summary>
        /// <param name="codeGen">The code generator</param>
        public void EmitInstructions(CodeGenerator codeGen)
        {
            // Init
            mcontext.Init();
            createLocals();

            // Process instructions
            Collection<Instruction> instructions = mcontext.Method.Body.Instructions;
            foreach (Instruction instruction in instructions)
            {
                // Switch branch
                if (mcontext.IsNewBlock(instruction))
                {
                    LLVM.PositionBuilderAtEnd(mbuilder, mcontext.GetBlockOf(instruction));

                    if (mcontext.IsNewStack(instruction))
                    {
                        mcontext.SetStack(instruction);
                    }
                }

                // Update stack
                if (instruction.OpCode.FlowControl == FlowControl.Branch || instruction.OpCode.FlowControl == FlowControl.Cond_Branch)
                {
                    Instruction dest = (Instruction)instruction.Operand;
                    if (mcontext.IsNewBlock(dest))
                    {
                        mcontext.UpdateStack(mbuilder, instruction, dest);
                    }
                }

                codeGen.Emit(instruction, mcontext, mbuilder);

                // If the next instruction is a new block, and we didn't have an explicit branch instruction to the next block
                // then we need to create the branch instruction explicitely
                if (mcontext.IsNewBlock(instruction.Next))
                {
                    // If this instruction did not already branch...
                    if (instruction.OpCode.FlowControl != FlowControl.Branch && instruction.OpCode.FlowControl != FlowControl.Cond_Branch)
                    {
                        if (mcontext.IsNewBlock(instruction.Next))
                        {
                            mcontext.UpdateStack(mbuilder, instruction, instruction.Next);
                        }

                        mcontext.SetStack(instruction.Next);
                        LLVM.BuildBr(mbuilder, mcontext.GetBlockOf(instruction.Next));
                    }
                }
            }
        }
    }
}
