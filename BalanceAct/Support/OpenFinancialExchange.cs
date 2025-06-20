using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using BalanceAct.Models;

namespace BalanceAct.Support
{
    /// <summary>
    /// Holds the data parsed from a QFX file.
    /// The Header property contains key–value pairs from the file header,
    /// and the OfxDocument contains the XML representation of the OFX section.
    /// </summary>
    public class QfxData
    {
        public Dictionary<string, string> Header { get; set; }
        public XDocument OfxDocument { get; set; }
    }

    /// <summary>
    /// Provides a static method to parse a Quicken Web Connect (QFX) file.
    /// </summary>
    /// <remarks>
    /// This class also supports the OFX standard (OFX Banking Specification).
    /// </remarks>
    public static class QfxParser
    {
        /// <summary>
        /// Parses a QFX file from the file system.
        /// </summary>
        /// <param name="filePath">The full path to the QFX file.</param>
        /// <returns>A QfxData object containing a header dictionary and an XDocument for the OFX section.</returns>
        public static QfxData ParseFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("The specified QFX file was not found.", filePath);

            StringComparer strComp = StringComparer.OrdinalIgnoreCase;

            // Read all lines from the file.
            var allLines = File.ReadAllLines(filePath).ToList();

            // Split the header from the OFX content.
            // The header is assumed to be all lines from the start until the first blank line.
            List<string> headerLines = new List<string>();
            int contentStartIndex = 0;
            for (int i = 0; i < allLines.Count; i++)
            {
                // Content begins once <OFX> tag is detected.
                if (strComp.Equals(allLines[i], "<OFX>")) { contentStartIndex = i; break; }
                headerLines.Add(allLines[i]);
            }
            /*
                Pulled from the "OFX Banking Specification v2.3.pdf"

                OFXHEADER:100    (specifies the version number of the Open Financial Exchange declaration)
                DATA:OFXSGML     (data type: Open Financial Exchange Standard Generalized Markup Language)
                VERSION:102      (specifies the version number of the following OFX data block)
                SECURITY:NONE    (defines the type of application-level security, if any, that is used for the <OFX> block)
                ENCODING:USASCII (file encoding type)
                CHARSET:1252     (code page indicator)
                COMPRESSION:NONE (if compression techniques are used on the data)
                OLDFILEUID:NONE  (is used together with NEWFILEUID only when the client and server support file-based error recovery)
                NEWFILEUID:NONE  (uniquely identifies this request file)

                ┌ Top Level <OFX>
                ├── Message Set and Version <xxxMSGSVn>
                ├───── Synchronization Wrappers <xxxSYNCRQ>, <xxxSYNCRS>
                ├──────── Transaction Wrappers <xxxTRNRQ>, <xxxTRNRS>
                └─────────── Specific requests and responses
             */

            // The rest of the file (after the blank line) is the OFX portion.
            var content = allLines.Skip(contentStartIndex).ToList();
            var contentLines = content.Select(line => line.Trim()).Where(line => !line.Contains("<INTU.BID>")).ToList();

            // Parse the header lines into a dictionary.
            Dictionary<string, string> headerDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string line in headerLines)
            {
                // Expecting header lines in the format "Key:Value".
                var parts = line.Split(new char[] { ':' }, 2);
                if (parts.Length == 2)
                {
                    headerDict[parts[0].Trim()] = parts[1].Trim();
                }
            }

            // Prepare to massage the OFX content lines into well-formed XML.
            List<string> processedContentLines = new List<string>();

            // Regex to capture an opening tag, any inline content, and optional trailing whitespace.
            // This regex matches lines like: "<TAG>value" but skips those that already include a closing tag.
            Regex regex = new Regex(@"^(?<indent>\s*)<(?<tag>\w+)>(?<value>.*)$");

            foreach (string line in contentLines)
            {
                // If the line already contains a closing tag (or is empty), assume it’s formatted correctly.
                if (string.IsNullOrWhiteSpace(line) || line.Contains("</"))
                {
                    processedContentLines.Add(line);
                    continue;
                }

                // Match lines of the format: <Tag>SomeValue
                Match match = regex.Match(line);
                if (match.Success)
                {
                    string indent = match.Groups["indent"].Value;
                    string tag = match.Groups["tag"].Value;
                    string value = match.Groups["value"].Value.Trim();

                    // If the value is non-empty, wrap it with a closing tag.
                    if (!string.IsNullOrEmpty(value))
                    {
                        string newLine = $"{indent}<{tag}>{value}</{tag}>";
                        processedContentLines.Add(newLine);
                    }
                    else
                    {
                        // Otherwise, output the line as-is (it might be the start of a container element).
                        processedContentLines.Add(line);
                    }
                }
                else
                {
                    processedContentLines.Add(line);
                }
            }

            // Join the processed lines back into a single string.
            string xmlContent = string.Join(Environment.NewLine, processedContentLines);
            //string xmlContent = string.Join("", processedContentLines);

            // Load the OFX portion into an XDocument, if we're going to fail it'll be here.
            // We assume that the OFX content provided in the file has a root element (typically <OFX>).
            XDocument ofxDoc = XDocument.Parse(xmlContent);

