using CSharpLLVM.Generator;
using CSharpLLVM.Helpers;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using Swigged.LLVM;

namespace CSharpLLVM.Compiler
{
    class InstructionEmitter
    {
        private MethodContext m_context;
        private BuilderRef m_builder;

        /// <summary>
        /// Creates a new InstructionEmitter
        /// </summary>
        /// <param name="context">The method context</param>
        public InstructionEmitter(MethodContext context)
        {
            m_context = context;
            m_builder = LLVM.CreateBuilderInContext(context.Compiler.ModuleContext);
        }

        /// <summary>
        /// Cleanup
        /// </summary>
        ~InstructionEmitter()
        {
            LLVM.DisposeBuilder(m_builder);
        }

        /// <summary>
        /// Creates locals
        /// </summary>
        private void createLocals()
        {
            MethodBody body = m_context.Method.Body;
            m_context.LocalValues = new ValueRef[body.Variables.Count];
            m_context.LocalTypes = new TypeRef[body.Variables.Count];

            // Set to start
            LLVM.PositionBuilderAtEnd(m_builder, m_context.GetBlockOf(body.Instructions[0]));

            foreach (VariableDefinition varDef in body.Variables)
            {
                TypeRef type = TypeHelper.GetTypeRefFromType(varDef.VariableType);
                m_context.LocalValues[varDef.Index] = LLVM.BuildAlloca(m_builder, type, string.Format("local{0}", varDef.Index));
                m_context.LocalTypes[varDef.Index] = type;
            }
        }

        /// <summary>
        /// Emits the instructions of this method
        /// </summary>
        /// <param name="codeGen">The code generator</param>
        public void EmitInstructions(CodeGenerator codeGen)
        {
            // Init
            m_context.Init();
            createLocals();

            // Process instructions
            Collection<Instruction> instructions = m_context.Method.Body.Instructions;
            foreach (Instruction instruction in instructions)
            {
                // Switch branch
                if (m_context.IsNewBlock(instruction))
                {
                    LLVM.PositionBuilderAtEnd(m_builder, m_context.GetBlockOf(instruction));

                    if (m_context.IsNewStack(instruction))
                    {
                        m_context.SetStack(instruction);
                    }
                }

                // Update stack
                if (instruction.OpCode.FlowControl == FlowControl.Branch || instruction.OpCode.FlowControl == FlowControl.Cond_Branch)
                {
                    Instruction dest = (Instruction)instruction.Operand;
                    if (m_context.IsNewBlock(dest))
                    {
                        m_context.UpdateStack(m_builder, instruction, dest);
                    }
                }

                codeGen.Emit(instruction, m_context, m_builder);

                // If the next instruction is a new block, and we didn't have an explicit branch instruction to the next block
                // then we need to create the branch instruction explicitely
                if (m_context.IsNewBlock(instruction.Next))
                {
                    // If this instruction did not already branch...
                    if (instruction.OpCode.FlowControl != FlowControl.Branch && instruction.OpCode.FlowControl != FlowControl.Cond_Branch)
                    {
                        LLVM.BuildBr(m_builder, m_context.GetBlockOf(instruction.Next));

                        m_context.SetStack(instruction.Next);
                        
                        if (m_context.IsNewBlock(instruction.Next))
                        {
                            m_context.UpdateStack(m_builder, instruction, instruction.Next);
                        }
                    }
                }
            }
        }
    }
}
