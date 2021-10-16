using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ld2Extractor
{
    class Program
    {
        public static string TestLD2Path = "Test.ld2";
        static void Main(string[] args)
        {
            MainWin win = new MainWin();
            if (args.Length >= 3)
            {
                Helper.DEBUG = args[1].Contains("True", StringComparison.OrdinalIgnoreCase);
                int debug;
                if (int.TryParse(args[1], out debug))
                {
                    Helper.DEBUG |= debug > 0;
                }
                win.Main(args[0], args[1]);
            }
            else if (args.Length >= 2)
                win.Main(args[0], args[1]);
            else
            {
                Console.WriteLine("参数不存在,使用默认参数!");
                Console.WriteLine($"eg: Ld2Extractor.exe {TestLD2Path} Test.txt");
                if (System.IO.File.Exists(TestLD2Path))
                    win.Main(TestLD2Path, "Test.txt");
                else
                {
                    Console.WriteLine($"字典文件{TestLD2Path}不存在!");
                }
            }
        }
    }
}
