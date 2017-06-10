using Mono.Cecil;

namespace CSharpLLVM.Lookups
{
    class StructBarrierEntry : IStructEntry
    {
        public bool IsBarrier { get { return true; } }
        public bool IsField { get { return false; } }

        public TypeDefinition Type { get; private set; }

        /// <summary>
        /// Creates a new StructBarrierEntry.
        /// </summary>
        /// <param name="type">The type.</param>
        public StructBarrierEntry(TypeDefinition type)
        {
            Type = type;
        }
    }
}
