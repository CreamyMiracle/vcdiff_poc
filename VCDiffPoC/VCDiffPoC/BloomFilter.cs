using HashDepot;

using System.Collections;
using System.Text;

namespace VCDiffPoC
{
    public class BloomFilter
    {
        public readonly BitArray Bits;
        public readonly Func<byte[], uint>[] Hashes;

        public BloomFilter(int size = 1024, int hashCount = 8)
        {
            Bits = new BitArray(size);
            var rand = new Random();
            Hashes = Enumerable
                .Repeat(int.MaxValue, hashCount)
                .Select(max => rand.Next(0, max))
                .Select(seed => {
                    Func<byte[], uint> func = bytes => Fnv1aHash(bytes, (uint)seed);
                    return func;
                })
                .ToArray();
        }

        public static uint Fnv1aHash(byte[] input, uint seed)
        {
            const int fnvPrime = 0x01000193; // 16777619
            uint hash = seed;

            foreach (char c in input)
            {
                hash ^= c;
                hash *= fnvPrime;
            }

            return hash;
        }

        public void Add(string s)
        {
            ComputeHashes(s).ToList().ForEach(n => Bits.Set((int)n, true));
        }

        public bool Check(string s)
        {
            return ComputeHashes(s).All(n => Bits.Get((int)n));
        }

        private uint[] ComputeHashes(string s)
        {
            return Hashes.Select(hash => ComputeHash(hash, s)).ToArray();
        }

        private uint ComputeHash(Func<byte[], uint> hashFn, string s)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(s);
            uint hash = hashFn(bytes);
            return hash % (uint)Bits.Length;
        }
    }
}
