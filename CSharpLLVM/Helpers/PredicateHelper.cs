using Mono.Cecil.Cil;
using Swigged.LLVM;
using System;

namespace CSharpLLVM.Helpers
{
    static class PredicateHelper
    {
        /// <summary>
        /// Gets an int predicate from a code.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <returns>The predicate.</returns>
        public static IntPredicate GetIntPredicateFromCode(Code code)
        {
            switch (code)
            {
                case Code.Brfalse:
                case Code.Brfalse_S:
                    return IntPredicate.IntNE;

                case Code.Brtrue:
                case Code.Brtrue_S:
                    return IntPredicate.IntEQ;

                case Code.Clt:
                    return IntPredicate.IntSLT;

                case Code.Clt_Un:
                    return IntPredicate.IntULT;

                case Code.Ceq:
                    return IntPredicate.IntEQ;

                case Code.Cgt:
                    return IntPredicate.IntSGT;

                case Code.Cgt_Un:
                    return IntPredicate.IntUGT;

                case Code.Beq:
                case Code.Beq_S:
                    return IntPredicate.IntEQ;

                case Code.Bge:
                case Code.Bge_S:
                    return IntPredicate.IntSGE;

                case Code.Bge_Un:
                case Code.Bge_Un_S:
                    return IntPredicate.IntUGE;

                case Code.Bgt:
                case Code.Bgt_S:
                    return IntPredicate.IntSGT;

                case Code.Bgt_Un:
                case Code.Bgt_Un_S:
                    return IntPredicate.IntUGT;

                case Code.Ble:
                case Code.Ble_S:
                    return IntPredicate.IntSLE;

                case Code.Ble_Un:
                case Code.Ble_Un_S:
                    return IntPredicate.IntULE;

                case Code.Blt:
                case Code.Blt_S:
                    return IntPredicate.IntSLT;

                case Code.Blt_Un:
                case Code.Blt_Un_S:
                    return IntPredicate.IntULT;

                case Code.Bne_Un:
                case Code.Bne_Un_S:
                    return IntPredicate.IntNE;

                default:
                    throw new Exception("Invalid code for IntPredicate: " + code);
            }
        }

        /// <summary>
        /// Gets a real predicate from a code.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <returns>The predicate.</returns>
        public static RealPredicate GetRealPredicateFromCode(Code code)
        {
            switch (code)
            {
                case Code.Clt:
                    return RealPredicate.RealOLT;

                case Code.Clt_Un:
                    return RealPredicate.RealULT;

                case Code.Ceq:
                    return RealPredicate.RealOEQ;

                case Code.Cgt:
                    return RealPredicate.RealOGT;

                case Code.Cgt_Un:
                    return RealPredicate.RealUGT;

                case Code.Beq:
                case Code.Beq_S:
                    return RealPredicate.RealOEQ;

                case Code.Bge:
                case Code.Bge_S:
                    return RealPredicate.RealOGE;

                case Code.Bge_Un:
                case Code.Bge_Un_S:
                    return RealPredicate.RealUGE;

                case Code.Bgt:
                case Code.Bgt_S:
                    return RealPredicate.RealOGT;

                case Code.Bgt_Un:
                case Code.Bgt_Un_S:
                    return RealPredicate.RealUGT;

                case Code.Ble:
                case Code.Ble_S:
                    return RealPredicate.RealOLE;

                case Code.Ble_Un:
                case Code.Ble_Un_S:
                    return RealPredicate.RealULE;

                case Code.Blt:
                case Code.Blt_S:
                    return RealPredicate.RealOLT;

                case Code.Blt_Un:
                case Code.Blt_Un_S:
                    return RealPredicate.RealULT;

                case Code.Bne_Un:
                case Code.Bne_Un_S:
                    return RealPredicate.RealUNE;

                default:
                    throw new Exception("Invalid code for RealPredicate: " + code);
            }
        }
    }
}
