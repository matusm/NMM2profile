//*******************************************************************************************
//
// Class for writing GPS data files (profiles) according to various standards. 
//
// Usage:
//   1) instantiate class;
//   2) provide required properties;
//   3) provide profile data by calling SetProfileData(double[]);
//   4) finally produce the output file by calling WriteToFile(string, FileFormat).
//
// Caveat:
//   SetProfileData(double[]) multiplies the z-data with 1e6 (assuming data is in m)
//
// Known problems and restrictions:
//   most properties must be set in advance, otherwise no output will be generated
//
//*******************************************************************************************


using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace Nmm2Profile
{
    public class ProfileDataPod
    {

        #region Ctor
        public ProfileDataPod()
        {
            ResetData();
        }
        #endregion

        #region Properties
        public DateTime CreationDate { get; set; }
        public string FileName { get; set; }
        public string SampleIdentification { get; set; }
        public double DeltaX { get; set; } // in µm !
        #endregion

        #region Methods
        // provide the height values in m !
        public void SetProfileData(double[] zRawData)
        {
            zData = new double[zRawData.Length];
            zData = Array.ConvertAll(zRawData, z => z * 1.0E6);
        }

        public string DataToString(FileFormat fileFormat)
        {
            // all file types are explicitly designed for MSDOS
            // for this the newline is hard coded as "CR LF"
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            StringBuilder sb = new StringBuilder();
            switch (fileFormat)
            {
                case FileFormat.Txt:
                    // **********************************************************
                    // Very simple format, no place for comments etc.
                    // http://resource.npl.co.uk/softgauges/Help.htm
                    // **********************************************************
                    sb.Append($"{DeltaX:F3}\r\n");
                    foreach (double z in zData)
                        sb.Append($"{z:F6}\r\n");
                    break;
                case FileFormat.SigmaSurf:
                    // **********************************************************
                    // Format used by the freeware SigmaSurf
                    // http://www.digitalmetrology.com
                    // **********************************************************
                    sb.Append("SigmaSurf_Data 1.0\r\n");
                    sb.Append($"IDENTIFICATION: {SampleIdentification}\r\n");
                    sb.Append($"DATE: {CreationDate.ToString("dd MMM yyyy - HH:mm")}\r\n");
                    sb.Append($"SPACING_UM: {DeltaX.ToString("F5")}\r\n");
                    sb.Append($"NUMBER_OF_POINTS: {zData.Length}\r\n");
                    sb.Append($"PROFILE_UM:\r\n");
                    foreach (double z in zData)
                        sb.Append($"{z:F6}\r\n"); // resolution 1 pm
                    break;
                case FileFormat.Prf:
                    // **********************************************************
                    // not well documented, implementation taken
                    // from the following NPL site:
                    // http://resource.npl.co.uk/softgauges/Help.htm
                    // **********************************************************
                    sb.Append("1 2\r\n");
                    sb.Append("SG2004 0.000000e+000 PRF\r\n");
                    sb.Append(string.Format("CX M {0:e} MM 1.000000e+000 D\r\n", (double)zData.Length));
                    sb.Append(string.Format("CZ M {0:e} MM 1.000000e-009 L\r\n", (double)zData.Length));
                    sb.Append("EOR\r\n");
                    sb.Append("STYLUS_RADIUS 0.000000e+000 MM\r\n");
                    sb.Append(string.Format("SPACING CX {0:e}\r\n", DeltaX * 1000.0)); // in mm!
                    sb.Append("MAP 1.000000e+000 CZ CZ 1.000000e+000 1.000000e+000\r\n");
                    sb.Append("MAP 2.000000e+000 CZ CX 1.000000e+000 0.000000e+000\r\n");
                    sb.Append($"COMMENT {SampleIdentification}\r\n");
                    sb.Append("EOR\r\n");
                    foreach (double z in zData)
                        sb.Append($"{z * 1e6:F0}\r\n"); // resolution 1 pm, as in example
                    sb.Append("EOR\r\n");
                    sb.Append("EOF\r\n");
                    break;
                case FileFormat.PrDE:
                case FileFormat.PrEN:
                    // **********************************************************
                    // This format was propably developed by PTB and documented
                    // on the following web site:
                    // http://www.ptb.de/en/org/5/51/517/rptb_web/wizard/greeting.php
                    // Umlaute are part of the definition!
                    // It seems to originate from MSDOS times.
                    // update in format definition to avoid Umlaute as described in:
                    // https://ptb.de/rptb
                    // **********************************************************
                    double xLen = DeltaX * (zData.Length - 1);
                    double xResolution = 1000.0 / DeltaX;
                    sb.Append($"Profil {FileName}\r\n");
                    if (fileFormat == FileFormat.PrDE)
                    {
                        sb.Append($"X-Maß = {xLen:F8} X-Auflösung {xResolution:F6} Punkte/Zeile : {zData.Length}\r\n");
                    }
                    if (fileFormat == FileFormat.PrEN)
                    {
                        sb.Append($"X-len = {xLen:F8} X-resolution {xResolution:F6} points/scanline: {zData.Length}\r\n");
                    }
                    foreach (double z in zData)
                        sb.Append($"{z:F6}\r\n"); // resolution 1 pm, as in example
                    // from the format definition:
                    // "The last of all z-values shall be in the last line and not followed by a line feed."
                    sb.Length--;
                    sb.Length--;
                    break;
                case FileFormat.Smd:
                    // TODO implement!
                    break;
                case FileFormat.Sdf:
                    // TODO implement!
                    break;
                case FileFormat.Unknown:
                case FileFormat.x3p:
                    // will not be implemented!
                    break;
                default:
                    break;
            }
            return sb.ToString();
        }

        public bool WriteToFile(string fileName, FileFormat fileFormat)
        {
            // check if data present
            if (string.IsNullOrWhiteSpace(DataToString(fileFormat)))
                return false;
            // change file name extension
            fileName = Path.ChangeExtension(fileName, ExtensionFor(fileFormat));
            FileName = fileName;
            // write the file
            try
            {
                StreamWriter hOutFile = File.CreateText(fileName);
                hOutFile.Write(DataToString(fileFormat));
                hOutFile.Close();
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public bool IsFormatImplemented(FileFormat fileFormat)
        {
            switch (fileFormat)
            {
                case FileFormat.Unknown:
                    return false;
                case FileFormat.SigmaSurf:
                    return true;
                case FileFormat.Prf:
                    return true;
                case FileFormat.PrDE:
                    return true;
                case FileFormat.PrEN:
                    return true;
                case FileFormat.Txt:
                    return true;
                case FileFormat.Sdf:
                    return false;
                case FileFormat.Smd:
                    return false;
                case FileFormat.x3p:
                    return false;
                default:
                    return false;
            }
        }
        
        public string ExtensionFor(FileFormat fileFormat)
        {
            switch (fileFormat)
            {
                case FileFormat.Unknown:
                    return ".???";
                case FileFormat.SigmaSurf:
                    return ".sig";
                case FileFormat.Prf:
                    return ".prf";
                case FileFormat.PrDE:
                    return ".pr";
                case FileFormat.PrEN:
                    return ".pr";
                case FileFormat.Txt:
                    return ".txt";
                case FileFormat.Sdf:
                    return ".sdf";
                case FileFormat.Smd:
                    return ".smd";
                case FileFormat.x3p:
                    return ".x3p";
                default:
                    return ".???";
            }
        }
        #endregion

        #region Private stuff
        private void ResetData()
        {
            CreationDate = DateTime.UtcNow;
            SampleIdentification = "<unknown sample>";
            FileName = "<unknown file name>";
            DeltaX = 0.0; // or NaN?
        }

        // this is the profile in µm
        private double[] zData;
        #endregion
    }

    public enum FileFormat
    {
        Unknown,
        SigmaSurf,  // format used by the freeware SigmaSurf
        Prf,    // NPL format
        PrDE,   // PTB format, old
        PrEN,   // PTB format, new
        Txt,    // NPL format, basic
        Sdf,    // ISO 25178-71:2012 and EUNA 15178 ENC (1993)
        Smd,    // ISO 5436-2:2012
        x3p     // XML with schema ISO 25178-72
    }
}
