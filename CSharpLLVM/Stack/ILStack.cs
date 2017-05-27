using CSharpLLVM.Helpers;
using Swigged.LLVM;
using System.Collections.Generic;

namespace CSharpLLVM.Stack
{
    class ILStack
    {
        private List<StackElement> mStack;
        private List<int> mPhi;
        private BasicBlockRef mOldBlock;

        public StackElement this[int index] { get { return mStack[index]; } }

        public int Count { get { return mStack.Count; } }

        /// <summary>
        /// Creates a new ILStack
        /// </summary>
        public ILStack()
        {
            mStack = new List<StackElement>();
            mPhi = new List<int>();
        }

        /// <summary>
        /// Clears the stack
        /// </summary>
        public void Clear()
        {
            mStack.Clear();
            mPhi.Clear();
        }

        /// <summary>
        /// Insert an element at start
        /// </summary>
        /// <param name="element">The element</param>
        public void InsertAtStart(StackElement element)
        {
            mStack.Insert(0, element);
            mPhi.Insert(0, 0);
        }

        /// <summary>
        /// Pops an element from the stack
        /// </summary>
        /// <returns>The popped element</returns>
        public StackElement Pop()
        {
            mPhi.RemoveAt(mPhi.Count - 1);
            StackElement top = mStack[mStack.Count - 1];
            mStack.RemoveAt(mStack.Count - 1);
            return top;
        }

        /// <summary>
        /// Returns the top element without popping it
        /// </summary>
        /// <returns>The top element</returns>
        public StackElement Peek()
        {
            return mStack[mStack.Count - 1];
        }

        /// <summary>
        /// Pushes an element on the stack
        /// </summary>
        /// <param name="element">The element</param>
        public void Push(StackElement element)
        {
            mPhi.Add(0);
            mStack.Add(element);
        }
        
        /// <summary>
        /// Update stack with phi nodes
        /// </summary>
        /// <param name="builder">The builder</param>
        /// <param name="srcStack">The source stack</param>
        /// <param name="oldBlock">The old block</param>
        /// <param name="newBlock">The new block</param>
        /// <param name="refers">The amount of references to the new block</param>
        public void Update(BuilderRef builder, ILStack srcStack, BasicBlockRef oldBlock, BasicBlockRef newBlock, int refers)
        {
            // If only one reference, just push the values
            if (refers <= 1)
            {
                for (int i = 0; i < srcStack.Count; i++)
                {
                    Push(new StackElement(srcStack[i]));
                }
            }
            // Multiple references, need to build phi nodes
            else
            {
                // Calculate independent values (not changed by this operation)
                int difference = 0;
                if (Count > 0 && srcStack.Count > Count)
                {
                    difference = srcStack.Count - Count;
                }

                // Insert values that are independent
                for (int i = 0; i < difference; i++)
                {
                    InsertAtStart(new StackElement(srcStack[i]));
                }

                // Insert dependent values
                for (int i = difference; i < srcStack.Count; i++)
                {
                    // First time we're here, so that means no dependencies on this value yet
                    if (mPhi.Count <= i || mPhi[i] == 0)
                    {
                        Push(new StackElement(srcStack[i]));
                        mOldBlock = oldBlock;
                    }
                    // Use phi
                    else if (mPhi[i] >= 1)
                    {
                        ValueRef phi;

                        // Second time for this value, so it depends on two blocks
                        if (mPhi[i] == 1)
                        {
                            StackElement first = Pop();

                            LLVM.PositionBuilderAtEnd(builder, newBlock);
                            phi = LLVM.BuildPhi(builder, first.Type, "phi");
                            LLVM.PositionBuilderAtEnd(builder, oldBlock);
                            Push(new StackElement(phi, first.ILType, first.Type));
                            LLVM.AddIncoming(phi, new ValueRef[] { first.Value }, new BasicBlockRef[] { mOldBlock });
                        }
                        // > 2 times we've been here for this value, so it depends on multiple blocks
                        else
                        {
                            phi = this[i].Value;
                        }

                        /**
                         * We might need to cast the incoming value to the phi type
                         * This is because it is possible that an integer type of a smaller type is pushed on the stack
                         * by IL, for example in "branch on condition"
                        */
                        TypeRef phiType = LLVM.TypeOf(phi);
                        ValueRef newValue = srcStack[i].Value;
                        
                        // Cast if not the same type
                        if (srcStack[i].Type != phiType)
                        {
                            CastHelper.HelpIntAndPtrCast(builder, ref newValue, srcStack[i].Type, phiType);
                        }

                        // Add incoming block for the phi
                        LLVM.AddIncoming(phi, new ValueRef[] { newValue }, new BasicBlockRef[] { oldBlock });
                    }

                    mPhi[i]++;
                }
            }
        }
    }
}
