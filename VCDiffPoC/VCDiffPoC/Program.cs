using System.Drawing;
using System.IO.Compression;
using System.Net.Security;
using System.Text;
using System.Xml.Linq;
using VCDiff.Decoders;
using VCDiff.Encoders;
using VCDiff.Includes;

namespace VCDiffPoC
{
    internal class Program
    {
        private static string Original()
        {
            var dir = new DirectoryInfo(Environment.CurrentDirectory);
            return dir.Parent.Parent.Parent.Parent.Parent + "\\rev1";
        }

        private static string Modified()
        {
            var dir = new DirectoryInfo(Environment.CurrentDirectory);
            return dir.Parent.Parent.Parent.Parent.Parent + "\\rev2";
        }

        private static string VCDiffEncoded()
        {
            var dir = new DirectoryInfo(Environment.CurrentDirectory);
            return dir.Parent.Parent.Parent.Parent.Parent + "\\vcdiff_encoded";
        }

        private static string GZipEncoded()
        {
            var dir = new DirectoryInfo(Environment.CurrentDirectory);
            return dir.Parent.Parent.Parent.Parent.Parent + "\\gzip_encoded";
        }

        private static string DeflateEncoded()
        {
            var dir = new DirectoryInfo(Environment.CurrentDirectory);
            return dir.Parent.Parent.Parent.Parent.Parent + "\\deflate_encoded";
        }

        private static string BrotliEncoded()
        {
            var dir = new DirectoryInfo(Environment.CurrentDirectory);
            return dir.Parent.Parent.Parent.Parent.Parent + "\\brotli_encoded";
        }

        private static string VCDiffDecoded()
        {
            var dir = new DirectoryInfo(Environment.CurrentDirectory);
            return dir.Parent.Parent.Parent.Parent.Parent + "\\vcdiff_decoded";
        }

        private static string GZipDecoded()
        {
            var dir = new DirectoryInfo(Environment.CurrentDirectory);
            return dir.Parent.Parent.Parent.Parent.Parent + "\\gzip_decoded";
        }

        private static string DeflateDecoded()
        {
            var dir = new DirectoryInfo(Environment.CurrentDirectory);
            return dir.Parent.Parent.Parent.Parent.Parent + "\\deflate_decoded";
        }

        private static string BrotliDecoded()
        {
            var dir = new DirectoryInfo(Environment.CurrentDirectory);
            return dir.Parent.Parent.Parent.Parent.Parent + "\\brotli_decoded";
        }

        private static HashSet<string> _skipExtensions = new HashSet<string> { ".pdf", ".jpg", ".png", ".txt" };

        static void Main(string[] args)
        {
            string joo = Original();
            DirectoryInfo ogDir = Directory.CreateDirectory(Original());
            Directory.CreateDirectory(Modified());
            Directory.CreateDirectory(VCDiffEncoded());
            Directory.CreateDirectory(VCDiffDecoded());
            Directory.CreateDirectory(GZipEncoded());
            Directory.CreateDirectory(GZipDecoded());
            Directory.CreateDirectory(DeflateEncoded());
            Directory.CreateDirectory(DeflateDecoded());
            Directory.CreateDirectory(BrotliEncoded());
            Directory.CreateDirectory(BrotliDecoded());

            for (int i = 0; i < encodeFuncs.Count; i++)
            {
                double totalOgSize = 0;
                double totalModSize = 0;
                double totalDiffSize = 0;

                var encode = encodeFuncs[i];
                var decode = decodeFuncs[i];

                string funcName = encode.Method.Name;
                var files = ogDir.GetFiles();
                int j = 0;
                foreach (FileInfo file in files)
                {
                    j++;
                    string name = file.Name;
                    if (_skipExtensions.Contains(file.Extension))
                    {
                        continue;
                    }

                    TimeSpan encodeDuration = encode.Invoke(name);
                    encodeTime[i] += encodeDuration;

                    TimeSpan decodeDuration = decode.Invoke(name);
                    decodeTime[i] += decodeDuration;

                    PrintFileResult(funcName, file, encodeDir[i], decodeDir[i], ref totalOgSize, ref totalModSize, ref totalDiffSize, encodeDuration, decodeDuration, false);
                }
                PrintTotalResult(funcName, totalModSize, totalOgSize, totalDiffSize, encodeTime[i], decodeTime[i]);
            }
        }

