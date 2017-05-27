using CSharpLLVM.Helpers;
using Swigged.LLVM;
using System.Collections.Generic;

namespace CSharpLLVM.Stack
{
    class ILStack
    {
        private List<StackElement> mstack;
        private List<int> mphi;
        private BasicBlockRef moldBlock;

        public StackElement this[int index] { get { return mstack[index]; } }

        public int Count { get { return mstack.Count; } }

        /// <summary>
        /// Creates a new ILStack
        /// </summary>
        public ILStack()
        {
            mstack = new List<StackElement>();
            mphi = new List<int>();
        }

        /// <summary>
        /// Clears the stack
        /// </summary>
        public void Clear()
        {
            mstack.Clear();
            mphi.Clear();
        }

        /// <summary>
        /// Insert an element at start
        /// </summary>
        /// <param name="element">The element</param>
        public void InsertAtStart(StackElement element)
        {
            mstack.Insert(0, element);
            mphi.Insert(0, 0);
        }

        /// <summary>
        /// Pops an element from the stack
        /// </summary>
        /// <returns>The popped element</returns>
        public StackElement Pop()
        {
            mphi.RemoveAt(mphi.Count - 1);
            StackElement top = mstack[mstack.Count - 1];
            mstack.RemoveAt(mstack.Count - 1);
            return top;
        }

        /// <summary>
        /// Returns the top element without popping it
        /// </summary>
        /// <returns>The top element</returns>
        public StackElement Peek()
        {
            return mstack[mstack.Count - 1];
        }

        /// <summary>
        /// Pushes an element on the stack
        /// </summary>
        /// <param name="element">The element</param>
        public void Push(StackElement element)
        {
            mphi.Add(0);
            mstack.Add(element);
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
                    if (mphi.Count <= i || mphi[i] == 0)
                    {
                        Push(new StackElement(srcStack[i]));
                        moldBlock = oldBlock;
                    }
                    // Use phi
                    else if (mphi[i] >= 1)
                    {
                        ValueRef phi;

                        // Second time for this value, so it depends on two blocks
                        if (mphi[i] == 1)
                        {
                            StackElement first = Pop();

                            LLVM.PositionBuilderAtEnd(builder, newBlock);
                            phi = LLVM.BuildPhi(builder, first.Type, "phi");
                            LLVM.PositionBuilderAtEnd(builder, oldBlock);
                            Push(new StackElement(phi, first.ILType, first.Type));
                            LLVM.AddIncoming(phi, new ValueRef[] { first.Value }, new BasicBlockRef[] { moldBlock });
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

                    mphi[i]++;
                }
            }
        }
    }
}
