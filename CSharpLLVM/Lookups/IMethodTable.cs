using Mono.Cecil;

namespace CSharpLLVM.Lookups
{
    interface IMethodTable
    {
        /// <summary>
        /// Gets the method index of a method
        /// </summary>
        /// <param name="method">The method</param>
        /// <returns>The index</returns>
        int GetMethodIndex(MethodReference method);

        /// <summary>
        /// Gets the method index of a method
        /// </summary>
        /// <param name="methodName">The method name</param>
        /// <returns>The index</returns>
        int GetMethodIndex(string methodName);

        /// <summary>
        /// Checks if this table has an entry for a method
        /// </summary>
        /// <param name="method">The method</param>
        /// <returns>If the table has an entry for that method</returns>
        bool HasMethod(MethodReference method);

        /// <summary>
        /// Checks if this table has an entry for a method
        /// </summary>
        /// <param name="methodName">The method name</param>
        /// <returns>If the table has an entry for that method</returns>
        bool HasMethod(string methodName);

        /// <summary>
        /// Used to show the table (for debug)
        /// </summary>
        void Dump();
    }
}
