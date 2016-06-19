using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommandLine;
using SInnovations.VSTeamServices.TasksBuilder.Attributes;
using SInnovations.VSTeamServices.TasksBuilder.ConsoleUtils;

namespace SInnovations.VSTeamServices.TasksBuilder.ResourceTypes
{
    enum TokenKind
    {
        Wildcard = 0,               // *
        CharacterWildcard = 1,      // ?
        DirectoryWildcard = 2,      // **

        CharacterSetStart = 3,     // [
        CharacterSetEnd = 4,       // ]

        LiteralSetStart = 5,       // {
        LiteralSetSeperator = 6,   // ,
        LiteralSetEnd = 7,         // }

        PathSeperator = 8,          // / \

        Identifier = 9,              // Letter or Number

        WindowsRoot = 10,           // :

        EOT = 100,
    }
    class Token
    {
        public Token(TokenKind kind, string spelling)
        {
            this.Kind = kind;
            this.Spelling = spelling;
        }

        public TokenKind Kind { get; private set; }
        public string Spelling { get; private set; }
    }
    class Scanner
    {
        private readonly string _source;

        private int _sourceIndex;
        private char? _currentCharacter;
        private readonly StringBuilder _currentSpelling;
        private TokenKind _currentKind;

        public Scanner(string source)
        {
            this._source = source;
            this._sourceIndex = 0;
            this._currentSpelling = new StringBuilder();
            SetCurrentCharacter();
        }

        public void Take(char character)
        {
            if (this._currentCharacter == character)
            {
                this.TakeIt();
                return;
            }

            throw new InvalidDataException();
        }

        public void TakeIt()
        {
            this._currentSpelling.Append(this._currentCharacter);
            this._sourceIndex++;

            SetCurrentCharacter();
        }

        private void SetCurrentCharacter()
        {
            if (this._sourceIndex >= this._source.Length)
                this._currentCharacter = null;
            else
                this._currentCharacter = this._source[this._sourceIndex];
        }

        public Token Peek()
        {
            var index = this._sourceIndex;
            var token = this.Scan();
            this._sourceIndex = index;
            SetCurrentCharacter();
            return token;
        }

        public Token Scan()
        {
            this._currentSpelling.Clear();
            this._currentKind = this.ScanToken();

            return new Token(this._currentKind, this._currentSpelling.ToString());
        }

        private TokenKind ScanToken()
        {
            if (IsAlphaNumeric(this._currentCharacter))
            {
                while (IsAlphaNumeric(this._currentCharacter))
                {
                    this.TakeIt();
                }

                return TokenKind.Identifier;
            }

            switch (this._currentCharacter)
            {
                case '*':
                    this.TakeIt();
                    if (this._currentCharacter == '*')
                    {
                        this.TakeIt();
                        return TokenKind.DirectoryWildcard;
                    }

                    return TokenKind.Wildcard;
                case '?':
                    this.TakeIt();
                    return TokenKind.CharacterWildcard;

                case '[':
                    this.TakeIt();
                    return TokenKind.CharacterSetStart;

                case ']':
                    this.TakeIt();
                    return TokenKind.CharacterSetEnd;

                case '{':
                    this.TakeIt();
                    return TokenKind.LiteralSetStart;

                case ',':
                    this.TakeIt();
                    return TokenKind.LiteralSetSeperator;


                case '}':
                    this.TakeIt();
                    return TokenKind.LiteralSetEnd;

                case '/':
                case '\\':
                    this.TakeIt();
                    return TokenKind.PathSeperator;

                case ':':
                    this.TakeIt();
                    return TokenKind.WindowsRoot;

                case null:
                    return TokenKind.EOT;

                default:
                    throw new Exception("Unable to scan for next token. Stuck on '" + this._currentCharacter + "'");
            }
        }

        private static bool IsAlphaNumeric(char? c)
        {
            return c != null && (char.IsLetterOrDigit(c.Value) || c.Value == '-' || c == '.' || c==' ' || c=='_');
        }
    }
    class GlobParser
    {
        private Scanner _scanner;
        private Token _currentToken;

        public GlobParser(string pattern = null)
        {
            if (!string.IsNullOrEmpty(pattern))
                this._scanner = new Scanner(pattern);
        }

        private void Accept(TokenKind expectedKind)
        {
            if (this._currentToken.Kind == expectedKind)
            {
                this.AcceptIt();
                return;
            }

            throw new Exception("Parser error Unexpected TokenKind detected.");
        }

        private void AcceptIt()
        {
            if (this._scanner == null)
            {
                throw new Exception("No source text was provided");
            }
            this._currentToken = this._scanner.Scan();
        }

        private GlobNode ParseIdentifier()
        {
            if (this._currentToken.Kind == TokenKind.Identifier)
            {
                var identifier = new GlobNode(GlobNodeType.Identifier, this._currentToken.Spelling);
                this.AcceptIt();
                return identifier;
            }

            throw new Exception("Unable to parse Identifier");
        }

