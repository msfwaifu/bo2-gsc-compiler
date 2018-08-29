using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets_Loader
{
    static class Offsets
    {
        static Offsets()
        {
            Load_ScriptParseTreeAsset = FindPattern(0x400000, 0x900000,
                "\x83\xEC\x10\x8B\x4C\x24\x14\x56",
                "xxxxxxxx");
            Assets_Pool = FindPattern(0x400000, 0x900000,
                "\x8D\x88\x00\x00\x00\x00\x89\x88\x00\x00\x00\x00\x83\xC0\x10", "xx????xx????xxx");
            Assets_Pool = Fix(Assets_Pool, 2, 0);
        }

        public static int Load_ScriptParseTreeAsset { get; private set; }

        public static int Assets_Pool { get; private set; }

        private static int Fix(int address, int offset, int correct)
        {
            var ptr = Utils.ReadInt(address + offset);
            return (ptr + correct);
        }
        
        private static int FindPattern(int startAddress, int endAddress, string pattern, string mask)
        {
            byte[] lpBuffer = new byte[endAddress - startAddress];
            lpBuffer = Utils.Read(startAddress, lpBuffer.Length);
            for (int i = 0; i < lpBuffer.Length; i++)
            {
                if (pattern.TakeWhile((t, j) => (lpBuffer[i + j] == t) || (mask[j] == '?')).Where((t, j) => j == (pattern.Length - 1)).Any())
                {
                    return (startAddress + i);
                }
            }
            return -1;
        }
    }
}
