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

        [Option('l', "llvm-ir", HelpText = "Output LLVM IR code.", MutuallyExclusiveSet = "output")]
        public bool OutputLLVMIR { get; set; }

        [Option('b', "llvm-bitcode", HelpText = "Output LLVM bitcode.", MutuallyExclusiveSet = "output")]
        public bool OutputLLVMBitCode { get; set; }

        [Option("instance-methods-internal", HelpText = "Sets the linkage of instance methods to internal linkage.")]
        public bool InstanceMethodInternalLinkage { get; set; }

        [Option("internal-methods-fastcc", HelpText = "This option will make methods with internal linkage use the fastcc calling convention.")]
        public bool InternalMethodsFastCC { get; set; }

        [Option("target", DefaultValue = "default", HelpText = "The target triplet of the outputted code.")]
        public string Target { get; set; }

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
