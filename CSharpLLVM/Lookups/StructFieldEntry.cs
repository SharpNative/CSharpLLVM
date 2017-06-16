using Mono.Cecil;

namespace CSharpLLVM.Lookups
{
    class StructFieldEntry : IStructEntry
    {
        public StructEntryType EntryType { get { return StructEntryType.Field; } }
        public FieldDefinition Field { get; private set; }

        /// <summary>
        /// Creates a new StructFieldEntry.
        /// </summary>
        /// <param name="field">The field.</param>
        public StructFieldEntry(FieldDefinition field)
        {
            Field = field;
        }
    }
}
