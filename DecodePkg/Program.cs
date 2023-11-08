﻿using DecodePkg;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

string[] export_types = new[]
        {
            ".lua",
            ".txt",
        };

string content = "";

bool ok = false;

string error = "";

string filePath = string.Empty;

// for linux or mac os platform, we need interact with user for pkg file path.
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Tencent\QQSG\SYS");

    if (key != null && key.GetValue("pathRoot") != null)
    {
        filePath = key.GetValue("pathRoot").ToString() + @"\data\update.pkg";
    }

    Console.WriteLine("使用文件 {0} --- 按回城确认  CTRL+A 取消", filePath);

    var readkey = Console.ReadKey();

    if (readkey.Modifiers.HasFlag(ConsoleModifiers.Control) && readkey.Key == ConsoleKey.A)
    {
        filePath = string.Empty;
    }
}
else
{
    filePath = string.Empty;
}


while (!File.Exists(filePath))
{
    Console.WriteLine("拖入pkg文件到该控制台");

    filePath = Console.ReadLine();
}

// 有时候路径有空格，导致读不到路径
filePath = filePath.Trim('"');

var outdir = Path.Combine(Path.GetTempPath(), "qqsg_objects");

Console.WriteLine("输出目录为 {0}", outdir);

string[] accpetableInputs = ["Y", "N", "YES", "NO"];

Console.WriteLine("仅导出 {0} 扩展名的文件？", string.Join("|", export_types));

var approval_type = string.Empty;

while (!accpetableInputs.Contains(approval_type))
{
    Console.WriteLine("输入 Y/N (YES/NO) 默认回车 YES");

    approval_type = Console.ReadLine();

    approval_type = string.IsNullOrEmpty(approval_type) ? "YES" : approval_type;
}


if ("N".Equals(approval_type, StringComparison.CurrentCultureIgnoreCase) || "NO".Equals(approval_type, StringComparison.CurrentCultureIgnoreCase))
{
    Console.WriteLine("输入你需要导出的文件扩展名, 每个扩展名用 , 分隔，输入回车确认.(不输入任何扩展名，按回车则导出所有扩展名的文件)");

    var extensions = Console.ReadLine();

    if (string.IsNullOrEmpty(extensions))
    {
        export_types = [];
    }
    else
    {
        export_types = extensions.Split(",").Select(x => x.StartsWith(".") ? x : $".{x}").ToArray();
    }

    export_types = [];
}

_ = Task.Run(() => WriteToDiskAsync(filePath, outdir)).ContinueWith(x =>
{
    if (x.Exception != null)
    {
        ok = true;

        error = x.Exception.ToString();
    }
});

while (!ok)
{
    Console.Clear();
    Console.WriteLine(content);

    Thread.Sleep(200);
}

Console.WriteLine("执行完成");

Console.WriteLine(error);

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    Process.Start("explorer.exe", outdir);
}

Console.ReadKey();



void WriteToDiskAsync(string fileName, string outdir)
{
    var outputdir = Path.Join(outdir, Path.GetFileName(fileName));

    if(Directory.Exists(outputdir))
    {
        // delete existing directory.
        Directory.Delete(outputdir, true);
    }

    Directory.CreateDirectory(outputdir);

    var resource = 0;
    var txt = 0;
    var other = 0;
    var formattedString = "输出目录为: " + outputdir + " 资源文件(gsn|gso|gsa|ef3):{0}  txt文件：{1}  其他 {2}";

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

            //continue;
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

        if (export_types.Length == 0 || export_types.Contains(extension))
        {

            var privateKey = string.Empty;

            text = Decompress(text);

            if (extension == ".txt" && text.Length > 4 && text[0] == 63 && text[1] == 83 && text[2] == 63 && text[3] == 71)
            {
                privateKey = "pleasebecareful0";

                // 头4个字节是标志是否是加密的txt
                text = text[4..];
            }

            if (extension == ".lua" && text.TakeLast(4).SequenceEqual("QQSG".Select(x => (byte)x)))
            {
                privateKey = "leaf12345678yech";
            }

            var outfilename = Path.Join(outputdir, name);

            if (!Directory.Exists(Path.GetDirectoryName(outfilename)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outfilename));
            }

            using var file = File.Create(outfilename);

            // 密钥:
            file.Write(string.IsNullOrEmpty(privateKey) ? text : Decrypter.Decrypt(text, Encoding.ASCII.GetBytes(privateKey)));

            file.Flush();
        }
    }

    ok = true;
}

string ReadString(FileStream stream, int length)
{
    var buffer = new byte[length];

    stream.Read(buffer, 0, length);

    return Encoding.UTF8.GetString(buffer);
}

ushort ReadShort(FileStream stream)
{
    var buffer = new byte[2];

    stream.Read(buffer, 0, 2);

    return BitConverter.ToUInt16(buffer);
}

uint ReadInt(FileStream stream)
{
    var buffer = new byte[4];

    var ans = stream.Read(buffer, 0, 4);

    return BitConverter.ToUInt32(buffer);
}

byte[] Decompress(byte[] data)
{
    using var memory = new MemoryStream(data);

    using var result = new MemoryStream();

    var inputStream = new InflaterInputStream(memory);

    inputStream.CopyTo(result);

    return result.ToArray();
}
