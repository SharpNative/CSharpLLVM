using Mono.Cecil.Cil;

namespace CSharpLLVM.Helpers
{
    static class InstructionHelper
    {
        /// <summary>
        /// Checks if an instruction has a certain prefix
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="code">The prefix code</param>
        /// <returns>If the instruction has that certain prefix code</returns>
        public static bool HasPrefix(this Instruction instruction, Code code)
        {
            return (instruction.Previous != null && instruction.Previous.OpCode.Code == code);
        }
    }
}
