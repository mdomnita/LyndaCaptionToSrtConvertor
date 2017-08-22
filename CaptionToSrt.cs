using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LyndaCaptionToSrtConvertor
{
    class CaptionToSrt
    {
        private string filePath;
        private string outFile;

        public CaptionToSrt(string afilePath)
        {
            this.filePath = afilePath;
        }

        public string FilePath
        {
            get
            {
                return filePath;
            }

            set
            {
                filePath = value;
            }
        }

        public string OutFile
        {
            get
            {
                return outFile;
            }

            set
            {
                outFile = value;
            }
        }

        public string PrepareSrt()
        {
            const int METADATA_LINES = 7, CHARS_BEFORE_TIMESTAMP = 13, CHARS_AFTER_TIMESTAMP = 14;
            //read all file in memory
            string content = File.ReadAllText(filePath);

            // Discard the first lines, containing metadata used by Lynda desktop app to link subtitle to video:
            string output = RemoveFirstLines(content, METADATA_LINES);

            // Before every timestamp we have a constant amount of characters (starting by [NUL][SOH] and ending with \n)
            output = Regex.Replace(output, @"\u0000\u0001[\s\S]{" + CHARS_BEFORE_TIMESTAMP + "}[\r\n]*", "");
            
            // After every timestamp we also have a constant amount of characters:
            output = Regex.Replace(output, @"(?<=\[\d\d:\d\d:\d\d\.\d\d\])[\s\S]{" + CHARS_AFTER_TIMESTAMP + "}", "");

            // Cleanup remaining non-UTF8 ASCII chars:            
            output = Regex.Replace(output, @"[^\u0020-\u007F \u000D\n\r]+", "");

            return output;
        }

        public bool PublishSrt(string output)
        {
            //split full formatted text in subtitle sections at start of timestamp
            string[] phrases = Regex.Split(output, @"(?=\[[0-9])");

            string start;
            string text;
            string[] subline;
            ArrayList timestamps = new ArrayList();
            ArrayList captions = new ArrayList();
            for (int i = 0; i < phrases.Length; i++)
            {
                try
                {
                    //get timestamp and text separately
                    subline = Regex.Split(phrases[i], @"(?<=\[[0-9:,.]+\])");
                    if (subline.Length == 2)
                    {
                        //separator for miliseconds is ',' in srt, '.' in .caption so switch it
                        start = Regex.Replace(subline[0], "\\.", ",");
                        start = start.Substring(1, start.Length - 2);
                        text = subline[1];
                        // delete any useless newlines at the end.
                        text = Regex.Replace(text, @"[\r\n\t]+$", "");
                        timestamps.Add(start);
                        captions.Add(text);
                    }

                    this.BuildSrt(timestamps, captions, this.outFile);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: Cannot convert caption content to srt " + ex.ToString());
                    return false;
                }
            }
            return true;
        }

        private static string RemoveFirstLines(string text, int linesCount)
        {
            // Source: https://stackoverflow.com/a/15925157/
            var lines = Regex.Split(text, "\r\n|\r|\n").Skip(linesCount);
            return string.Join(Environment.NewLine, lines.ToArray());
        }

        private bool BuildSrt(ArrayList timestamps, ArrayList captions, string path)
        {
            try
            {
                StreamWriter writer = new StreamWriter(path);
                //SRT is perhaps the most basic of all subtitle formats.
                //It consists of four parts, all in text..

                //1.A number indicating which subtitle it is in the sequence.
                //2.The time that the subtitle should appear on the screen, and then disappear.
                //3.The subtitle itself.
                //4.A blank line indicating the start of a new subtitle.

                //1
                //00:02:17,440-- > 00:02:20,375
                //and here goes the text, after which there's a blank line

                //last iinput in array is a single timestamp with no text, used only to see where the end of the last caption is
                for (int i = 0; i < timestamps.Count - 1; i++)
                {
                    writer.WriteLine(i + 1);
                    writer.WriteLine(timestamps[i] + " --> " + timestamps[i + 1]);
                    writer.WriteLine(captions[i]);
                    writer.WriteLine();
                }
                writer.Close();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: Cannot write file " + path + ex.ToString());
                return false;
            }
        }
    }
}
