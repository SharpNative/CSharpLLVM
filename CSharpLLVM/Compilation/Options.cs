using CommandLine;
using CommandLine.Text;
using System.Reflection;

namespace CSharpLLVM.Compilation
{
    enum OptimizationLevel
    {
        O0,
        O1,
        O2
    }

    class Options
    {
        [Option('i', "input", Required = true, HelpText = "Input file to compile.")]
        public string InputFile { get; set; }

        [Option('o', "output", Required = true, HelpText = "The output file.")]
        public string OutputFile { get; set; }

        [Option("optimization", DefaultValue = OptimizationLevel.O1, Required = false, HelpText = "The optimization level.")]
        public OptimizationLevel Optimization { get; set; }

        [Option('v', "verbose", HelpText = "Print details during execution.")]
        public bool Verbose { get; set; }

        [Option('s', "asm", HelpText = "Output assembly code.", MutuallyExclusiveSet = "output")]
        public bool OutputAssembly { get; set; }

        [Option('l', "llvm", HelpText = "Output LLVM code.", MutuallyExclusiveSet = "output")]
        public bool OutputLLVM { get; set; }

        /// <summary>
        /// Returns the usage.
        /// </summary>
        /// <returns>The usage.</returns>
        [HelpOption]
        public string GetUsage()
        {
            HelpText help = new HelpText
            {
                Heading = new HeadingInfo("CSharpLLVM", Assembly.GetEntryAssembly().GetName().Version.ToString()),
                AddDashesToOption = true
            };
            help.AddOptions(this);
            return help;
        }
    }
}
