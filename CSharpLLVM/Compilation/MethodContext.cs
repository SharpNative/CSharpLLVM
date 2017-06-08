using CSharpLLVM.Stack;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Swigged.LLVM;

namespace CSharpLLVM.Compilation
{
    class MethodContext
    {
        public Compiler Compiler { get; private set; }
        public ILStack CurrentStack { get; set; }
        public MethodDefinition Method { get; private set; }
        public ValueRef Function { get; private set; }

        public Branch[] Branches { get; private set; }
        public ValueRef[] LocalValues { get; set; }
        public TypeRef[] LocalTypes { get; set; }
        public TypeReference[] LocalILTypes { get; set; }
        public ValueRef[] ArgumentValues { get; set; }
        public TypeReference[] ArgumentILTypes { get; set; }

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
        /// Returns true if an instruction belongs to a new branch
        /// </summary>
        /// <param name="instr">The instruction</param>
        /// <returns>If it belongs to a new branch</returns>
        public bool IsNewBranch(Instruction instr)
        {
            return (instr != null && GetBranch(instr) != null);
        }

        /// <summary>
        /// Gets the branch where the instruction belongs to
        /// </summary>
        /// <param name="instr">The instruction</param>
        /// <returns>The branch</returns>
        public Branch GetBranch(Instruction instr)
        {
            return Branches[instr.Offset];
        }

        /// <summary>
        /// Gets the block where the instruction belongs to
        /// </summary>
        /// <param name="instr">The instruction</param>
        /// <returns>The block</returns>
        public BasicBlockRef GetBlockOf(Instruction instr)
        {
            return GetBranch(instr).Block;
        }
        
        /// <summary>
        /// Creates the branches
        /// </summary>
        private void createBranches()
        {
            // Look for branching, create blocks for the branches
            // Note: we first search all the branches so we can later add them in the correct order
            //       this is because we may not always have the branches in a chronological order
            bool[] isNewBranch = new bool[Method.Body.CodeSize];
            int[] refers = new int[Method.Body.CodeSize];
            isNewBranch[0] = true;
            Branches = new Branch[Method.Body.CodeSize];

            foreach (Instruction instruction in Method.Body.Instructions)
            {
                FlowControl flow = instruction.OpCode.FlowControl;

                // If this instruction branches to a destination, create that destination block
                if (flow == FlowControl.Branch || flow == FlowControl.Cond_Branch)
                {
                    Instruction dest = (Instruction)instruction.Operand;
                    isNewBranch[dest.Offset] = true;
                    refers[dest.Offset]++;
                }

                if (instruction.Next != null)
                {
                    // If this instruction does branching by a conditional, we also need to have a block after this instruction
                    // for if the conditional branch is not being executed
                    if (flow == FlowControl.Cond_Branch)
                        isNewBranch[instruction.Next.Offset] = true;
                    else if (flow == FlowControl.Next)
                        refers[instruction.Next.Offset]++;
                }
            }
            
            // Create branches
            for (int i = 0; i < Branches.Length; i++)
            {
                if (isNewBranch[i])
                    Branches[i] = new Branch(this, i);
            }

            // Now that we know the reference count and where to put branches, let's create them
            // For more explanation: refer to the loop above
            Branch current = Branches[0];
            foreach (Instruction instruction in Method.Body.Instructions)
            {
                FlowControl flow = instruction.OpCode.FlowControl;

                if (isNewBranch[instruction.Offset])
                {
                    current = Branches[instruction.Offset];
                }

                current.AddInstruction(instruction);

                if (flow == FlowControl.Branch || flow == FlowControl.Cond_Branch)
                {
                    Instruction dest = (Instruction)instruction.Operand;
                    Branch destBranch = Branches[dest.Offset];
                    destBranch.AddSource(current);
                }

                if (instruction.Next != null)
                {
                    Branch destBranch = Branches[instruction.Next.Offset];
                    if (flow == FlowControl.Cond_Branch || refers[instruction.Next.Offset] > 1)
                        destBranch.AddSource(current);
                }
            }
        }

        /// <summary>
        /// Prepares the arguments
        /// </summary>
        private void prepareArguments()
        {
            BuilderRef builder = LLVM.CreateBuilderInContext(Compiler.ModuleContext);
            LLVM.PositionBuilderAtEnd(builder, Branches[0].Block);

            uint count = LLVM.CountParams(Function);
            ArgumentValues = new ValueRef[count];
            ArgumentILTypes = new TypeReference[count];

            // It is possible that the first argument is an instance reference
            int offset = (Method.HasThis) ? 1 : 0;
            for (int i = offset; i < count; i++)
            {
                ValueRef param = LLVM.GetParam(Function, (uint)i);
                ArgumentILTypes[i] = Method.Parameters[i - offset].ParameterType;
                ArgumentValues[i] = LLVM.BuildAlloca(builder, LLVM.TypeOf(param), "arg" + i);
                LLVM.BuildStore(builder, param, ArgumentValues[i]);
            }

            // Instance reference
            if (Method.HasThis)
            {
                ValueRef param = LLVM.GetParam(Function, 0);
                ArgumentILTypes[0] = new PointerType(Method.DeclaringType);
                ArgumentValues[0] = LLVM.BuildAlloca(builder, LLVM.TypeOf(param), "arg0");
                LLVM.BuildStore(builder, param, ArgumentValues[0]);
            }

            LLVM.DisposeBuilder(builder);
        }

        /// <summary>
        /// Initializes the context
        /// </summary>
        public void Init()
        {
            CurrentStack = new ILStack();
            createBranches();
            prepareArguments();
        }
    }
}
