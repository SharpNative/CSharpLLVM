namespace CSharpLLVM.Lookups
{
    enum StructEntryType
    {
        Field,
        ClassVTable,
        InterfaceVTablesTable
    }

    interface IStructEntry
    {
        StructEntryType EntryType { get; }
    }
}
