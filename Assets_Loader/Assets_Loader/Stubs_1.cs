using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets_Loader
{
    class Stubs
    {

        public static byte[] Load_ScriptParseTreeAssetStub = new byte[]
        {
            0xB9, 0x00, 0x00, 0x00, 0x00, //mov ecx, 0
            0x68, 0x00, 0xA0, 0x05, 0x10, //push 0x1005a000
            0xB8, 0x30, 0x00, 0x00, 0x00, // mov eax, 0x30
            0xFF, 0xD1,//call ecx
            0x83, 0xC4, 0x04,//add esp, 4
            0xC3 //retn
        };
    }
}
