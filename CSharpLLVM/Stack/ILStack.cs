using Swigged.LLVM;
using System;
using System.Collections.Generic;

namespace CSharpLLVM.Stack
{
    class ILStack
    {
        private List<StackElement> mStack;
        private List<int> mPhi;

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
            // This shouldn't happen...
            if (refers == 0)
                throw new InvalidOperationException("refers == 0");

            // If there's only one reference to this branch, there's only one way to get here.
            // That means the stack elements only depend on one other branch, therefor we don't need to build phi nodes.
            if (refers == 1)
            {
                for (int i = 0; i < srcStack.Count; i++)
                {
                    Push(new StackElement(srcStack[i]));
                }
            }
            // Multiple references, need to build phi nodes.
            else
            {
                // TODO: we got three cases here:
                //       1. #deststack = #srcstack => build phi nodes for every element.
                //       2. #deststack > #srcstack => build phi nodes for the top elements of the srcstack.
                //       3. #deststack < #srcstack => build phi nodes for the top elements and put the rest on the stack as independent values.

                throw new NotImplementedException("WIP");
            }
        }
    }
}
