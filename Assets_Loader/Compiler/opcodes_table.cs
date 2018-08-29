namespace Compiler
{
    public partial class ScriptCompiler
    {
        public const byte OP_End = 0;
        public const byte OP_GetString = 0xA;
        public const byte OP_GetIString = 0xB;
        public const byte OP_checkclearparams = 0x26;
        public const byte OP_PreScriptCall = 0x2D;
        public const byte OP_ScriptFunctionCall = 0x2E;
        public const byte OP_DecTop = 0x36;
    }
}