        private static List<Func<string, TimeSpan>> encodeFuncs = new List<Func<string, TimeSpan>> { VCDiffEncode, GZipEncode, DeflateEncode, BrotliEncode };
        private static List<Func<string, TimeSpan>> decodeFuncs = new List<Func<string, TimeSpan>> { VCDiffDecode, GZipDecode, DeflateDecode, BrotliDecode };
        private static List<Func<string>> encodeDir = new List<Func<string>> { VCDiffEncoded, GZipEncoded, DeflateEncoded, BrotliEncoded };
        private static List<Func<string>> decodeDir = new List<Func<string>> { VCDiffDecoded, GZipDecoded, DeflateDecoded, BrotliDecoded };
        private static List<TimeSpan> encodeTime = new List<TimeSpan> { TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero };
        private static List<TimeSpan> decodeTime = new List<TimeSpan> { TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero };

        private static TimeSpan VCDiffEncode(string name)
        {
            DirectoryInfo ogDir = new DirectoryInfo(Original());
            DirectoryInfo modDir = new DirectoryInfo(Modified());
            DirectoryInfo diffDir = new DirectoryInfo(VCDiffEncoded());

            FileInfo og = new FileInfo(ogDir.FullName + "\\" + name);
            FileInfo mod = new FileInfo(modDir.FullName + "\\" + name);
            FileInfo diff = new FileInfo(diffDir.FullName + "\\" + name);

            if (!og.Exists || !mod.Exists)
            {
                Console.WriteLine("VCDiffEncode: File " + name + " does not exist");
                return TimeSpan.Zero;
            }

            using var originalStream = File.Open(og.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var modifiedStream = File.Open(mod.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var deltaStream = File.Create(diff.FullName);

            DateTime encodeStart = DateTime.Now;
            using VcEncoder coder = new VcEncoder(originalStream, modifiedStream, deltaStream);
            VCDiffResult encodeRes = coder.Encode();

            return DateTime.Now - encodeStart;
        }

        private static TimeSpan VCDiffDecode(string name)
        {
            DirectoryInfo ogDir = new DirectoryInfo(Original());
            DirectoryInfo recDir = new DirectoryInfo(VCDiffDecoded());
            DirectoryInfo diffDir = new DirectoryInfo(VCDiffEncoded());

            FileInfo og = new FileInfo(ogDir.FullName + "\\" + name);
            FileInfo diff = new FileInfo(diffDir.FullName + "\\" + name);
            FileInfo rec = new FileInfo(recDir.FullName + "\\" + name);

            if (!og.Exists || !diff.Exists)
            {
                Console.WriteLine("VCDiffDecode: File " + name + " does not exist");
                return TimeSpan.Zero;
            }

            using var originalStream = File.Open(og.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var deltaStream = File.Open(diff.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var outputStream = File.Create(rec.FullName);

            DateTime decodeStart = DateTime.Now;
            using VcDecoder decoder = new VcDecoder(originalStream, deltaStream, outputStream);
            VCDiffResult decodeRes = decoder.Decode(out long bytesWritten);
            return DateTime.Now - decodeStart;
        }

        private static TimeSpan GZipEncode(string name)
        {
            DirectoryInfo ogDir = new DirectoryInfo(Original());
            DirectoryInfo diffDir = new DirectoryInfo(GZipEncoded());

            FileInfo og = new FileInfo(ogDir.FullName + "\\" + name);
            FileInfo diff = new FileInfo(diffDir.FullName + "\\" + name);

            if (!og.Exists)
            {
                Console.WriteLine("GZipEncode: File " + name + " does not exist");
                return TimeSpan.Zero;
            }

            using var originalStream = File.Open(og.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var compressedStream = File.Create(diff.FullName);
            using var gzipStream = new GZipStream(compressedStream, CompressionMode.Compress);

            DateTime encodeStart = DateTime.Now;
            originalStream.CopyTo(gzipStream);
            return DateTime.Now - encodeStart;
        }

        private static TimeSpan GZipDecode(string name)
        {
            DirectoryInfo decompressed = new DirectoryInfo(GZipDecoded());
            DirectoryInfo diffDir = new DirectoryInfo(GZipEncoded());

            FileInfo diff = new FileInfo(diffDir.FullName + "\\" + name);
            FileInfo dec = new FileInfo(decompressed.FullName + "\\" + name);

            if (!diff.Exists)
            {
                Console.WriteLine("GZipDecode: File " + name + " does not exist");
                return TimeSpan.Zero;
            }

            using var compressedStream = File.Open(diff.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var decompressedStream = File.Create(dec.FullName);
            using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);

            DateTime decodeStart = DateTime.Now;
            gzipStream.CopyTo(decompressedStream);
            return DateTime.Now - decodeStart;
        }

        private static TimeSpan DeflateEncode(string name)
        {
            DirectoryInfo ogDir = new DirectoryInfo(Original());
            DirectoryInfo diffDir = new DirectoryInfo(DeflateEncoded());

            FileInfo og = new FileInfo(ogDir.FullName + "\\" + name);
            FileInfo diff = new FileInfo(diffDir.FullName + "\\" + name);

            if (!og.Exists)
            {
                Console.WriteLine("DeflateEncode: File " + name + " does not exist");
                return TimeSpan.Zero;
            }

            using var originalStream = File.Open(og.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var compressedStream = File.Create(diff.FullName);
            using var gzipStream = new DeflateStream(compressedStream, CompressionMode.Compress);

            DateTime encodeStart = DateTime.Now;
            originalStream.CopyTo(gzipStream);
            return DateTime.Now - encodeStart;
        }

        private static TimeSpan DeflateDecode(string name)
        {
            DirectoryInfo decompressed = new DirectoryInfo(DeflateDecoded());
            DirectoryInfo diffDir = new DirectoryInfo(DeflateEncoded());

            FileInfo diff = new FileInfo(diffDir.FullName + "\\" + name);
            FileInfo dec = new FileInfo(decompressed.FullName + "\\" + name);

            if (!diff.Exists)
            {
                Console.WriteLine("DeflateDecode: File " + name + " does not exist");
                return TimeSpan.Zero;
            }

            using var compressedStream = File.Open(diff.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var decompressedStream = File.Create(dec.FullName);
            using var gzipStream = new DeflateStream(compressedStream, CompressionMode.Decompress);

            DateTime decodeStart = DateTime.Now;
            gzipStream.CopyTo(decompressedStream);
            return DateTime.Now - decodeStart;
        }

        private static TimeSpan BrotliEncode(string name)
        {
            DirectoryInfo ogDir = new DirectoryInfo(Original());
            DirectoryInfo diffDir = new DirectoryInfo(BrotliEncoded());

            FileInfo og = new FileInfo(ogDir.FullName + "\\" + name);
            FileInfo diff = new FileInfo(diffDir.FullName + "\\" + name);

            if (!og.Exists)
            {
                Console.WriteLine("BrotliEncode: File " + name + " does not exist");
                return TimeSpan.Zero;
            }

            using var originalStream = File.Open(og.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var compressedStream = File.Create(diff.FullName);
            using var gzipStream = new BrotliStream(compressedStream, CompressionMode.Compress);

            DateTime encodeStart = DateTime.Now;
            originalStream.CopyTo(gzipStream);
            return DateTime.Now - encodeStart;
        }

        private static TimeSpan BrotliDecode(string name)
        {
            DirectoryInfo decompressed = new DirectoryInfo(BrotliDecoded());
            DirectoryInfo diffDir = new DirectoryInfo(BrotliEncoded());

            FileInfo diff = new FileInfo(diffDir.FullName + "\\" + name);
            FileInfo dec = new FileInfo(decompressed.FullName + "\\" + name);

            if (!diff.Exists)
            {
                Console.WriteLine("BrotliDecode: File " + name + " does not exist");
                return TimeSpan.Zero;
            }

            using var compressedStream = File.Open(diff.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var decompressedStream = File.Create(dec.FullName);
            using var gzipStream = new BrotliStream(compressedStream, CompressionMode.Decompress);

            DateTime decodeStart = DateTime.Now;
            gzipStream.CopyTo(decompressedStream);
            return DateTime.Now - decodeStart;
        }

        private static void PrintTotalResult(string funcName, double totalModSize, double totalOgSize, double totalDiffSize, TimeSpan totalEncodeDuration, TimeSpan totalDecodeDuration)
        {
            double modToOg = ((totalModSize / totalOgSize) - 1) * 100;
            double diffToMod = (totalDiffSize / totalModSize) * 100;
            double efficiency = totalModSize / totalDiffSize;
            Console.WriteLine("-----------------------------------------------------------------");
            Console.WriteLine("Func:                  {0}", funcName);
            Console.WriteLine("Total original size:   {0}kB", Math.Round(totalOgSize, 1));
            Console.WriteLine("Total modified size:   {0}kB ({1}% growth)", Math.Round(totalModSize, 1), Math.Round(modToOg, 1));
            Console.WriteLine("Total diff size:       {0}kB ({1}% compared to modified)", Math.Round(totalDiffSize, 1), Math.Round(diffToMod, 1));
            Console.WriteLine("Efficiency:            {0}", Math.Round(efficiency, 1));
            Console.WriteLine("Total encode duration: {0}", totalEncodeDuration.TotalMilliseconds + "ms");
            Console.WriteLine("Total decode duration: {0}", totalDecodeDuration.TotalMilliseconds + "ms");
            Console.WriteLine("-----------------------------------------------------------------");
            Console.WriteLine(Environment.NewLine);
        }

        private static void PrintFileResult(string funcName, FileInfo ogFile, Func<string> encodeDir, Func<string> decodeDir, ref double totalOgSize, ref double totalModSize, ref double totalEncSize, TimeSpan encodeDuration, TimeSpan decodeDuration, bool print)
        {
            DirectoryInfo ogDir = new DirectoryInfo(Original());
            DirectoryInfo modDir = new DirectoryInfo(Modified());
            DirectoryInfo encDir = new DirectoryInfo(encodeDir.Invoke());
            DirectoryInfo decDir = new DirectoryInfo(decodeDir.Invoke());

            string name = ogFile.Name;

            FileInfo encFile = new FileInfo(encDir.FullName + "\\" + name);
            FileInfo modFile = new FileInfo(modDir.FullName + "\\" + name);
            FileInfo decFile = new FileInfo(decDir.FullName + "\\" + name);

            double ogSize = 0;
            FileInfo og = new FileInfo(ogDir.FullName + "\\" + name);
            if (og.Exists)
            {
                ogSize = ConvertBytesToKB(ogFile.Length);
            }

            double modSize = 0;
            if (modFile.Exists)
            {
                modSize = ConvertBytesToKB(modFile.Length);
            }

            double encSize = 0;
            if (encFile.Exists)
            {
                encSize = ConvertBytesToKB(encFile.Length);
            }

            double decSize = 0;
            if (decFile.Exists)
            {
                decSize = ConvertBytesToKB(decFile.Length);
            }

            double modToOg = ((modSize / ogSize) - 1) * 100;
            double encToMod = (encSize / modSize) * 100;
            double efficiency = modSize / encSize;

            if (print)
            {
                Console.WriteLine("Func:              {0}", funcName);
                Console.WriteLine("Name:              {0}", ogFile.Name);
                Console.WriteLine("Original:          {0}kB", Math.Round(ogSize, 1));
                Console.WriteLine("Modified:          {0}kB --> {1}% growth", Math.Round(modSize, 1), Math.Round(modToOg, 1));
                Console.WriteLine("Difference:        {0}kB --> {1}% compared to modified", Math.Round(encSize, 1), Math.Round(encToMod, 1));
                Console.WriteLine("Efficiency:        {0}", Math.Round(efficiency, 1));
                Console.WriteLine("Reconstructed:     {0}kB", Math.Round(decSize, 1));
                Console.WriteLine("Encoding duration: {0}", encodeDuration.TotalMilliseconds + "ms");
                Console.WriteLine("Decoding duration: {0}", decodeDuration.TotalMilliseconds + "ms");
                Console.WriteLine(Environment.NewLine);
            }

            totalOgSize += ogSize;
            totalModSize += modSize;
            totalEncSize += encSize;
        }

        public static double ConvertBytesToKB(double bytes)
        {
            return bytes / 1024f;
        }

        public static double ConvertBytesToMB(double bytes)
        {
            return ConvertBytesToKB(bytes) / 1024f;
        }
    }
}