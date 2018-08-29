using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets_Loader.Models
{
    class XAssetHeaderModel
    {
        private readonly int XAssetHeaderPtr = Offsets.Assets_Pool;
        private const int Size = 16;
        private readonly int Index;
        public XAssetHeaderModel(int index)
        {
            Index = index;
        }

        private int currentAssetPointer
        {
            get { return XAssetHeaderPtr + Index*Size; }
        }
        public int Type
        {
            get { return Utils.ReadInt(currentAssetPointer); }
        }

        public int XAsset
        {
            get { return Utils.ReadInt(currentAssetPointer + 4); }
        }
    }
}
