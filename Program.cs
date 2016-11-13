using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Collections;

namespace LyndaCaptionToSrtConvertor
{
    class Program
    {
        static void Main(string[] args)
        {
            string folderPath = "";
            int index = 0;
            foreach (string arg in args)
            {
                if (string.IsNullOrWhiteSpace(arg))
                {
                    index++;
                    continue;
                }

                switch (arg.ToUpper())
                {
                    case "/D": // Directory 
                        folderPath = args[index + 1];
                        Console.WriteLine("Directory with captions: " + folderPath);
                        break;
                }
            }
            //directory with srt files, will get from console in real use case
            if (String.IsNullOrEmpty(folderPath))
            {
                folderPath = "..\\tests";
                Console.WriteLine("defaulting to ..\\tests directory. Please input full subtitle folder path in the /D parameter.");
            }
            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine("Directory not found. Press any key to exit");
                // Console app
                Console.ReadKey();
                System.Environment.Exit(1);
            }

            foreach (string entry in Directory.EnumerateFiles(folderPath, "*.caption", SearchOption.AllDirectories))
            {
                string filePath = entry;
                if (!File.Exists(filePath))
                    throw new FileNotFoundException();
                //rea all file in memory
                string content = File.ReadAllText(filePath);

                //crude replacement of characters that are not plain text. File has NUL, SOX, ACK and other non printing ASCII / binary chars
                //Observed a pattern in file, rows in caption are ordered/marked by 
                // a structure of characters of the form [ACK]<0-9A-Z>[NUL] remove these first
                // a structure of characters of the form [ACK]<0-9A-Z>[SOH] remove these first
                string output = Regex.Replace(content, @"\u0006[\u0020-\u007F][\u0000\u0001\u0002]", "");

                //there are some chars right after the timestamp ] and before [NUL] or [SOH] or [ETB] chars, drop them 
                output = Regex.Replace(output, @"\][^\]\u0000-\u001F]+[\u0000-\u001F]", "]");

                //delete all non-UTF8 ASCII printable chars by this regexp
                output = Regex.Replace(output, @"[^\u0020-\u007F \u000D\n\r]+", "");

                //now we might be left with a newline or useless white space after the timestamp and before the text, delete that too
                output = Regex.Replace(output, @"\][ \n\r\t]", "");

                //remove all info at start of file used by Lynda desktop app to link subtitle to video
                //presume first timestamp starts with '[00:00...' so this is where the actual subtitle text starts
                if (output.IndexOf("[0") > 0)
                {
                    output = output.Substring(output.IndexOf("[0"));
                }

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
                            //separator for miliseconds is ',' in srt, '.' in .caption switch it
                            start = Regex.Replace(subline[0], "\\.", ",");
                            start = start.Substring(1, start.Length - 2);
                            text = subline[1];
                            //there may be a number or sign before the actual text, drop it.
                            //ATTENTION, if there is a subtitle phrase starting eth numbers or symbols this line will delete information from it
                            text = Regex.Replace(text, @"^[\u0020-\u0040]+", "");
                            //and delete any useless newlines at the end.
                            text = Regex.Replace(text, @"[\r\n\t]+$", "");
                            timestamps.Add(start);
                            captions.Add(text);
                        }
                    }
                    finally
                    {
                    }
                }
                string filename = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath)) + ".srt";
                buildSrt(timestamps, captions, filename);
                //got list of timestamps here and list of texts, start building the .srt file
                Console.WriteLine("Done " + filename);

            }

            Console.WriteLine("Press any key to exit the program...");
            Console.ReadKey();
        }

        static bool buildSrt(ArrayList timestamps, ArrayList captions, string path)
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

    }
}