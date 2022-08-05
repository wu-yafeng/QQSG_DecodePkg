using System;
using System.IO;
using System.Text;

namespace Seaching
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var outdir = Path.Combine(Path.GetTempPath(), "qqsg_objects");

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // term sample: *.txt

            Console.WriteLine("输入搜索term, 如：*.txt 若要搜索全部文件，则输入*");

            var term = Console.ReadLine();

            foreach (var path in Directory.GetFiles(outdir))
            {
                var content = File.ReadAllText(path, Encoding.GetEncoding("GB2312"));

                if (content.Contains(term))
                {
                    Console.WriteLine($"term {term} FOUND:{path}");
                    continue;
                }
            }

        }

    }
}
