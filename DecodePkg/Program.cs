using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DecodePkg
{
    internal class Program
    {
        static string[] export_types = new[]
        {
            ".lua",
            ".txt"
        };

        static string content = "";

        static bool ok;

        static void Main(string[] args)
        {
            string filePath = string.Empty;

            while (!File.Exists(filePath))
            {
                filePath = Console.ReadLine();
            }

            var outdir = Path.Combine(Path.GetTempPath(), "qqsg_objects");

            Console.WriteLine("output dir is ");


            _ = Task.Run(() => WriteToDiskAsync(filePath, outdir));

            while (!ok)
            {
                Console.Clear();
                Console.WriteLine(content);

                Thread.Sleep(200);
            }

            Console.WriteLine("执行完成");

            Console.ReadKey();
        }

        private static void WriteToDiskAsync(string fileName, string outdir)
        {
            var resource = 0;
            var txt = 0;
            var other = 0;
            var formattedString = "资源文件(gsn|gso|gsa|ef3):{0}  txt文件：{1}  其他 {2}";

            content = string.Format(formattedString, resource, txt, other);


            var reader = File.OpenRead(fileName);

            // 4字节，貌似是版本号
            var version = ReadInt(reader);

            // 该压缩包里的文件数
            var filenums = ReadInt(reader);

            // 文件列表摘要信息的偏移位置
            var filename_table_offset = ReadInt(reader);
            var filename_table_len = ReadInt(reader);

            // 移动到文件表
            reader.Seek(filename_table_offset, SeekOrigin.Begin);

            for (var index = 0; index < filenums; index++)
            {
                var name_len = ReadShort(reader);

                var name = ReadString(reader, name_len);

                var flag = ReadInt(reader);

                var offset = ReadInt(reader);
                var size = ReadInt(reader);// 文件原始的大小

                var zlib_size = (int)ReadInt(reader);// 文件压缩后的大小

                var current_position = reader.Position;

                reader.Seek(offset, SeekOrigin.Begin);
                var text = new byte[zlib_size];
                reader.Read(text, 0, zlib_size);

                // 还原到下一个文件的位置
                reader.Seek(current_position, SeekOrigin.Begin);

                var extension = Path.GetExtension(name);

                if (extension == ".gsn" || extension == ".gso" || extension == ".sk3" || extension == ".ef3" || extension == ".map" || extension == ".srv" || extension == ".gsa" || extension == ".av3" || extension == ".grp" || extension == ".rp")
                {
                    resource++;
                    content = string.Format(formattedString, resource, txt, other);

                    continue;
                }

                else if (extension == ".txt")
                {
                    txt++;
                    content = string.Format(formattedString, resource, txt, other);
                }

                else
                {
                    other++;
                    content = string.Format(formattedString, resource, txt, other);
                }

                if(export_types.Contains(extension))
                {
                    var outfilename = Path.Join(outdir, Path.GetFileName(fileName), name);

                    if (!Directory.Exists(Path.GetDirectoryName(outfilename)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(outfilename));
                    }

                    using var file = File.Open(outfilename, FileMode.OpenOrCreate);

                    file.Write(Decompress(text));

                    file.Flush();
                }
            }

            ok = true;
        }

        private static string ReadString(FileStream stream, int length)
        {
            var buffer = new byte[length];

            stream.Read(buffer, 0, length);

            return Encoding.UTF8.GetString(buffer);
        }

        private static ushort ReadShort(FileStream stream)
        {
            var buffer = new byte[2];

            stream.Read(buffer, 0, 2);

            return BitConverter.ToUInt16(buffer);
        }

        private static uint ReadInt(FileStream stream)
        {
            var buffer = new byte[4];

            var ans = stream.Read(buffer, 0, 4);

            return BitConverter.ToUInt32(buffer);
        }

        private static byte[] Decompress(byte[] data)
        {
            using var memory = new MemoryStream(data);

            using var result = new MemoryStream();

            var inputStream = new InflaterInputStream(memory);

            inputStream.CopyTo(result);

            return result.ToArray();
        }
    }
}
