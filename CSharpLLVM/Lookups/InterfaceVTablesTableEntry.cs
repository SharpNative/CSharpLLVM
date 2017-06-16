using Mono.Cecil;

namespace CSharpLLVM.Lookups
{
    class InterfaceVTablesTableEntry : IStructEntry
    {
        public StructEntryType EntryType { get { return StructEntryType.InterfaceVTablesTable; } }
        public TypeReference[] Interfaces { get; private set; }

        /// <summary>
        /// Creates a new InterfaceVTablesTableEntry.
        /// </summary>
        /// <param name="interfaces">The interfaces.</param>
        public InterfaceVTablesTableEntry(TypeReference[] interfaces)
        {
            Interfaces = interfaces;
        }
    }
}
