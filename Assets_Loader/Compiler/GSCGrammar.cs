using Irony.Parsing;

namespace Compiler
{
    /// <summary>
    ///     The grammar of the GameScript language.
    /// </summary>
    [Language("GameScript", "1.0", "GameScript grammar for the Call of Duty: Black Ops 2")]
    public class GSCGrammar : Grammar
    {
        public GSCGrammar()
        {
            #region Lexical structure

            //Comments
            var blockComment = new CommentTerminal("block-comment", "/*", "*/");
            var lineComment = new CommentTerminal("line-comment", "//",
                "\r", "\n", "\u2085", "\u2028", "\u2029");
            NonGrammarTerminals.Add(blockComment);
            NonGrammarTerminals.Add(lineComment);

            //Literals
            var numberLiteral = new NumberLiteral("numberLiteral", NumberOptions.AllowSign);
            var stringLiteral = new StringLiteral("stringLiteral", "\"");
            var identifier = new IdentifierTerminal("identifier", @"_/\", "_");

            MarkPunctuation("(", ")", "{", "}", "[", "]", ",", ".", ";", "::", "[[", "]]", "#include", "#using_animtree");

            RegisterOperators(1, "+", "-");
            RegisterOperators(2, "*", "/", "%");
            RegisterOperators(3, "|", "&", "^");
            RegisterOperators(4, "&&", "||");
            RegisterBracePair("(", ")");

            #endregion

            var program = new NonTerminal("program");
            var functions = new NonTerminal("functions");
            var function = new NonTerminal("function");
            var declarations = new NonTerminal("declarations");
            var declaration = new NonTerminal("declaration");
            var baseCall = new NonTerminal("baseCall");
            var scriptFunctionCall = new NonTerminal("scriptFunctionCall");
            var call = new NonTerminal("call");
            var simpleCall = new NonTerminal("simpleCall");
            var parenParameters = new NonTerminal("parenParameters");
            var parameters = new NonTerminal("parameters");
            var expr = new NonTerminal("expr");
            var expression = new NonTerminal("expression");
            var isString = new NonTerminal("isString");
            var block = new NonTerminal("block");
            var blockContent = new NonTerminal("blockContent");
            var parenExpr = new NonTerminal("parenExpr");


            program.Rule =  functions | functions | functions;
            functions.Rule = MakePlusRule(functions, function);
            function.Rule = identifier + parenParameters + block;
            declarations.Rule = MakePlusRule(declarations, declaration);
            declaration.Rule = simpleCall;
            block.Rule = ToTerm("{") + blockContent + "}" | ToTerm("{") + "}";
            blockContent.Rule = declarations;
            parenExpr.Rule = ToTerm("(") + expr + ")";
            expr.Rule =  call | identifier | stringLiteral | numberLiteral | expression | isString | parenExpr;
            parameters.Rule = MakeStarRule(parameters, ToTerm(","), expr) | expr;
            parenParameters.Rule = ToTerm("(") + parameters + ")" | "(" + ")";
            expression.Rule = expr +  expr | "(" + expr + expr + ")";
            isString.Rule = ToTerm("&") + stringLiteral;
            baseCall.Rule = identifier + parenParameters | identifier + parenParameters;
            scriptFunctionCall.Rule = baseCall;

            call.Rule = scriptFunctionCall;
            simpleCall.Rule = call + ";";

            
           
           
           
         

            
        
          
            Root = program;
        }
    }
}
