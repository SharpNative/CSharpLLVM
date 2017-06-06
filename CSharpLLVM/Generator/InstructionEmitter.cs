using CSharpLLVM.Compilation;
using CSharpLLVM.Helpers;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using Swigged.LLVM;

namespace CSharpLLVM.Generator
{
    class InstructionEmitter
    {
        private MethodContext mContext;
        private BuilderRef mBuilder;

        /// <summary>
        /// Creates a new InstructionEmitter
        /// </summary>
        /// <param name="context">The method context</param>
        public InstructionEmitter(MethodContext context)
        {
            mContext = context;
            mBuilder = LLVM.CreateBuilderInContext(context.Compiler.ModuleContext);
        }

        /// <summary>
        /// Cleanup
        /// </summary>
        ~InstructionEmitter()
        {
            LLVM.DisposeBuilder(mBuilder);
        }

        /// <summary>
        /// Creates locals
        /// </summary>
        private void createLocals()
        {
            MethodBody body = mContext.Method.Body;
            mContext.LocalValues = new ValueRef[body.Variables.Count];
            mContext.LocalTypes = new TypeRef[body.Variables.Count];
            mContext.LocalILTypes = new TypeReference[body.Variables.Count];

            // Set to start
            LLVM.PositionBuilderAtEnd(mBuilder, mContext.GetBlockOf(body.Instructions[0]));

            foreach (VariableDefinition varDef in body.Variables)
            {
                TypeReference type = varDef.VariableType;
                TypeRef typeRef = TypeHelper.GetTypeRefFromType(type);
                
                mContext.LocalValues[varDef.Index] = LLVM.BuildAlloca(mBuilder, typeRef, string.Format("local{0}", varDef.Index));
                mContext.LocalTypes[varDef.Index] = typeRef;
                mContext.LocalILTypes[varDef.Index] = type;
            }
        }

        /// <summary>
        /// Emits the instructions of this method
        /// </summary>
        /// <param name="codeGen">The code generator</param>
        public void EmitInstructions(CodeGenerator codeGen)
        {
            // Init
            mContext.Init();
            createLocals();

            // Process instructions
            Collection<Instruction> instructions = mContext.Method.Body.Instructions;
            foreach (Instruction instruction in instructions)
            {
                // Switch branch
                if (mContext.IsNewBlock(instruction))
                {
                    LLVM.PositionBuilderAtEnd(mBuilder, mContext.GetBlockOf(instruction));

                    if (mContext.IsNewStack(instruction))
                    {
                        mContext.SetStack(instruction);
                    }
                }

                // Update stack
                if (instruction.OpCode.FlowControl == FlowControl.Branch || instruction.OpCode.FlowControl == FlowControl.Cond_Branch)
                {
                    Instruction dest = (Instruction)instruction.Operand;
                    if (mContext.IsNewBlock(dest))
                    {
                        mContext.UpdateStack(mBuilder, instruction, dest);
                    }
                }

                codeGen.Emit(instruction, mContext, mBuilder);

                // If the next instruction is a new block, and we didn't have an explicit branch instruction to the next block
                // then we need to create the branch instruction explicitely
                if (mContext.IsNewBlock(instruction.Next))
                {
                    // If this instruction did not already branch...
                    if (instruction.OpCode.FlowControl != FlowControl.Branch && instruction.OpCode.FlowControl != FlowControl.Cond_Branch)
                    {
                        if (mContext.IsNewBlock(instruction.Next))
                        {
                            mContext.UpdateStack(mBuilder, instruction, instruction.Next);
                        }

                        mContext.SetStack(instruction.Next);
                        LLVM.BuildBr(mBuilder, mContext.GetBlockOf(instruction.Next));
                    }
                }
            }
        }
    }
}
