namespace CSharpLLVM.Lookups
{
    interface IStructEntry
    {
        bool IsField { get; }
        bool IsBarrier { get; }
    }
}
