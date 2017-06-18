using System;

namespace CSharpLLVM.Compilation
{
    enum TypeKind
    {
        Struct,
        Enum,
        Class,
        Interface
    }

    static class TypeKindHelper
    {
        /// <summary>
        /// Gets the color of a type kind.
        /// </summary>
        /// <param name="typeKind">The type kind.</param>
        /// <returns>The color.</returns>
        public static ConsoleColor GetColor(this TypeKind typeKind)
        {
            switch (typeKind)
            {
                case TypeKind.Struct:
                    return ConsoleColor.DarkCyan;
                case TypeKind.Enum:
                    return ConsoleColor.DarkGreen;
                case TypeKind.Class:
                    return ConsoleColor.Cyan;
                case TypeKind.Interface:
                    return ConsoleColor.DarkMagenta;
                default:
                    return ConsoleColor.Black;
            }
        }
    }
}
