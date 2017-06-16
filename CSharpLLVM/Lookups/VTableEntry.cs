using Mono.Cecil;

namespace CSharpLLVM.Lookups
{
    class VTableEntry : IStructEntry
    {
        public StructEntryType EntryType { get { return StructEntryType.ClassVTable; } }
        public TypeDefinition Type { get; private set; }

        /// <summary>
        /// Creates a new VTableEntry.
        /// </summary>
        /// <param name="type">The type.</param>
        public VTableEntry(TypeDefinition type)
        {
            Type = type;
        }
    }
}