            return new QfxData
            {
                Header = headerDict,
                OfxDocument = ofxDoc
            };
        }

        /// <summary>
        /// Parses an OFX date string (e.g., "20250609120000[0:GMT]") into a DateTime object.
        /// This method extracts the first 14 digits representing "yyyyMMddHHmmss" and ignores any trailing timezone info.
        /// </summary>
        /// <param name="ofxDateString">The OFX date string (e.g., "20250609120000[0:GMT]").</param>
        /// <returns>A DateTime object corresponding to the parsed date and time.</returns>
        /// <exception cref="FormatException">Thrown if the date portion cannot be parsed.</exception>
        public static DateTime? ParseOfxDate(string ofxDateString)
        {
            if (string.IsNullOrEmpty(ofxDateString))
                return null;

            // Find the start of the timezone specifier (if present) and remove it.
            int bracketIndex = ofxDateString.IndexOf('[');
            string dateTimePart = bracketIndex >= 0
                ? ofxDateString.Substring(0, bracketIndex)
                : ofxDateString;

            // We expect the dateTimePart to be in "yyyyMMddHHmmss" format.
            const string format = "yyyyMMddHHmmss";
            if (DateTime.TryParseExact(dateTimePart, format, CultureInfo.InvariantCulture,
                                       DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                                       out DateTime parsedDate))
            {
                return parsedDate;
            }
            //else
            //{
            //    throw new FormatException($"The date string '{ofxDateString}' was not in the expected format.");
            //}
            
            return null;
        }
    }

    /// <summary>
    /// Usage example
    /// </summary>
    public class QFXTest
    {
        static StringComparer strComp = StringComparer.OrdinalIgnoreCase;

        public static void Run(string filePath = @"E:\Sample.qfx")
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.WriteLine("[WARNING] QFX/OFX file not found. Please ensure the file is available.");
                    return;
                }
                QfxData qfxData = QfxParser.ParseFile(filePath);

                Debug.WriteLine("QFX Header Properties:");
                Debug.WriteLine(new string('=', 80));
                foreach (var kv in qfxData.Header)
                {
                    Debug.WriteLine($"  {kv.Key}: {kv.Value}");
                }
                Debug.WriteLine(new string('=', 80));
                Debug.WriteLine("QFX Document Root: " + qfxData.OfxDocument.Root.Name);
                Debug.WriteLine($"QFX Document Content:\r\n{qfxData.OfxDocument.Beautify()}");
                /** 
                 **   Further processing of the OFX XML done here 
                 **/
                var xml = qfxData.OfxDocument.ToString(SaveOptions.DisableFormatting);
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(OFX));
                using (StringReader reader = new StringReader(xml))
                {
                    var tranList = (OFX)serializer.Deserialize(reader);
                    foreach (var t in tranList.CREDITCARDMSGSRSV1.CCSTMTTRNRS.CCSTMTRS.BANKTRANLIST.STMTTRN)
                    {
                        /*
                        <STMTTRN>
                            <TRNTYPE>DEBIT</TRNTYPE>
                            <DTPOSTED>20250617120000[0:GMT]</DTPOSTED>
                            <TRNAMT>-42.54</TRNAMT>
                            <FITID>2025061700000000000000000000000</FITID>
                            <NAME>STORE 6074</NAME>
                        </STMTTRN>
                        */

                        var dt = ParseOfxDate(t.DTPOSTED).ToLongDateString();

                        if (strComp.Equals(t.TRNTYPE, "DEBIT"))
                        {
                            Debug.WriteLine($"Money removed: {t.TRNTYPE} ⇨ ${t.TRNAMT} ⇨ {t.NAME} ⇨ {dt} ⇨ {t.FITID}");
                        }
                        else if (strComp.Equals(t.TRNTYPE, "CREDIT"))
                        {
                            Debug.WriteLine($"Money added: {t.TRNTYPE} ⇨ ${t.TRNAMT} ⇨ {t.NAME} ⇨ {dt} ⇨ {t.FITID}");
                        }
                        else
                        {
                            Debug.WriteLine($"Undefined transaction type '{t.TRNTYPE}'");
                        }
                    }
                    Debug.WriteLine($"Available balance: ${tranList.CREDITCARDMSGSRSV1.CCSTMTTRNRS.CCSTMTRS.AVAILBAL.BALAMT}");
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error parsing QFX file: " + ex.Message);
            }
        }

        /// <summary>
        /// Parses an OFX date string (e.g., "20250609120000[0:GMT]") into a DateTime object.
        /// This method extracts the first 14 digits representing "yyyyMMddHHmmss" and ignores any trailing timezone info.
        /// </summary>
        /// <param name="ofxDateString">The OFX date string (e.g., "20250609120000[0:GMT]").</param>
        /// <returns>A DateTime object corresponding to the parsed date and time.</returns>
        /// <exception cref="FormatException">Thrown if the date portion cannot be parsed.</exception>
        public static DateTime ParseOfxDate(string ofxDateString)
        {
            if (string.IsNullOrEmpty(ofxDateString))
                return DateTime.MinValue;

            // Find the start of the timezone specifier (if present) and remove it.
            int bracketIndex = ofxDateString.IndexOf('[');
            string dateTimePart = bracketIndex >= 0
                ? ofxDateString.Substring(0, bracketIndex)
                : ofxDateString;

            // We expect the dateTimePart to be in "yyyyMMddHHmmss" format.
            const string format = "yyyyMMddHHmmss";
            if (DateTime.TryParseExact(dateTimePart, format, CultureInfo.InvariantCulture,
                                       DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                                       out DateTime parsedDate))
            {
                return parsedDate;
            }
            else
            {
                throw new FormatException($"The date string '{ofxDateString}' was not in the expected format.");
            }
        }

    }
}
