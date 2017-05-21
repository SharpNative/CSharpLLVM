using Swigged.LLVM;
using System.Collections.Generic;

namespace CSharpLLVM.Stack
{
    class ILStack
    {
        private List<StackElement> m_stack;
        private List<int> m_phi;
        private BasicBlockRef m_oldBlock;

        /// <summary>
        /// Creates a new ILStack
        /// </summary>
        public ILStack()
        {
            m_stack = new List<StackElement>();
            m_phi = new List<int>();
        }
        
        /// <summary>
        /// Clears the stack
        /// </summary>
        public void Clear()
        {
            m_stack.Clear();
            m_phi.Clear();
        }

        /// <summary>
        /// Insert an element at start
        /// </summary>
        /// <param name="element">The element</param>
        public void InsertAtStart(StackElement element)
        {
            m_stack.Insert(0, element);
            m_phi.Insert(0, 0);
        }
        
        /// <summary>
        /// Pops an element from the stack
        /// </summary>
        /// <returns>The popped element</returns>
        public StackElement Pop()
        {
            m_phi.RemoveAt(m_phi.Count - 1);
            StackElement a = m_stack[m_stack.Count - 1];
            m_stack.RemoveAt(m_stack.Count - 1);
            return a;
        }

        /// <summary>
        /// Pushes an element on the stack
        /// </summary>
        /// <param name="element">The element</param>
        public void Push(StackElement element)
        {
            m_phi.Add(0);
            m_stack.Add(element);
        }

        /// <summary>
        /// Pushes a value as an element on the stack
        /// </summary>
        /// <param name="value">The value</param>
        public void Push(ValueRef value)
        {
            Push(new StackElement(value));
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
            StackElement[] src = srcStack.ToArray();
            StackElement[] dst = ToArray();

            // If only one reference, just push the values
            if (refers <= 1)
            {
                for (int i = 0; i < src.Length; i++)
                {
                    Push(src[i].Value);
                }
            }
            // Multiple references, need to build phi nodes
            else
            {
                // Calculate independent values (not changed by this operation)
                int difference = 0;
                if (dst.Length > 0 && src.Length > dst.Length)
                {
                    difference = src.Length - dst.Length;
                }

                // Insert values that are independent
                for (int i = 0; i < difference; i++)
                {
                    InsertAtStart(new StackElement(src[i].Value));
                }

                // Insert dependent values
                for (int i = difference; i < src.Length; i++)
                {
                    // First time we're here, so that means no dependencies on this value yet
                    if (m_phi.Count <= i || m_phi[i] == 0)
                    {
                        Push(src[i].Value);
                        m_oldBlock = oldBlock;
                    }
                    // Use phi
                    else if (m_phi[i] >= 1)
                    {
                        ValueRef phi;

                        // Second time for this value, so it depends on two blocks
                        if (m_phi[i] == 1)
                        {
                            StackElement first = Pop();

                            LLVM.PositionBuilderAtEnd(builder, newBlock);
                            phi = LLVM.BuildPhi(builder, first.Type, "phi");
                            LLVM.PositionBuilderAtEnd(builder, oldBlock);
                            Push(phi);
                            LLVM.AddIncoming(phi, new ValueRef[] { first.Value }, new BasicBlockRef[] { m_oldBlock });
                        }
                        // > 2 times we've been here for this value, so it depends on multiple blocks
                        else
                        {
                            phi = dst[i].Value;
                        }

                        /**
                         * We might need to cast the incoming value to the phi type
                         * This is because it is possible that an integer type of a smaller type is pushed on the stack
                         * by IL, for example in "branch on condition"
                        */
                        TypeRef phiType = LLVM.TypeOf(phi);
                        ValueRef newValue = src[i].Value;

                        if (src[i].Type != phiType)
                        {
                            newValue = LLVM.BuildIntCast(builder, newValue, phiType, "phicast");
                        }

                        // Add incoming block for the phi
                        LLVM.AddIncoming(phi, new ValueRef[] { newValue }, new BasicBlockRef[] { oldBlock });
                    }

                    m_phi[i]++;
                }
            }
        }

        /// <summary>
        /// Returns an array containing the elements on the stack
        /// </summary>
        /// <returns>The array</returns>
        public StackElement[] ToArray()
        {
            return m_stack.ToArray();
        }
    }
}
