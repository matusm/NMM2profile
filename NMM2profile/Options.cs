using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace Nmm2Profile
{
    class Options
    {
        [Option('c', "channel", DefaultValue = "-LZ+AZ", HelpText = "Channel to export.")]
        public string ChannelSymbol { get; set; }

        [Option("comment", DefaultValue = "*none*", HelpText = "User supplied comment string.")]
        public string UserComment { get; set; }

        [Option('q', "quiet", HelpText = "Quiet mode. No screen output (except for errors).")]
        public bool BeQuiet { get; set; }

        [Option('s', "scan", DefaultValue = 0, HelpText = "Scan index for multi-scan files.")]
        public int ScanIndex { get; set; }

        [Option('r', "reference", DefaultValue = 1, HelpText = "Height reference technique.")]
        public int ReferenceMode { get; set; }

        [Option('b', "bias", DefaultValue = 0.0, HelpText = "bias value [um] to be subtracted.")]
        public double Bias { get; set; }

        [Option("heydemann", HelpText = "Perform Heydemann correction.")]
        public bool DoHeydemann { get; set; }

        [Option("back", HelpText = "Use backtrace profile (when present).")]
        public bool UseBack { get; set; }

        [Option("both", HelpText = "Mean of forward and backtrace profile (when present).")]
        public bool UseBoth { get; set; }

        [Option("diff", HelpText = "Difference (forward-backtrace) profile (when present).")]
        public bool UseDiff { get; set; }

        [Option('p', "profile", DefaultValue = 0, HelpText = "Extract single profile. (0 for all)")]
        public int ProfileIndex { get; set; }

        [Option("sdf",  HelpText = "Convert to SDF file format (ISO 25178-71, EUNA 15178).")]
        public bool convertBcr { get; set; }

        [Option("txt",  HelpText = "Convert to basic TXT format (by NPL).")]
        public bool convertTxt { get; set; }

        [Option("sig", HelpText = "Convert to SIG file format used by freeware SigmaSurf.")]
        public bool convertSig { get; set; }

        [Option("prf",  HelpText = "Convert to PRF file format (by NPL).")]
        public bool convertPrf { get; set; }

        [Option("prEN",  HelpText = "Convert to PR file format (by PTB, english).")]
        public bool convertPrEn { get; set; }

        [Option("prDE",  HelpText = "Convert to PR file format (by PTB, german).")]
        public bool convertPrDe { get; set; }

        [Option("smd",  HelpText = "Convert to SMD file format (ISO 5436-2).")]
        public bool convertSmd { get; set; }

        [Option("x3p",  HelpText = "Convert to X3P file format (ISO 25178-72).")]
        public bool convertX3p { get; set; }


        [ValueList(typeof(List<string>), MaximumElements = 2)]
        public IList<string> ListOfFileNames { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            string AppName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            string AppVer = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

            HelpText help = new HelpText
            {
                Heading = new HeadingInfo(AppName, "version " + AppVer),
                Copyright = new CopyrightInfo("Michael Matus", 2020),
                AdditionalNewLineAfterOption = false,
                AddDashesToOption = true
            };
            string sPre = "Program to convert scanning files by SIOS NMM-1 to files readable by standard surface profiling software. " +
                "For input files containing multiple line profiles (raster files), a single profile is extracted. " +
                "Eight different output files formats can be choosen. " +
                "A rudimentary data processing is possible via the -r option.";
            help.AddPreOptionsLine(sPre);
            help.AddPreOptionsLine("");
            help.AddPreOptionsLine("Usage: " + AppName + " filename1 [filename2] [options]");
            help.AddPostOptionsLine("");
            help.AddPostOptionsLine("Supported values for --reference (-r):");
            help.AddPostOptionsLine("    0: nop");
            help.AddPostOptionsLine("    1: min");
            help.AddPostOptionsLine("    2: max");
            help.AddPostOptionsLine("    3: average");
            help.AddPostOptionsLine("    4: mid");
            help.AddPostOptionsLine("    5: bias");
            help.AddPostOptionsLine("    6: first");
            help.AddPostOptionsLine("    7: last");
            help.AddPostOptionsLine("    8: center");
            help.AddPostOptionsLine("    9: linear");
            help.AddPostOptionsLine("   10: LSQ");
            help.AddPostOptionsLine("   11: linear(positive)");
            help.AddPostOptionsLine("   12: LSQ(positive)");

            help.AddOptions(this);

            return help;
        }
    }
}