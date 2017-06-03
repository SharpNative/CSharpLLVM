using CSharpLLVM.Compiler;
using System;
using System.IO;
using System.Reflection;

namespace CSharpLLVM
{
    class Program
    {
        /// <summary>
        /// Prints usage
        /// </summary>
        private static void printUsage()
        {
            // TODO
        }

        /// <summary>
        /// Entrypoint
        /// </summary>
        /// <param name="args">Arguments</param>
        static void Main(string[] args)
        {
            Version version = Assembly.GetEntryAssembly().GetName().Version;
            Console.WriteLine("CSharpLLVM version " + version);

            // No input?
            if (args.Length == 0)
            {
                Console.WriteLine("No input");
                printUsage();
                return;
            }

            string path = args[0];
            string moduleName = Path.GetFileNameWithoutExtension(path);

            CompilerSettings settings = new CompilerSettings()
            {
                InputFile = path,
                ModuleName = moduleName
            };

            Compiler.Compiler compiler = new Compiler.Compiler(settings);
            compiler.Compile();

            Console.ReadLine();
        }
    }
}
