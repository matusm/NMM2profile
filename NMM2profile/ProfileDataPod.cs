//*******************************************************************************************
//
// Class for writing GPS data files (profiles) according to various standards. 
//
// Usage:
//   1) instantiate class;
//   2) provide required properties;
//   3) provide profile data by calling SetProfileData(double[]);
//   4) eventually trim profile by calling ShortenProfile(double, double)
//   5) finally produce the output file by calling WriteToFile(string, FileFormat).
//
// Caveat:
//   SetProfileData(double[]) multiplies the z-data with 1e6 (assuming data is in m)
//
// Known problems and restrictions:
//   most properties must be set in advance, otherwise no output will be generated
//   ShortenProfile() modifies zData. It should be called at most once.
//
//*******************************************************************************************

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Nmm2Profile
{
    public class ProfileDataPod
    {

        public ProfileDataPod()
        {
            ResetData();
        }

        public DateTime CreationDate { get; set; }
        public string FileName { get; set; }
        public string SampleIdentification { get; set; }
        public double DeltaX { get; set; } // in µm !
        public string UserComment { get; set; }
        public double Start { get; private set; }
        public double Length { get; private set; }
        public string TipConvolutionMessage { get; private set; }

        // provide the height values in m !
        public void SetProfileData(double[] zRawData)
        {
            zData = new double[zRawData.Length];
            zData = Array.ConvertAll(zRawData, z => z * 1.0E6);
        }

        public void ShortenProfile(double start, double length)
        {
            Start = start;
            Length = length;
            if (zData == null) return;
            List<double> zTemp = new List<double>();
            double x = 0.0;
            for (int i = 0; i < zData.Length; i++)
            {
                if (x >= start && x <= start + length)
                {
                    zTemp.Add(zData[i]);
                }
                x += DeltaX;
            }
            zData = zTemp.ToArray();
        }

        public void TipConvolution(double tipRadius)
        {
            TipConvolutionMessage = "no tip convolution performed";
            if (tipRadius <= 0.0) return;
            int n = (int)(tipRadius / DeltaX);
            if (n < 1) return;
            double[] tipProfile = new double[n + 1];
            for (int i = 0; i < tipProfile.Length; i++)
            {
                double x = (i-n) * DeltaX;
                tipProfile[i] = Math.Sqrt(tipRadius * tipRadius - x * x);
            }

            double[] zDataTemp = new double[zData.Length];
            
            for (int i = 0; i < zData.Length; i++)
            {
                List<double> convoluted = new List<double>();
                for (int j = -n; j <= n; j++)
                {
                    if ((i + j) < 0) break;
                    if ((i + j) >= zData.Length) break;

                    double y = tipProfile[Math.Abs(j)] + zData[i + j];

                    convoluted.Add(y);
                }
                zDataTemp[i] = convoluted.Max();
            }
            TipConvolutionMessage = $"spherical tip radius {tipRadius} µm";
            Array.Copy(zDataTemp, zData, zDataTemp.Length);
        }

        public string DataToString(FileFormat fileFormat)
        {
            // all file types are explicitly designed for MSDOS
            // for this the newline is hard coded as "CR LF"
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
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
                    // **********************************************************
                    // Standardized format used by the surface texture community
                    // uses some non printeable characters
                    // This format was designed for F1 softgauges (only?)
                    // ISO 5436-2:2012
                    // **********************************************************
                    string endOfRecord = $"{(char)3}\r\n";
                    sb.Append($"ISO 5436-2:2012\0{FileName}\0\r\n");
                    sb.Append("PRF\0 1 ISO5436\0\r\n");
                    sb.Append($"CX\0 I\0 {zData.Length} mm\0 1.0e0 D\0 {DeltaX / 1000:e5} \r\n");
                    sb.Append($"CZ\0 A\0 {zData.Length} um\0 1.0e0 D\0\r\n");
                    sb.Append(endOfRecord); // end of record 1
                    sb.Append($"DATE: {CreationDate.ToString("dd-MMMM-yyyy")}\0\r\n");
                    sb.Append($"TIME: {CreationDate.ToString("HH:mm")}\0\r\n");
                    sb.Append("CREATED_BY Michael Matus, BEV\0\r\n"); //TODO
                    sb.Append($"COMMENT /* {UserComment} */\0\r\n");
                    sb.Append(endOfRecord); // end of record 2
                    foreach (double z in zData)
                        sb.Append($"{z:F5}\r\n");
                    sb.Append(endOfRecord); // end of record 3
                    // checksum calculation
                    ushort chkSum = 0;
                    string s = sb.ToString();
                    byte[] buffer = System.Text.Encoding.ASCII.GetBytes(s);
                    foreach (byte byt in buffer)
                        chkSum += (ushort)byt;
                    sb.Append($"{chkSum.ToString()}\r\n");
                    sb.Append(endOfRecord); // end of record 4
                    sb.Append($"{(char)26}"); // end of file
                    break;
                case FileFormat.Bcr:
                    // **********************************************************
                    // GPS data files according to 
                    // ISO 25178-7, ISO 25178-71 and EUNA 15178.
                    // **********************************************************
                    sb.AppendLine("aBCR - 1.0");
                    sb.AppendLine($"ManufacID   = BEV / SIOS NMM");
                    sb.AppendLine($"CreateDate  = {CreationDate.ToString("ddMMyyyyHHmm")}");
                    sb.AppendLine($"ModDate     = {DateTime.UtcNow.ToString("ddMMyyyyHHmm")}");
                    sb.AppendLine($"NumPoints   = {zData.Length}");
                    sb.AppendLine("NumProfiles = 1");
                    sb.AppendLine($"Xscale      = {(DeltaX * 1e-6).ToString("G17")}");
                    sb.AppendLine($"Yscale      = 0");
                    sb.AppendLine($"Zscale      = 1.0e-6");
                    sb.AppendLine("Zresolution = -1"); // clause 5.2.8, do not modify!
                    sb.AppendLine("Compression = 0"); // clause 5.2.9, do not modify!
                    sb.AppendLine("DataType    = 7");
                    sb.AppendLine("CheckType   = 0"); // clause 5.2.11, do not modify!
                    sb.AppendLine("*");
                    foreach (double z in zData)
                        sb.AppendLine($"{z:G17}"); // round-trip format
                    sb.AppendLine("*");
                    string AppName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
                    string AppVer = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                    sb.AppendLine($"ConvertedBy          = {AppName} version {AppVer}");
                    sb.AppendLine($"SampleIdentification = {SampleIdentification}");
                    sb.AppendLine($"FileName             = {FileName}"); // this is useless
                    sb.AppendLine($"UserComment          = {UserComment}");
                    sb.AppendLine($"TrimmedStart         = {Start} µm");
                    sb.AppendLine($"TrimmedLength        = {Length} µm");
                    sb.AppendLine("*");
                    break;
                case FileFormat.Csv:
                    double x = 0.0;
                    sb.AppendLine("x in µm,z in µm");
                    foreach (double z in zData)
                    {
                        sb.AppendLine($"{x:G17},{z:G17}");
                        x += DeltaX;
                    }
                    break;
                case FileFormat.Unknown:
                case FileFormat.X3p:
                    // will not be implemented!
                    throw new NotImplementedException();
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
                case FileFormat.Bcr:
                    return ".sdf";
                case FileFormat.Smd:
                    return ".smd";
                case FileFormat.X3p:
                    return ".x3p";
                case FileFormat.Csv:
                    return ".csv";
                default:
                    return ".???";
            }
        }

        private void ResetData()
        {
            CreationDate = DateTime.UtcNow;
            SampleIdentification = "<unknown sample>";
            FileName = "<unknown file name>";
            DeltaX = 0.0; // or NaN?
            UserComment = "<unknown user comment>";
            Start = 0.0;
            Length = 0.0;
        }

        // this is the profile in µm
        private double[] zData;
    }

    public enum FileFormat
    {
        Unknown,
        SigmaSurf,  // format used by the freeware SigmaSurf
        Prf,    // NPL format
        PrDE,   // PTB format, old
        PrEN,   // PTB format, new
        Txt,    // NPL format, basic
        Bcr,    // ISO 25178-71:2012 and EUNA 15178 ENC (1993)
        Smd,    // ISO 5436-2:2012
        X3p,    // XML with schema ISO 25178-72
        Csv     // good old CSV
    }
}
