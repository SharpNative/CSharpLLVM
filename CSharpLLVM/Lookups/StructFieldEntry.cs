using Mono.Cecil;

namespace CSharpLLVM.Lookups
{
    class StructFieldEntry : IStructEntry
    {
        public bool IsBarrier { get { return false; } }
        public bool IsField { get { return true; } }

        public FieldDefinition Field { get; private set; }

        /// <summary>
        /// Creates a new StructFieldEntry
        /// </summary>
        /// <param name="field">The field</param>
        public StructFieldEntry(FieldDefinition field)
        {
            Field = field;
        }
    }
}
