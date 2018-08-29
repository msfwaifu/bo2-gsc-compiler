using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets_Loader.Models
{
    class ScriptParseTreeModel
    {
        private readonly int Pointer;

        public ScriptParseTreeModel() { }

        public ScriptParseTreeModel(int pointer)
        {
            Pointer = pointer;
        }

        public string Name
        {
            get { return Utils.ReadString(Pointer); }
            set
            {
                int NamePtr = Utils.Malloc(value.Length);
                Utils.Write(NamePtr, Encoding.ASCII.GetBytes(value + '\0'));
                Utils.WriteInt(0x1005A000, NamePtr);
            }
        }

        public int Length
        {
            set { Utils.WriteInt(0x1005A004, value); }
        }

        public byte[] Script
        {
            set
            {
                int scriptPtr = Utils.Malloc(value.Length);
                Utils.Write(scriptPtr, value);
                Utils.WriteInt(Pointer != 0 ? Pointer + 8 : 0x1005A008, scriptPtr);
            }
        }
    }
}