        private GlobNode ParseLiteralSet()
        {
            var items = new List<GlobNode>();
            this.Accept(TokenKind.LiteralSetStart);
            items.Add(this.ParseIdentifier());

            while (this._currentToken.Kind == TokenKind.LiteralSetSeperator)
            {
                this.AcceptIt();
                items.Add(this.ParseIdentifier());
            }
            this.Accept(TokenKind.LiteralSetEnd);
            return new GlobNode(GlobNodeType.LiteralSet, items);
        }

        private GlobNode ParseCharacterSet()
        {
            this.Accept(TokenKind.CharacterSetStart);
            var characterSet = this.ParseIdentifier();
            this.Accept(TokenKind.CharacterSetEnd);
            return new GlobNode(GlobNodeType.CharacterSet, characterSet);
        }

        private GlobNode ParseWildcard()
        {
            this.Accept(TokenKind.Wildcard);
            return new GlobNode(GlobNodeType.WildcardString);
        }

        private GlobNode ParseCharacterWildcard()
        {
            this.Accept(TokenKind.CharacterWildcard);
            return new GlobNode(GlobNodeType.CharacterWildcard);
        }

        private GlobNode ParseSubSegment()
        {
            switch (this._currentToken.Kind)
            {
                case TokenKind.Identifier:
                    return this.ParseIdentifier();
                case TokenKind.CharacterSetStart:
                    return this.ParseCharacterSet();
                case TokenKind.LiteralSetStart:
                    return this.ParseLiteralSet();
                case TokenKind.CharacterWildcard:
                    return this.ParseCharacterWildcard();
                case TokenKind.Wildcard:
                    return this.ParseWildcard();
                default:
                    throw new Exception("Unable to parse PathSubSegment");
            }
        }

        private GlobNode ParseSegment()
        {
            if (this._currentToken.Kind == TokenKind.DirectoryWildcard)
            {
                this.AcceptIt();
                return new GlobNode(GlobNodeType.DirectoryWildcard);
            }

            return ParsePathSegment();
        }

        private GlobNode ParsePathSegment()
        {
            var items = new List<GlobNode>();
            while (true)
            {
                switch (this._currentToken.Kind)
                {
                    case TokenKind.Identifier:
                    case TokenKind.CharacterSetStart:
                    case TokenKind.LiteralSetStart:
                    case TokenKind.CharacterWildcard:
                    case TokenKind.Wildcard:
                        items.Add(this.ParseSubSegment());
                        continue;
                    default:
                        break;
                }
                break;
            }

            return new GlobNode(GlobNodeType.PathSegment, items);
        }

        private GlobNode ParseRoot()
        {
            if (this._currentToken.Kind == TokenKind.PathSeperator)
                return new GlobNode(GlobNodeType.Root); //dont eat it so we can leave it for the segments


            if (this._currentToken.Kind == TokenKind.Identifier &&
               this._currentToken.Spelling.Length == 1 &&
               this._scanner.Peek().Kind == TokenKind.WindowsRoot)
            {
                var ident = this.ParseIdentifier();
                this.Accept(TokenKind.WindowsRoot);
                return new GlobNode(GlobNodeType.Root, ident);
            }

            return new GlobNode(GlobNodeType.Root, Directory.GetCurrentDirectory());
        }

        private GlobNode ParseTree()
        {
            var items = new List<GlobNode>();

            items.Add(this.ParseRoot());

            while (this._currentToken.Kind == TokenKind.PathSeperator)
            {
                this.AcceptIt();
                items.Add(this.ParseSegment());
            }

            return new GlobNode(GlobNodeType.Tree, items);
        }

        public GlobNode Parse(string text = null)
        {
            if (text != null)
                this._scanner = new Scanner(text);

            this.AcceptIt();
            var path = this.ParseTree();
            if (this._currentToken.Kind != TokenKind.EOT)
            {
                throw new Exception("Expected EOT");
            }

            return path;
        }
    }
    class GlobNode
    {

        public GlobNode(GlobNodeType type, IEnumerable<GlobNode> children)
        {
            this.Type = type;
            this.Text = null;
            this.Children = new List<GlobNode>(children);
        }

        public GlobNode(GlobNodeType type)
        {
            this.Type = type;
            this.Text = null;
            this.Children = new List<GlobNode>();
        }


        public GlobNode(GlobNodeType type, GlobNode child)
        {
            this.Type = type;
            this.Text = null;
            this.Children = new List<GlobNode> { child };
        }

        public GlobNode(GlobNodeType type, string text)
        {
            this.Type = type;
            this.Text = text;
            this.Children = new List<GlobNode>();
        }

        public string Text { get; private set; }

        public GlobNodeType Type { get; private set; }

        public List<GlobNode> Children { get; private set; }
    }

    enum GlobNodeType
    {
        CharacterSet, //string, no children
        Tree, // children
        Identifier, //string
        LiteralSet, //children
        PathSegment, //children
        Root, //string 
        WildcardString, //string
        CharacterWildcard, //string
        DirectoryWildcard,
    }
    class GlobToRegexVisitor
    {
        public static string Process(GlobNode node)
        {
            if (node.Type != GlobNodeType.Tree)
                throw new InvalidOperationException();

            return ProcessTree(node);
        }

