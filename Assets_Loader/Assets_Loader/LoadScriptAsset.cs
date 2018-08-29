using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets_Loader.Models;

namespace Assets_Loader
{
    class LoadScriptAsset
    {
        public void LoadScript(byte[] asset, string name)
        {
            var script = FindScriptByName(name);
            if (script != null)
            {
                script.Script = asset;
                Console.WriteLine("Asset {0} was overriden.", script.Name);
                return;
            }
            var scriptParseTree = new ScriptParseTreeModel();
            scriptParseTree.Name = name;
            scriptParseTree.Length = asset.Length;
            scriptParseTree.Script = asset;
            Load_ScriptParseTreeAsset();
            Console.WriteLine("Asset {0} was added.", name);
        }

        private ScriptParseTreeModel FindScriptByName(string name)
        {
            for (int i = 0; i < 4096; i++)
            {
                var header = new XAssetHeaderModel(i);
                var script = new ScriptParseTreeModel(header.XAsset);
                if (header.Type == 0x30 && script.Name.Contains(name))
                    return script;
            }
            return null;
        }

        private void Load_ScriptParseTreeAsset()
        {
            int ptr = Utils.Malloc(Stubs.Load_ScriptParseTreeAssetStub.Length);
            Array.Copy(BitConverter.GetBytes(Offsets.Load_ScriptParseTreeAsset), 0, Stubs.Load_ScriptParseTreeAssetStub, 1, 4);
            Utils.Write(ptr, Stubs.Load_ScriptParseTreeAssetStub);
            Utils.CreateRemoteThread(ptr);
            Utils.Free(ptr, Stubs.Load_ScriptParseTreeAssetStub.Length);
        }
    }
}
