using System.Drawing;
using System.Net.Security;
using System.Text;
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
            return dir.Parent.Parent.Parent.Parent.Parent + "\\original";
        }

        private static string Modified()
        {
            var dir = new DirectoryInfo(Environment.CurrentDirectory);
            return dir.Parent.Parent.Parent.Parent.Parent + "\\modified";
        }

        private static string Difference()
        {
            var dir = new DirectoryInfo(Environment.CurrentDirectory);
            return dir.Parent.Parent.Parent.Parent.Parent + "\\difference";
        }

        private static string Reconstructed()
        {
            var dir = new DirectoryInfo(Environment.CurrentDirectory);
            return dir.Parent.Parent.Parent.Parent.Parent + "\\reconstructed";
        }


        static void Main(string[] args)
        {
            string joo = Original();
            DirectoryInfo ogDir = Directory.CreateDirectory(Original());
            DirectoryInfo modDir = Directory.CreateDirectory(Modified());
            DirectoryInfo diffDir = Directory.CreateDirectory(Difference());
            DirectoryInfo recDir = Directory.CreateDirectory(Reconstructed());

            double totalOgSize = 0;
            double totalModSize = 0;
            double totalDiffSize = 0;

            foreach (FileInfo file in ogDir.GetFiles())
            {
                string name = file.Name;
                using var originalStream = File.OpenRead(ogDir.FullName + "\\" + name);
                using var modifiedStream = File.OpenRead(modDir.FullName + "\\" + name);
                using var deltaStream = File.Create(diffDir.FullName + "\\" + name);

                DateTime encodeStart = DateTime.Now;
                using VcEncoder coder = new VcEncoder(originalStream, modifiedStream, deltaStream);
                VCDiffResult encodeRes = coder.Encode();

                var encodeDuration = DateTime.Now - encodeStart;



                // --- Transfer difference files over network ---


                originalStream.Position = 0;
                modifiedStream.Position = 0;
                deltaStream.Position = 0;

                using var outputStream = File.Create(recDir.FullName + "\\" + name);
                DateTime decodeStart = DateTime.Now;
                using VcDecoder decoder = new VcDecoder(originalStream, deltaStream, outputStream);
                VCDiffResult decodeRes = decoder.Decode(out long bytesWritten);



                // --- Just printing ---



                FileInfo delta = new FileInfo(diffDir.FullName + "\\" + name);
                FileInfo mod = new FileInfo(modDir.FullName + "\\" + name);
                FileInfo rec = new FileInfo(recDir.FullName + "\\" + name);
                PrintFileInfo(file, mod, delta, rec, ref totalOgSize, ref totalModSize, ref totalDiffSize);
                Console.WriteLine("Encoding duration: {0}", encodeDuration.TotalMilliseconds + "ms");
                Console.WriteLine("Decoding duration: {0}", (DateTime.Now - decodeStart).TotalMilliseconds + "ms");
                Console.WriteLine(Environment.NewLine);
            }

            double modToOg = (totalModSize / totalOgSize) * 100;
            double diffToMod = (totalDiffSize / totalModSize) * 100;
            Console.WriteLine("Total original size: {0}", Math.Round(ConvertBytesToKB(totalOgSize), 1));
            Console.WriteLine("Total modified size: {0} ({1}%)", Math.Round(ConvertBytesToKB(totalModSize), 1), Math.Round(modToOg, 1));
            Console.WriteLine("Total diff size: {0} ({1}%)", Math.Round(ConvertBytesToKB(totalDiffSize), 1), Math.Round(diffToMod, 1));
        }

        private static void PrintFileInfo(FileInfo ogFile, FileInfo modFile, FileInfo diffFile, FileInfo recFile, ref double totalOgSize, ref double totalModSize, ref double totalDiffSize)
        {
            double ogSize = ConvertBytesToKB(ogFile.Length);
            double modSize = ConvertBytesToKB(modFile.Length);
            double diffSize = ConvertBytesToKB(diffFile.Length);
            double recSize = ConvertBytesToKB(recFile.Length);

            double modToOg = (modSize / ogSize) * 100;
            double diffToMod = (diffSize / modSize) * 100;

            Console.WriteLine("Name: {0}", ogFile.Name);
            Console.WriteLine("Original: {0}kB", Math.Round(ogSize, 1));
            Console.WriteLine("Modified: {0}kB --> {1}% compared to original", Math.Round(modSize, 1), Math.Round(modToOg, 1));
            Console.WriteLine("Difference: {0}kB --> {1}% compared to modified", Math.Round(diffSize, 1), Math.Round(diffToMod, 1));
            Console.WriteLine("Reconstructed: {0}kB", Math.Round(recSize, 1));

            totalOgSize += ogSize;
            totalModSize += modSize;
            totalDiffSize += diffSize;
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