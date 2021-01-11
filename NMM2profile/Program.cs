using Bev.IO.NmmReader;
using Bev.IO.NmmReader.scan_mode;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace Nmm2Profile
{
    class MainClass
    {
        // some objects will be needed in methods other than Main()
        static readonly Options options = new Options();
        static readonly ProfileDataPod prf = new ProfileDataPod();
        static NmmFileName nmmFileNameObject;
        static NmmScanData theData;
        static TopographyProcessType topographyProcessType;
        static string[] fileNames;

        public static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            if (!CommandLine.Parser.Default.ParseArgumentsStrict(args, options))
                Console.WriteLine("*** ParseArgumentsStrict returned false");
            // consume the verbosity option
            if (options.BeQuiet == true)
                ConsoleUI.BeSilent();
            else
                ConsoleUI.BeVerbatim();
            // print a welcome message
            ConsoleUI.Welcome();
            ConsoleUI.WriteLine();
            // get the filename(s)
            fileNames = options.ListOfFileNames.ToArray();
            if (fileNames.Length == 0)
                ConsoleUI.ErrorExit("!Missing input file", 1);

            // if no filetype was choosen, use default one
            if (!(options.convertBcr ||
                options.convertPrDe ||
                options.convertPrEn ||
                options.convertPrf ||
                options.convertSig ||
                options.convertSmd ||
                options.convertTxt ||
                options.convertCsv ||
                options.convertX3p)) options.convertSig = true; // this should be convertBcr in the future

            // read all relevant scan data
            ConsoleUI.StartOperation("Reading and evaluating files");
            nmmFileNameObject = new NmmFileName(fileNames[0]);
            nmmFileNameObject.SetScanIndex(options.ScanIndex);
            theData = new NmmScanData(nmmFileNameObject);
            ConsoleUI.Done();
            ConsoleUI.WriteLine();

            if (options.DoHeydemann)
            {
                theData.ApplyHeydemannCorrection();
                if (theData.HeydemannCorrectionApplied)
                {
                    ConsoleUI.WriteLine($"Heydemann correction applied, span {theData.HeydemannCorrectionSpan * 1e9:F1} nm");
                }
                else
                {
                    ConsoleUI.WriteLine($"Heydemann correction not successful.");
                }
                ConsoleUI.WriteLine();
            }

            // some checks of the provided CLA options
            if (options.ProfileIndex < 0)
                options.ProfileIndex = 0;   // automatically extract all profiles
            if (options.ProfileIndex > theData.MetaData.NumberOfProfiles)
                options.ProfileIndex = theData.MetaData.NumberOfProfiles;

            topographyProcessType = TopographyProcessType.ForwardOnly;
            if (options.UseBack)
                topographyProcessType = TopographyProcessType.BackwardOnly;
            if (options.UseBoth)
                topographyProcessType = TopographyProcessType.Average;
            if (options.UseDiff)
                topographyProcessType = TopographyProcessType.Difference;
            if (theData.MetaData.ScanStatus == ScanDirectionStatus.ForwardOnly)
            {
                if (topographyProcessType != TopographyProcessType.ForwardOnly)
                    ConsoleUI.WriteLine("No backward scan data present, switching to forward only.");
                topographyProcessType = TopographyProcessType.ForwardOnly;
            }
            if (theData.MetaData.ScanStatus == ScanDirectionStatus.Unknown)
                ConsoleUI.ErrorExit("!Unknown scan type", 2);
            if (theData.MetaData.ScanStatus == ScanDirectionStatus.NoData)
                ConsoleUI.ErrorExit("!No scan data present", 3);

            // now we can start to sort and format everything we need

            prf.CreationDate = theData.MetaData.CreationDate;
            prf.SampleIdentification = theData.MetaData.SampleIdentifier;
            prf.DeltaX = theData.MetaData.ScanFieldDeltaX * 1e6;
            prf.UserComment = options.UserComment;

            // extract the requested profile
            if (options.ProfileIndex != 0)
            {
                ProcessSingleProfile(options.ProfileIndex);
                return;
            }
            // (ProfileIndex == 0) => extract all profiles
            for (int i = 1; i <= theData.MetaData.NumberOfProfiles; i++)
            {
                ProcessSingleProfile(i);
            }

        }

        //======================================================================

        static void ProcessSingleProfile(int selectedProfile)
        {
            // read actual topography data for given channel
            if (!theData.ColumnPresent(options.ChannelSymbol))
                ConsoleUI.ErrorExit($"!Channel {options.ChannelSymbol} not in scan data", 5);
            double[] rawData = theData.ExtractProfile(options.ChannelSymbol, selectedProfile, topographyProcessType);

            // level data 
            DataLeveling levelObject = new DataLeveling(rawData, theData.MetaData.NumberOfDataPoints);
            levelObject.BiasValue = options.Bias * 1.0e-6; //  bias is given in µm on the command line
            double[] leveledTopographyData = levelObject.LevelData(MapOptionToReference(options.ReferenceMode));

            prf.SetProfileData(leveledTopographyData);
            prf.TipConvolution(options.TipRadius);
            prf.ShortenProfile(options.Xstart, options.Xlength);

            // now generate output
            string outFileName;
            if (fileNames.Length >= 2)
                outFileName = fileNames[1];
            else
            {
                outFileName = nmmFileNameObject.GetFreeFileNameWithIndex(""); // extension will be added by WriteToFile()
                outFileName = Path.ChangeExtension(outFileName, null);
                outFileName += $"_p{selectedProfile}";
            }

            // write all selected file formats at a flush
            WriteAllOutputFiles(outFileName);

        }

        //======================================================================

        static void WriteAllOutputFiles(string filename)
        {
            if (options.convertBcr)
            {
                WriteSingleOutputFile(filename, FileFormat.Bcr);
            }
            if (options.convertPrDe)
            {
                WriteSingleOutputFile(filename, FileFormat.PrDE);
            }
            if (options.convertPrEn)
            {
                WriteSingleOutputFile(filename, FileFormat.PrEN);
            }
            if (options.convertPrf)
            {
                WriteSingleOutputFile(filename, FileFormat.Prf);
            }
            if (options.convertSig)
            {
                WriteSingleOutputFile(filename, FileFormat.SigmaSurf);
            }
            if (options.convertSmd)
            {
                WriteSingleOutputFile(filename, FileFormat.Smd);
            }
            if (options.convertTxt)
            {
                WriteSingleOutputFile(filename, FileFormat.Txt);
            }
            if (options.convertCsv)
            {
                WriteSingleOutputFile(filename, FileFormat.Csv);
            }
            if (options.convertX3p)
            {
                WriteSingleOutputFile(filename, FileFormat.X3p);
            }
        }

        //======================================================================

        static void WriteSingleOutputFile(string filename, FileFormat fileFormat)
        {
            ConsoleUI.WriteLine(FileFormatToString(fileFormat));
            ConsoleUI.WritingFile(filename);
            if (!prf.WriteToFile(filename, fileFormat))
            {
                ConsoleUI.Abort();
                ConsoleUI.ErrorExit("!could not write file", 4);
            }
            ConsoleUI.Done();
            ConsoleUI.WriteLine();
        }

        //======================================================================

        static string FileFormatToString(FileFormat fileFormat)
        {
            switch (fileFormat)
            {
                case FileFormat.PrDE:
                    return $"Output PR format as defined by PTB with German key words. [*{prf.ExtensionFor(fileFormat)}]";
                case FileFormat.PrEN:
                    return $"Output PR format as defined by PTB with English key words. [*{prf.ExtensionFor(fileFormat)}]";
                case FileFormat.Prf:
                    return $"Output PRF format as defined by NPL. [*{prf.ExtensionFor(fileFormat)}]";
                case FileFormat.Bcr:
                    return $"Output SMD format as of ISO 25178-71 and EUNA 15178. [*{prf.ExtensionFor(fileFormat)}]";
                case FileFormat.SigmaSurf:
                    return $"Output format as used by SigmaSurf freeware. [*{prf.ExtensionFor(fileFormat)}]";
                case FileFormat.Smd:
                    return $"Output SMD format as of ISO 5436-2. [*{prf.ExtensionFor(fileFormat)}]";
                case FileFormat.Txt:
                    return $"Output as basic text file as defined by NPL. [*{prf.ExtensionFor(fileFormat)}]";
                case FileFormat.Csv:
                    return $"Output as basic CSV file. [*{prf.ExtensionFor(fileFormat)}]";
                case FileFormat.X3p:
                    return $"Output format to XML with schema as of ISO 25178-72. You may also use Nmm2x3p instead. [*{prf.ExtensionFor(fileFormat)}]";
                default:
                    return "Requested output format unknown.";
            }
        }

        //======================================================================

        // maps the numerical option to the appropriate enumeration
        static ReferenceTo MapOptionToReference(int reference)
        {
            switch (reference)
            {
                case 1:
                    return ReferenceTo.Minimum;
                case 2:
                    return ReferenceTo.Maximum;
                case 3:
                    return ReferenceTo.Average;
                case 4:
                    return ReferenceTo.Central;
                case 5:
                    return ReferenceTo.Bias;
                case 6:
                    return ReferenceTo.First;
                case 7:
                    return ReferenceTo.Last;
                case 8:
                    return ReferenceTo.Center;
                case 9:
                    return ReferenceTo.Line;
                case 10:
                    return ReferenceTo.Lsq;
                case 11:
                    return ReferenceTo.LinePositive;
                case 12:
                    return ReferenceTo.LsqPositive;
                default:
                    return ReferenceTo.None;
            }
        }

    }
}
