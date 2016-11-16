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
                
                string filename = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath)) + ".srt";

                CaptionToSrt aCaption = new CaptionToSrt(filePath);
                //got list of timestamps here and list of texts, start building the .srt file
                aCaption.OutFile = filename;

                string srtContent = aCaption.preparesrt();
                aCaption.publishSrt(srtContent);

                Console.WriteLine("Done " + filename);

            }

            Console.WriteLine("Press any key to exit the program...");
            Console.ReadKey();
        }

       

    }
}