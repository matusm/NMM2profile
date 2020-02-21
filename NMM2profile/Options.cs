using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace NMM2profile
{
    class Options
    {
        [Option('c', "channel", DefaultValue = "-LZ+AZ", HelpText = "Channel to export.")]
        public string ChannelSymbol { get; set; }

        [Option('s', "scan", DefaultValue = 0, HelpText = "Scan index for multi-scan files.")]
        public int ScanIndex { get; set; }

        [Option('r', "reference", DefaultValue = 1, HelpText = "Height reference technique.")]
        public int ReferenceMode { get; set; }

        [Option('b', "bias", DefaultValue = 0.0, HelpText = "bias value [um] to be subtracted.")]
        public double Bias { get; set; }

        [Option('q', "quiet", HelpText = "Quiet mode. No screen output (except for errors).")]
        public bool BeQuiet { get; set; }

        [Option('f', "filetype", DefaultValue = 2, HelpText = "Type of output file.")]
        public int FileTypeNumber { get; set; }

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
                Copyright = new CopyrightInfo("Michael Matus", 2015),
                AdditionalNewLineAfterOption = false,
                AddDashesToOption = true
            };
            string sPre = "Program to convert scanning files by SIOS NMM-1 to files readable by standard surface profiling software. " +
                "For input files containing multiple line profiles (raster files), a single profile is extracted. " +
                "Six different output files formats can be choosen. " +
                "A rudimentary data processing is possible via the -r option.";
            help.AddPreOptionsLine(sPre);
            help.AddPreOptionsLine("");
            help.AddPreOptionsLine("Usage: " + AppName + " filename1 [filename2] [options]");
            help.AddPostOptionsLine("");
            help.AddPostOptionsLine("Supported values for -f: 1=*.txt, 2=*.sig, 3=*.prf, 4=*.pr, 5=*.sdf, 6=*.smd");
            help.AddPostOptionsLine("Supported values for -r: 1=min 2=max 3=average 4=mid 5=bias 6=first 7=last 8=center 9=linear 10=LSQ 11=linear(positive) 12=LSQ(positive)");

            help.AddOptions(this);

            return help;
        }
    }
}