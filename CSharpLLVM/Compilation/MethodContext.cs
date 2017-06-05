using CSharpLLVM.Stack;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Swigged.LLVM;

namespace CSharpLLVM.Compilation
{
    class MethodContext
    {
        public Compiler Compiler { get; private set; }
        public ILStack CurrentStack { get; private set; }
        public MethodDefinition Method { get; private set; }
        public ValueRef Function { get; private set; }

        public ValueRef[] LocalValues { get; set; }
        public TypeRef[] LocalTypes { get; set; }
        public TypeReference[] LocalILTypes { get; set; }
        public ValueRef[] ArgumentValues { get; set; }
        public TypeReference[] ArgumentILTypes { get; set; }

        // Blocks & branching
        private BasicBlockRef[] mBlocks;
        private bool[] mBranch;
        private int[] mRefers;
        private ILStack[] mStacks;

        /// <summary>
        /// Creates a new MethodContext
        /// </summary>
        /// <param name="compiler">The compiler</param>
        /// <param name="method">The method</param>
        /// <param name="function">The function</param>
        public MethodContext(Compiler compiler, MethodDefinition method, ValueRef function)
        {
            Compiler = compiler;
            Method = method;
            Function = function;
        }

        /// <summary>
        /// Gets the block that starts with a given instruction
        /// </summary>
        /// <param name="instr">The instruction that starts a new block</param>
        /// <returns>The block</returns>
        public BasicBlockRef GetBlockOf(Instruction instr)
        {
            return mBlocks[instr.Offset];
        }

        /// <summary>
        /// Checks if an instruction starts a new block
        /// </summary>
        /// <param name="instr">The instruction</param>
        /// <returns>If the given instruction starts a new block</returns>
        public bool IsNewBlock(Instruction instr)
        {
            return (instr != null && mBranch[instr.Offset]);
        }

        /// <summary>
        /// Checks if this instruction causes a stack switch
        /// </summary>
        /// <param name="instr">The instruction</param>
        /// <returns>If it causes a stack switch</returns>
        public bool IsNewStack(Instruction instr)
        {
            return (mStacks[instr.Offset] != null);
        }

        /// <summary>
        /// Updates the stack with phi nodes
        /// </summary>
        /// <param name="builder">The builder</param>
        /// <param name="srcInstr">The instruction in the source block</param>
        /// <param name="dstInstr">The instruction in the destination block</param>
        public void UpdateStack(BuilderRef builder, Instruction srcInstr, Instruction dstInstr)
        {
            ILStack srcStack = GetStack(srcInstr);
            ILStack dstStack = GetStack(dstInstr);
            BasicBlockRef oldBlock = GetBlockOf(srcInstr);
            BasicBlockRef newBlock = GetBlockOf(dstInstr);

            dstStack.Update(builder, srcStack, oldBlock, newBlock, mRefers[dstInstr.Offset]);
        }

        /// <summary>
        /// Gets a stack for the block where the instruction is
        /// </summary>
        /// <param name="instr">The instruction</param>
        /// <returns>The stack</returns>
        public ILStack GetStack(Instruction instr)
        {
            return mStacks[instr.Offset];
        }

        /// <summary>
        /// Sets the current stack for the block where the instruction is
        /// </summary>
        /// <param name="instr">The instruction</param>
        public void SetStack(Instruction instr)
        {
            CurrentStack = mStacks[instr.Offset];
        }

        /// <summary>
        /// Finds all blocks
        /// </summary>
        private void findBlocks()
        {
            // Look for branching, create blocks for the branches
            // Note: we first search all the branches so we can later add them in the correct order
            //       this is because we may not always have the branches in a chronological order
            mBlocks = new BasicBlockRef[Method.Body.CodeSize];
            mBranch = new bool[Method.Body.CodeSize];
            mStacks = new ILStack[Method.Body.CodeSize];
            mRefers = new int[Method.Body.CodeSize];

            foreach (Instruction instruction in Method.Body.Instructions)
            {
                FlowControl flow = instruction.OpCode.FlowControl;

                // If this instruction branches to a destination, create that destination block
                if (flow == FlowControl.Branch || flow == FlowControl.Cond_Branch)
                {
                    Instruction dest = (Instruction)instruction.Operand;
                    mBranch[dest.Offset] = true;
                    mRefers[dest.Offset]++;
                }

                // If this instruction does branching by a conditional, we also need to have a block after this instruction
                // for if the conditional branch is not being executed
                if (instruction.Next != null && instruction.OpCode.FlowControl == FlowControl.Cond_Branch)
                {
                    mBranch[instruction.Next.Offset] = true;
                    mRefers[instruction.Next.Offset]++;
                }

                if (instruction.Next != null && instruction.OpCode.FlowControl == FlowControl.Next)
                {
                    mRefers[instruction.Next.Offset]++;
                }
            }
        }

        /// <summary>
        /// Creates blocks
        /// </summary>
        private void createBlocks()
        {
            // Add blocks
            BasicBlockRef currentBlock = LLVM.AppendBasicBlockInContext(Compiler.ModuleContext, Function, "entry");
            ILStack currentStack = CurrentStack;
            for (int i = 0; i < mBranch.Length; i++)
            {
                if (mBranch[i])
                {
                    currentBlock = LLVM.AppendBasicBlockInContext(Compiler.ModuleContext, Function, string.Format("L{0:x}", i));
                    currentStack = new ILStack();
                }

                mBlocks[i] = currentBlock;
                mStacks[i] = currentStack;
            }
        }

        /// <summary>
        /// Prepares the arguments
        /// </summary>
        private void prepareArguments()
        {
            uint count = LLVM.CountParams(Function);
            ArgumentValues = new ValueRef[count];
            ArgumentILTypes = new TypeReference[count];

            // It is possible that the first argument is an instance reference
            int offset = (Method.HasThis) ? 1 : 0;
            for (int i = offset; i < count; i++)
            {
                ArgumentValues[i] = LLVM.GetParam(Function, (uint)i);
                ArgumentILTypes[i] = Method.Parameters[i - offset].ParameterType;
            }

            // Instance reference
            if (Method.HasThis)
            {
                ArgumentValues[0] = LLVM.GetParam(Function, 0);
                ArgumentILTypes[0] = new PointerType(Method.DeclaringType);
            }
        }

        /// <summary>
        /// Initializes the context
        /// </summary>
        public void Init()
        {
            CurrentStack = new ILStack();
            findBlocks();
            createBlocks();
            prepareArguments();
        }
    }
}
