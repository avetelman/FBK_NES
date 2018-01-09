using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Fbk2
{
    class Program
    {
        private static string Folder = new Settings1().InputFolder;
        private static string Output = new Settings1().OutputFolder + @"\output_{0}.txt";
        private static object obj1 = new object();
        private static int _counter = 0;

        static void Main(string[] args)
        {
            var files = Directory.GetFiles(Folder);

            var f = files.Select(Search).ToArray();
            while (true)
            {
                Console.ReadKey();
            }
        }

        static async Task Search(string file)
        {
            await Task.Run(() =>
            {

                var pattern = @".*[а-яА-Я]+[a-zA-Z]+[а-яА-Я]+.*";
                var regex = new Regex(pattern);
                string filename = file.Substring(file.LastIndexOf("\\", StringComparison.Ordinal) + 1)
                    .Replace(".txt", "");

                var newFile = string.Format(Output, filename);
                lock (obj1)
                {
                    _counter++;
                    Console.Out.WriteLineAsync("Start of: " + filename + "\t\t\tIn work:" + _counter);
                }
                using (var f = new StreamReader(file))
                {
                    string line;

                    try
                    {
                        var str = File.ReadAllLines(newFile);
                        while ((line = f.ReadLine()) != null)
                        {
                            if (str.Last().Contains(line)) break;
                        }
                    }
                    catch (Exception)
                    {
                        // ignored
                    }

                    while ((line = f.ReadLine()) != null)
                    {
                        if (regex.IsMatch(line))
                        {
                            File.AppendAllText(newFile, $"{line};{filename}\n");
                        }
                    }
                    //System.Console.WriteLine(line);
                }

                lock (obj1)
                {
                    _counter--;
                    Console.Out.WriteLineAsync("End of: " + filename + "\t\t\tIn work:" + _counter);
                }

            });
        }
    }
}