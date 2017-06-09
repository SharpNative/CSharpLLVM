using CSharpLLVM.Stack;
using Mono.Cecil.Cil;
using Swigged.LLVM;
using System.Collections.Generic;

namespace CSharpLLVM.Compilation
{
    class Branch
    {
        private MethodContext mContext;

        private List<Instruction> mInstructions = new List<Instruction>();
        private List<Branch> mSources = new List<Branch>();

        public BasicBlockRef Block { get; private set; }
        public ILStack Stack { get; private set; }
        public Instruction[] Instructions { get { return mInstructions.ToArray(); } }
        public Branch[] Sources { get { return mSources.ToArray(); } }
        public int References { get { return mSources.Count; } }
        public int Offset { get; private set; }
        public int Generation { get; set; }

        /// <summary>
        /// Creates a new Branch
        /// </summary>
        /// <param name="context">The method context of the method where this branch belongs</param>
        /// <param name="offset">The offset of this branch (instruction offset)</param>
        public Branch(MethodContext context, int offset)
        {
            Offset = offset;
            mContext = context;
            createBlock();
            Stack = new ILStack();
        }

        /// <summary>
        /// Adds an instruction to this branch
        /// </summary>
        /// <param name="instruction">The instruction</param>
        public void AddInstruction(Instruction instruction)
        {
            mInstructions.Add(instruction);
        }
        
        /// <summary>
        /// Adds a source branch to this branch
        /// </summary>
        /// <param name="branch">The source branch</param>
        public void AddSource(Branch branch)
        {
            mSources.Add(branch);
        }

        /// <summary>
        /// Updates the stack of this branch using the incoming source branches
        /// </summary>
        /// <param name="builder">The builder</param>
        public void UpdateStack(BuilderRef builder)
        {
            foreach (Branch source in Sources)
            {
                Stack.Update(builder, source.Stack, source.Block, Block, References);
            }
        }

        /// <summary>
        /// Creates the block
        /// </summary>
        private void createBlock()
        {
            Block = LLVM.AppendBasicBlockInContext(mContext.Compiler.ModuleContext, mContext.Function, string.Format("IL_{0:x4}", Offset));
        }
    }
}
