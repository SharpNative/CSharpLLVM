using CSharpLLVM.Compilation;
using CSharpLLVM.Helpers;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Swigged.LLVM;
using System;
using System.Collections.Generic;
using System.Linq;

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
            LLVM.PositionBuilderAtEnd(mBuilder, mContext.GetBranch(body.Instructions[0]).Block);

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

            // Add branches in a list so we can sort them
            /*List<Branch> branches = new List<Branch>();
            for (int i = 0; i < mContext.Branches.Length; i++)
            {
                Branch branch = mContext.Branches[i];
                if (branch != null)
                    branches.Add(branch);
            }
            branches.Sort(sortBranches);//TODO: remove this method

            foreach (Branch branch in branches)
            {
                Console.WriteLine("generate branch: " + string.Format("{0:x4}", branch.Offset));

                mContext.CurrentStack = branch.Stack;
                branch.UpdateStack(mBuilder);
                
                LLVM.PositionBuilderAtEnd(mBuilder, branch.Block);
                emitInstructionsInBranch(codeGen, branch);
            }*/

            for (int i = 0; i < mContext.Branches.Length; i++)
            {
                Branch branch = mContext.Branches[i];
                if (branch != null)
                    emitInstructionsInBranch(codeGen, branch);
            }
        }

        /// <summary>
        /// Emits the instructions within a branch
        /// </summary>
        /// <param name="codeGen">The code generator</param>
        /// <param name="branch">The branch</param>
        private void emitInstructionsInBranch(CodeGenerator codeGen, Branch branch)
        {
            // Check dependencies
            foreach(Branch source in branch.Sources)
            {
                if (!source.IsGenerated && !source.Sources.Contains(branch))
                {
                    emitInstructionsInBranch(codeGen, source);
                    
                }
            }

            branch.IsGenerated = true;
            mContext.CurrentStack = branch.Stack;
            branch.UpdateStack(mBuilder);
            LLVM.PositionBuilderAtEnd(mBuilder, branch.Block);

            foreach (Instruction instruction in branch.Instructions)
            {
                FlowControl flow = instruction.OpCode.FlowControl;
                
                codeGen.Emit(instruction, mContext, mBuilder);

                // If the next instruction is a new block, and we didn't have an explicit branch instruction to the next block
                // then we need to create the branch instruction explicitely
                if (mContext.IsNewBranch(instruction.Next))
                {
                    // If this instruction did not branch already...
                    if (flow != FlowControl.Branch && flow != FlowControl.Cond_Branch)
                    {
                        LLVM.BuildBr(mBuilder, mContext.GetBlockOf(instruction.Next));
                    }
                }
            }
        }

        /// <summary>
        /// Method to sort branches based on dependencies
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Order number</returns>
        private int sortBranches(Branch left, Branch right)
        {
            if (left.Sources.Contains(right))
                return 1;
            else if (right.Sources.Contains(left))
                return -1;

            return 0;
        }
    }
}
