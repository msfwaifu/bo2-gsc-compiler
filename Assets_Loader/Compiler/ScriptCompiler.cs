using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Irony.Parsing;
using System.IO;

namespace Compiler
{
    public partial class ScriptCompiler
    {
        private readonly ParseTree Tree;
        public static List<byte> CompiledPub = new List<byte>();
        private readonly List<GlobalString> GlobalStrings = new List<GlobalString>();
        private readonly List<RefString> RefStrings = new List<RefString>();
        private readonly List<Function> Functions = new List<Function>();
        private readonly List<string> LocalVariables = new List<string>();
        private readonly List<Call> Calls = new List<Call>();
        private readonly List<int> JumpOnTrueExprList = new List<int>();
        private byte numofParams;
        private readonly string Filename;
        private readonly List<int> LoopsStart = new List<int>();
        private readonly List<List<int>> BreakHistory = new List<List<int>>();

        public byte[] Compiled
        {
            get { return CompiledPub.ToArray(); }
        }
        public ScriptCompiler(ParseTree tree, string path)
        {
            CompiledPub.Clear();
            Tree = tree;
            Filename = path.Replace(".txt", ".gsc");
        }
        private void SetAlignedWord(byte offset = 0)
        {
            var alignedPos = (int)(CompiledPub.Count + 1 + offset & 0xFFFFFFFE);
            while (CompiledPub.Count < alignedPos)
            {
                CompiledPub.Add(0);
            }
        }
        private void SetAlignedDword(byte offset = 0)
        {
            var alignedPos = (int)(CompiledPub.Count + 3 + offset & 0xFFFFFFFC);
            while (CompiledPub.Count < alignedPos)
            {
                CompiledPub.Add(0);
            }
        }
        private void AddString(string str)
        {
            CompiledPub.AddRange(Encoding.ASCII.GetBytes(str + '\0'));
            GlobalStrings.Add(new GlobalString { Str = str, Position = (ushort)(CompiledPub.Count - str.Length - 1) });
        }    
        private void AddRefToCall(string name, byte numofParams, byte flag, ushort include)
        {
            foreach (
                Call call in
                    Calls.Where(
                        call =>
                            call.NamePtr == GetStringPosByName(name) && call.IncludePtr == include && call.Flag == flag &&
                            call.NumOfParams == numofParams)
                )
            {
                call.Refs.Add(CompiledPub.Count - 1);
                call.NumOfRefs++;
                return;
            }
            var refs = new List<int> { CompiledPub.Count - 1 };
            Calls.Add(new Call
            {
                NamePtr = GetStringPosByName(name),
                IncludePtr = include,
                NumOfRefs = 1,
                NumOfParams = numofParams,
                Flag = flag,
                Refs = refs
            });
        }
        private void AddRefToString(string str, byte type)
        {
            foreach (
                RefString refString in
                    RefStrings.Where(refString => refString.NamePtr == GetStringPosByName(str) && refString.Type == type)
                )
            {
                refString.Refs.Add(CompiledPub.Count);
                refString.NumOfRefs++;
                return;
            }
            var refs = new List<int> { CompiledPub.Count };
            RefStrings.Add(new RefString
            {
                NamePtr = GetStringPosByName(str),
                NumOfRefs = 1,
                Refs = refs,
                Type = type
            });
        }
        private void AddUInt(uint i)
        {
            CompiledPub.AddRange(BitConverter.GetBytes(i));
        }
        private void AddInt(int i)
        {
            CompiledPub.AddRange(BitConverter.GetBytes(i));
        }
        private void AddUshort(ushort u)
        {
            CompiledPub.AddRange(BitConverter.GetBytes(u));
        }
        private void WriteFunctionsStructToFile()
        {
            foreach (Function function in Functions)
            {
                AddUInt(function.Crc32);
                AddInt(function.Start);
                AddUshort(function.NamePtr);
                CompiledPub.Add(function.NumofParameters);
                CompiledPub.Add(0);
            }
        }
        private void WriteCallStackToFile()
        {
            foreach (Call call in Calls)
            {
                AddUshort(call.NamePtr);
                AddUshort(call.IncludePtr);
                AddUshort(call.NumOfRefs);
                CompiledPub.Add(call.NumOfParams);
                CompiledPub.Add(call.Flag);
                foreach (int _ref in call.Refs)
                {
                    AddInt(_ref);
                }
            }
        }
        private void WriteRefsToStringsToFile()
        {
            foreach (RefString refString in RefStrings)
            {
                AddUshort(refString.NamePtr);
                CompiledPub.Add(refString.NumOfRefs);
                CompiledPub.Add(refString.Type);
                foreach (int _ref in refString.Refs)
                {
                    AddInt(_ref);
                }
            }
        }
        public bool Init()
        {
            if (Tree.ParserMessages.Count > 0)
            {
                Console.WriteLine("Bad syntax in line {0}.", Tree.ParserMessages[0].Location.Line);
                Console.ReadKey();
                return false;
            }
            var file = new FileStructure();

            CompiledPub.AddRange(new byte[64]);
            PrepareStrings(Tree.Root);

            int functionsNodeIndex = Tree.Root.ChildNodes.FindIndex(e => e.Term.Name == "functions");
           
            file.CodeSectionStart       = CompiledPub.Count;
            file.NumOfFunctions         = (ushort)Tree.Root.ChildNodes[functionsNodeIndex].ChildNodes.Count;
            if (functionsNodeIndex != -1)
            {
                foreach (ParseTreeNode childNode in Tree.Root.ChildNodes[functionsNodeIndex].ChildNodes)
                {
                    EmitFunction(childNode);
                }
            }
            file.GscFunctions           = CompiledPub.Count;
            file.NumOfFunctions         = (ushort)Functions.Count;

            WriteFunctionsStructToFile();
            file.ExternalFunctions = CompiledPub.Count;
            file.NumOfExternalFunctions = (ushort)Calls.Count;
            WriteCallStackToFile();
            
            file.RefStrings = CompiledPub.Count;
            file.NumofRefStrings = (ushort)RefStrings.Count;
            WriteRefsToStringsToFile();
            file.Size = CompiledPub.Count;
            file.Size2 = CompiledPub.Count;

            file.Header = new byte[] { 0x80, 0x47, 0x53, 0x43, 0x0D, 0x0A, 0x00, 0x06 };
            file.Name = 0x40;

            Directory.CreateDirectory(Path.GetDirectoryName(Filename) + @"\Compiled\");
            using (var writer = File.Create(Path.GetDirectoryName(Filename) + @"\Compiled\" + Path.GetFileName(Filename)))
            {
                writer.Write(CompiledPub.ToArray(), 0, CompiledPub.Count);
            }
            return true;
        }

        private void EmitInclude(string include)
        {
            AddInt(GetStringPosByName(include));
        }

        private void EmitFunction(ParseTreeNode functionNode)
        {
            LocalVariables.Clear();
            numofParams = 0;
            foreach (ParseTreeNode parameterNode in functionNode.ChildNodes[1].ChildNodes[0].ChildNodes)
            {
                ParseParameter(parameterNode);
            }
            //PrepareLocalVariables(functionNode.ChildNodes[2]);
            var function = new Function();
            function.Start = CompiledPub.Count;
            function.NamePtr = GetStringPosByName(functionNode.ChildNodes[0].Token.ValueString);
            function.NumofParameters = numofParams;
            if (LocalVariables.Count > 0)
            {
                //EmitLocalVariables();
            }
            else
            {
                EmitOpcode(OP_checkclearparams);
            }
            ScriptCompile(functionNode.ChildNodes[2]);
            EmitOpcode(OP_End);
            Functions.Add(function);
            EmitCrc32();
            var alignedPos = (int)(CompiledPub.Count + 3 & 0xFFFFFFFE);
            while (CompiledPub.Count < alignedPos - 2)
            {
                CompiledPub.Add(0);
            }
        }

        private void ParseParameter(ParseTreeNode Node)
        {
            if (Node.Term.Name == "identifier")
            {
                LocalVariables.Add(Node.FindTokenAndGetValue().ToLower());
                numofParams++;
            }
            else
            {
                foreach (ParseTreeNode child in Node.ChildNodes)
                {
                    ParseParameter(child);
                }
            }
        }

        private void EmitCrc32()
        {
            var crc32 = new Crc32();
            int start = Functions[Functions.Count - 1].Start;
            crc32.AddData(start, CompiledPub.Count - start);
            Functions[Functions.Count - 1].Crc32 = crc32.GetCrc32();
        }

        

        /*--------------------------------*/
        private void ScriptCompile(ParseTreeNode Node, bool _ref = false, bool waitTillVar = false)
        {
            switch (Node.Term.Name)
            {
                case "block":
                    if (Node.ChildNodes.Count > 0)
                        ScriptCompile(Node.ChildNodes[0]); // needed
                    break;

                case "blockContent":
                    foreach (ParseTreeNode childNode in Node.ChildNodes[0].ChildNodes)
                        ScriptCompile(childNode.ChildNodes[0]); // needed
                    break;

                case "simpleCall":
                    EmitCall(Node.ChildNodes[0].ChildNodes[0], true); // needed
                    break;

                case "stringLiteral":
                    EmitGetString(Node.Token.ValueString, false); // needed
                    break;

                case "expr": // needed
                case "booleanExpression": 
                    EmitBooleanExpr(Node); // needed
                    break;
            }
        }
        private void EmitBooleanExpr(ParseTreeNode node)
        {
            ScriptCompile(node.ChildNodes[0]); // needed
        }
        private void EmitGetString(string str, bool isString) // needed
        {
            EmitOpcode(!isString ? OP_GetString : OP_GetIString);
            SetAlignedWord();
            AddRefToString(str, 0);
            AddUshort(GetStringPosByName(str));
        }
        private void EmitCall(ParseTreeNode callNode, bool decTop)
        {
          
            int baseCallNodeIndex = callNode.ChildNodes.FindIndex(e => e.Term.Name == "baseCall");
            int parenParamsNodeIndex = callNode.ChildNodes[baseCallNodeIndex].ChildNodes.FindIndex(e => e.Term.Name == "parenParameters");
            int functionNameNodeIndex = callNode.ChildNodes[baseCallNodeIndex].ChildNodes.FindIndex(e => e.Term.Name == "identifier");

            
            EmitOpcode(OP_PreScriptCall);
            ParseTreeNode parametersNode = null;

            if (callNode.ChildNodes[baseCallNodeIndex].ChildNodes[parenParamsNodeIndex].ChildNodes.Count > 0)
            {
                parametersNode =
                    callNode.ChildNodes[baseCallNodeIndex].ChildNodes[parenParamsNodeIndex].ChildNodes[0];
                parametersNode.ChildNodes.Reverse();
                foreach (ParseTreeNode childNode in parametersNode.ChildNodes)
                {
                    ScriptCompile(childNode);
                }
            }

            byte flag = 0;
            switch (callNode.Term.Name)
            {
                case "scriptFunctionCall":
                    EmitOpcode(OP_ScriptFunctionCall);
                    flag = 2;
                    break;
            }

            string name = callNode.ChildNodes[baseCallNodeIndex].ChildNodes[functionNameNodeIndex].Token.ValueString;
            byte numofParams = parametersNode != null ? (byte)GetNumOfParams(parametersNode) : (byte)0;
            ushort ptrToInclude = 0x3E;

            if (callNode.ChildNodes[baseCallNodeIndex].ChildNodes[0].Term.Name == "gscForFunction")
            {
                ptrToInclude =
                    GetStringPosByName(callNode.ChildNodes[baseCallNodeIndex].ChildNodes[0].FindTokenAndGetValue());
            }

            AddRefToCall(name, numofParams, flag, ptrToInclude);
            SetAlignedDword(1);
            AddInt(GetStringPosByName(name));
            if (decTop)
            {
                EmitOpcode(OP_DecTop);
            }
        }
        private int GetNumOfParams(ParseTreeNode node)
        {
            int count = 0;
            foreach (ParseTreeNode parameterNode in node.ChildNodes)
            {
                if (parameterNode.Term.Name != "expr" && parameterNode.Term.Name != "expr+")
                    count++;
                else
                    count += GetNumOfParams(parameterNode);
            }
            return count;
        }
        private void EmitOpcode(byte opcode)
        {
            CompiledPub.Add(opcode);
        }
        private ushort GetStringPosByName(string str)
        {
            return
                (from globalString in GlobalStrings where globalString.Str == str select globalString.Position)
                    .FirstOrDefault();
        }
        private bool StringShouldBeWritten(string str)
        {
            return GlobalStrings.All(globalString => globalString.Str != str && !IsObjectOwnerOrBuiltIn(str));
        }
        private bool IsObjectOwnerOrBuiltIn(string str)
        {
            return false;
        }
        private void PrepareStrings(ParseTreeNode node)
        {
            foreach (ParseTreeNode childNode in node.ChildNodes)
            {
                switch (childNode.Term.Name)
                {
                    case "identifier":
                        childNode.Token.Value = childNode.Token.ValueString.ToLower().Replace(@"\", "/");
                        if (!StringShouldBeWritten(childNode.Token.ValueString)) break;
                        AddString(childNode.Token.ValueString);
                        break;

                    case "stringLiteral":
                        if (!StringShouldBeWritten(childNode.Token.ValueString)) break;
                        AddString(childNode.Token.ValueString);
                        break;

                    default:
                        PrepareStrings(childNode);
                        break;
                }
            }
        }
        /*--------------------------------*/

       

        public class Call
        {
            public ushort NamePtr { get; set; }
            public ushort IncludePtr { get; set; }
            public ushort NumOfRefs { get; set; }
            public byte NumOfParams { get; set; }
            public byte Flag { get; set; }
            public List<int> Refs { get; set; }
        }
        public class Function
        {
            public uint Crc32 { get; set; }
            public int Start { get; set; }
            public ushort NamePtr { get; set; }
            public byte NumofParameters { get; set; }
            public byte Flag { get; set; }
        }
        public class GlobalString
        {
            public string Str { get; set; }
            public ushort Position { get; set; }
        }
        public class RefString
        {
            public List<int> Refs = new List<int>();
            public ushort NamePtr { get; set; }
            public byte NumOfRefs { get; set; }
            public byte Type { get; set; }
        }
    }
}
