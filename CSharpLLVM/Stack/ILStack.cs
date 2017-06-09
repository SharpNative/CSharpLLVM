using CSharpLLVM.Helpers;
using Swigged.LLVM;
using System.Collections.Generic;

namespace CSharpLLVM.Stack
{
    class ILStack
    {
        private List<StackElement> mStack = new List<StackElement>();
        private List<int> mPhi = new List<int>();
        private List<BasicBlockRef> oldBlocks = new List<BasicBlockRef>();

        public StackElement this[int index] { get { return mStack[index]; } }

        public int Count { get { return mStack.Count; } }
        public int IndependentValues { get; private set; }

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
            int index = mStack.Count - 1;
            StackElement top = mStack[index];

            mStack.RemoveAt(index);
            mPhi.RemoveAt(index);

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
        /// Gets the count of dependent items
        /// </summary>
        /// <returns></returns>
        public int GetDependentCount()
        {
            int sum = 0;
            for (int i = 0; i < mPhi.Count; i++)
                if (mPhi[i] > 0)
                    sum++;

            return sum;
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
            // If there's only one reference to this branch, there's only one way to get here.
            // That means the stack elements only depend on one other branch, therefor we don't need to build phi nodes.
            if (refers == 1)
            {
                for (int i = 0; i < srcStack.Count; i++)
                {
                    Push(new StackElement(srcStack[i]));
                }
            }
            // Multiple references.
            else
            {
                // We got three possible cases here:
                // 1. #deststack = #srcstack => build phi nodes for every element.
                // 2. #deststack > #srcstack => build phi nodes for the top elements of the srcstack.
                // 3. #deststack < #srcstack => build phi nodes for the top elements and put the rest on the stack as independent values.

                int dstOffset = Count - 1;
                int difference = 0;

                // Case 3.
                if (Count < srcStack.Count)
                {
                    difference = srcStack.Count - Count;

                    // Push independent values on the stack start.
                    for (int i = difference - 1; i >= 0; i--)
                    {
                        InsertAtStart(srcStack[i]);
                        oldBlocks.Insert(0, oldBlock);
                    }
                }

                difference += srcStack.GetDependentCount();

                for (int i = srcStack.Count - 1; i >= difference; i--)
                {
                    // Not a phi node yet. Transform to a phi node.
                    if (mPhi[dstOffset] == 0)
                    {
                        StackElement element = mStack[dstOffset];
                        ValueRef oldValue = element.Value;

                        LLVM.PositionBuilderAtEnd(builder, newBlock);
                        element.Value = LLVM.BuildPhi(builder, element.Type, "phi");
                        LLVM.PositionBuilderAtEnd(builder, oldBlock);
                        LLVM.AddIncoming(element.Value, new ValueRef[] { oldValue }, new BasicBlockRef[] { oldBlocks[i] });
                    }

                    ValueRef phi = mStack[dstOffset].Value;

                    // We might need to cast the incoming value to the phi type.
                    // This is because it is possible that an integer type of a smaller type is pushed on the stack.
                    // by IL, for example in "branch on condition".
                    TypeRef phiType = LLVM.TypeOf(phi);
                    ValueRef incomingValue = srcStack[i].Value;

                    // Cast if not the same type
                    if (srcStack[i].Type != phiType)
                    {
                        CastHelper.HelpIntAndPtrCast(builder, ref incomingValue, srcStack[i].Type, phiType);
                    }

                    // Add new incoming from source stack
                    LLVM.AddIncoming(phi, new ValueRef[] { incomingValue }, new BasicBlockRef[] { oldBlock });

                    mPhi[dstOffset]++;
                    dstOffset--;
                }
            }
        }
    }
}