        private void Assert(GlobNode node, GlobNodeType type)
        {
            if (node.Type != type)
                throw new InvalidOperationException();
        }

        private static string ProcessTree(GlobNode node)
        {
            return string.Join("/", node.Children.Select(ProcessSegment));
        }

        private static string ProcessSegment(GlobNode node)
        {
            switch (node.Type)
            {
                case GlobNodeType.Root:
                    return ProcessRoot(node);
                case GlobNodeType.DirectoryWildcard:
                    return ProcessDirectoryWildcard(node);
                case GlobNodeType.PathSegment:
                    return ProcessPathSegment(node);
                default:
                    throw new InvalidOperationException();
            }
        }

        private static string ProcessPathSegment(GlobNode node)
        {
            return string.Join("", node.Children.Select(ProcessSubSegment));
        }

        private static string ProcessSubSegment(GlobNode node)
        {
            switch (node.Type)
            {
                case GlobNodeType.Identifier:
                    return ProcessIdentifier(node);
                case GlobNodeType.CharacterSet:
                    return ProcessCharacterSet(node);
                case GlobNodeType.LiteralSet:
                    return ProcessLiteralSet(node);
                case GlobNodeType.CharacterWildcard:
                    return ProcessCharacterWildcard(node);
                case GlobNodeType.WildcardString:
                    return ProcessWildcardString(node);
            }
            throw new NotImplementedException();
        }

        private static string ProcessWildcardString(GlobNode node)
        {
            return @"[^/]*";
        }

        private static string ProcessCharacterWildcard(GlobNode node)
        {
            return @"[^/]{1}";
        }

        private static string ProcessLiteralSet(GlobNode node)
        {
            return "(" + string.Join(",", node.Children.Select(ProcessIdentifier)) + ")";
        }

        private static string ProcessCharacterSet(GlobNode node)
        {
            return "[" + ProcessIdentifier(node.Children.First()) + "]";
        }

        private static string ProcessIdentifier(GlobNode node)
        {
            return Regex.Escape(node.Text);
        }

        private static string ProcessDirectoryWildcard(GlobNode node)
        {
            return ".*";
        }

        private static string ProcessRoot(GlobNode node)
        {
            if (node.Children.Count > 0) //windows root
                return Regex.Escape(node.Children[0].Text + ":");

            if (!string.IsNullOrEmpty(node.Text)) // CWD
                return Regex.Escape(node.Text);


            return string.Empty;
        }
    }
    class Glob
    {
        public string Pattern { get; private set; }

        private GlobNode _root;
        private Regex _regex;

        public Glob(string pattern)//, GlobOptions options = GlobOptions.None)
        {
            this.Pattern = pattern;
            //if (options.HasFlag(GlobOptions.Compiled))
            //{
                this.Compile();
            //}
        }

        private void Compile()
        {
            if (_root != null)
                return;

            var parser = new GlobParser(this.Pattern);
            _root = parser.Parse();

            //TODO: this is basically cheating and probably not efficient but it works for now.
            _regex = new Regex(GlobToRegexVisitor.Process(_root) +(this.Pattern.EndsWith("*") ? "" : "$"), RegexOptions.Compiled);
        }

        public bool IsMatch(string input)
        {
            this.Compile();

            return _regex.IsMatch(input);
        }

        public static bool IsMatch(string input, string pattern)
        {
            return new Glob(pattern).IsMatch(input);
        }
    }

    [ResourceType( TaskInputType = "filePath")]
    public class GlobPath : IConsoleReader
    {

        public static string ResourceType = typeof(GlobPath).ToString();
        public GlobPath()
        {
        }
        public GlobPath(string pattern)
        {
            Pattern = pattern;
        }

        public string Pattern { get; private set; }


        public string Root { get; private set; }
        public IEnumerable<string> MatchedFiles()
        {
            if (string.IsNullOrEmpty(Pattern))
            {
                return new string[] { };
            }

            if (!Pattern.Contains("*"))
            {
                return new string[] { Pattern };
            }

            var root = Directory.GetCurrentDirectory();
            if (Path.IsPathRooted(Pattern))
            {
                
                var firstWild = Pattern.IndexOf('*');
                var firstDirBeforeWold = Pattern.Replace('\\', '/').LastIndexOf('/', firstWild);
                root = new DirectoryInfo(Pattern.Substring(0, firstDirBeforeWold)).FullName;
                Pattern = root + Pattern.Substring(firstDirBeforeWold);
            }
            var glob = new Glob(Pattern);
            Root = root;
            return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Where(directory => glob.IsMatch(directory.Replace('\\','/')));

        }



        public void OnConsoleParsing(Parser parser, string[] args, object options, PropertyInfo info)
        {
           
            var idx = Array.IndexOf(args, $"--{info.GetCustomAttribute<OptionAttribute>()?.LongName?? info.GetCustomAttribute<DisplayAttribute>()?.ShortName}");
            if (idx != -1)
            {
                Pattern = args[idx + 1];
            }

            if(info.PropertyType == typeof(string))
            {
                info.SetValue(options, this.MatchedFiles().FirstOrDefault());
            }
        }
    }
}
