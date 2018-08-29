using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Compiler;
using Irony.Parsing;

namespace Assets_Loader
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("Waiting for game...");
            while (!Utils.ConnectToGame()) ;
            Console.Clear();
            Console.WriteLine("Black Ops 2 GameScript Compiler by dtx12.");
            
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                    return;
                foreach (var file in ParseAllFiles(dialog.SelectedPath))
                {
                    var script = new LoadScriptAsset();
                    byte[] compiledScript = null;
                    if (file.Contains(".txt"))
                    {
                        var grammar = new GSCGrammar();
                        var parser = new Parser(grammar);
                        var compiler = new ScriptCompiler(parser.Parse(File.ReadAllText(file)), file);
                        if(!compiler.Init())
                            return;
                        compiledScript = compiler.Compiled;
                    }
                    else if(file.Contains("Compiled"))
                        continue;
                    else if(file.Contains(".gsc"))
                    {
                        compiledScript = File.ReadAllBytes(file);
                    }
                    script.LoadScript(compiledScript, Path.GetFileName(file.Replace(".txt", ".gsc")));
                }
            }
            Console.WriteLine("All scripts loaded and compiled. Press any key to quit.");
            Console.ReadKey();
        }

        private static IEnumerable<string> ParseAllFiles(string path)
        {
            var temp = Directory.GetFiles(path).ToList();
            foreach (var dir in Directory.GetDirectories(path))
            {
                temp.AddRange(ParseAllFiles(dir));
            }
            return temp;
        }
        
    }
}
