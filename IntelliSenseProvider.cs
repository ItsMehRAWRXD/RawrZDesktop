using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RawrZDesktop
{
    /// <summary>
    /// Provides IntelliSense and code completion functionality for the RawrZ IDE
    /// </summary>
    public class IntelliSenseProvider
    {
        private readonly List<string> csharpKeywords;
        private readonly List<string> systemTypes;
        private readonly List<string> commonMethods;

        public IntelliSenseProvider()
        {
            csharpKeywords = new List<string>
            {
                "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char",
                "checked", "class", "const", "continue", "decimal", "default", "delegate",
                "do", "double", "else", "enum", "event", "explicit", "extern", "false",
                "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit",
                "in", "int", "interface", "internal", "is", "lock", "long", "namespace",
                "new", "null", "object", "operator", "out", "override", "params", "private",
                "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
                "short", "sizeof", "stackalloc", "static", "string", "struct", "switch",
                "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked",
                "unsafe", "ushort", "using", "virtual", "void", "volatile", "while",
                "async", "await", "var", "dynamic", "get", "set", "value", "yield"
            };

            systemTypes = new List<string>
            {
                "Console", "String", "Int32", "Double", "Boolean", "DateTime", "TimeSpan",
                "List<T>", "Dictionary<TKey,TValue>", "Array", "Exception", "Task", "Task<T>",
                "IEnumerable<T>", "IList<T>", "StringBuilder", "Regex", "File", "Directory",
                "Path", "Stream", "StreamReader", "StreamWriter", "HttpClient", "Uri"
            };

            commonMethods = new List<string>
            {
                "WriteLine", "Write", "ReadLine", "ReadKey", "ToString", "Parse", "TryParse",
                "Add", "Remove", "Contains", "Count", "Length", "Substring", "Replace",
                "Split", "Join", "Trim", "ToUpper", "ToLower", "StartsWith", "EndsWith",
                "IndexOf", "LastIndexOf", "Insert", "Format", "IsNullOrEmpty", "IsNullOrWhiteSpace"
            };
        }

        public List<string> GetCompletions(string code, int position)
        {
            var completions = new List<string>();

            try
            {
                // Get the current word being typed
                var currentWord = GetCurrentWord(code, position);
                
                // If we're typing after a dot, provide member completions
                if (position > 0 && code[position - 1] == '.')
                {
                    completions.AddRange(GetMemberCompletions(code, position));
                }
                else
                {
                    // Provide general completions
                    completions.AddRange(GetKeywordCompletions(currentWord));
                    completions.AddRange(GetTypeCompletions(currentWord));
                    completions.AddRange(GetVariableCompletions(code, currentWord));
                }

                return completions.Distinct().OrderBy(c => c).ToList();
            }
            catch (Exception)
            {
                // Return empty list on error
                return new List<string>();
            }
        }

        private string GetCurrentWord(string code, int position)
        {
            if (position <= 0 || position > code.Length)
                return string.Empty;

            var start = position - 1;
            while (start >= 0 && (char.IsLetterOrDigit(code[start]) || code[start] == '_'))
                start--;
            start++;

            var end = position;
            while (end < code.Length && (char.IsLetterOrDigit(code[end]) || code[end] == '_'))
                end++;

            return code.Substring(start, end - start);
        }

        private List<string> GetKeywordCompletions(string prefix)
        {
            return csharpKeywords
                .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private List<string> GetTypeCompletions(string prefix)
        {
            return systemTypes
                .Where(t => t.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private List<string> GetMemberCompletions(string code, int position)
        {
            // Simple member completion based on common types
            var precedingText = code.Substring(0, position - 1);
            
            if (precedingText.EndsWith("Console"))
            {
                return new List<string> { "WriteLine", "Write", "ReadLine", "ReadKey", "Clear", "Beep" };
            }
            else if (precedingText.EndsWith("string") || precedingText.Contains("\""))
            {
                return new List<string> { "Length", "Substring", "Replace", "Split", "Trim", "ToUpper", "ToLower", "Contains", "StartsWith", "EndsWith" };
            }
            else if (precedingText.Contains("List<") || precedingText.Contains("[]"))
            {
                return new List<string> { "Add", "Remove", "Contains", "Count", "Clear", "Insert", "RemoveAt", "IndexOf" };
            }
            else
            {
                return commonMethods;
            }
        }

        private List<string> GetVariableCompletions(string code, string prefix)
        {
            var variables = new List<string>();

            try
            {
                // Parse the code to find variable declarations
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var root = syntaxTree.GetRoot();

                // Find variable declarations
                var variableDeclarations = root.DescendantNodes()
                    .OfType<VariableDeclaratorSyntax>()
                    .Select(v => v.Identifier.ValueText)
                    .Where(name => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

                variables.AddRange(variableDeclarations);

                // Find parameter declarations
                var parameters = root.DescendantNodes()
                    .OfType<ParameterSyntax>()
                    .Select(p => p.Identifier.ValueText)
                    .Where(name => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

                variables.AddRange(parameters);

                // Find method declarations
                var methods = root.DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Select(m => m.Identifier.ValueText)
                    .Where(name => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

                variables.AddRange(methods);

                // Find class declarations
                var classes = root.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Select(c => c.Identifier.ValueText)
                    .Where(name => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

                variables.AddRange(classes);
            }
            catch (Exception)
            {
                // Ignore parsing errors
            }

            return variables.Distinct().ToList();
        }

        public List<string> GetSnippets()
        {
            return new List<string>
            {
                "class",
                "interface", 
                "method",
                "property",
                "constructor",
                "for",
                "foreach",
                "if",
                "try-catch",
                "using",
                "switch"
            };
        }

        public string ExpandSnippet(string snippetName)
        {
            return snippetName switch
            {
                "class" => @"public class ClassName
{
    public ClassName()
    {
        
    }
}",
                "interface" => @"public interface IInterfaceName
{
    void MethodName();
}",
                "method" => @"public void MethodName()
{
    
}",
                "property" => @"public string PropertyName { get; set; }",
                "constructor" => @"public ClassName()
{
    
}",
                "for" => @"for (int i = 0; i < length; i++)
{
    
}",
                "foreach" => @"foreach (var item in collection)
{
    
}",
                "if" => @"if (condition)
{
    
}",
                "try-catch" => @"try
{
    
}
catch (Exception ex)
{
    
}",
                "using" => @"using (var resource = new Resource())
{
    
}",
                "switch" => @"switch (variable)
{
    case value1:
        break;
    default:
        break;
}",
                _ => snippetName
            };
        }

        public List<DiagnosticInfo> GetDiagnostics(string code)
        {
            var diagnostics = new List<DiagnosticInfo>();

            try
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("temp")
                    .AddSyntaxTrees(syntaxTree);

                foreach (var diagnostic in compilation.GetDiagnostics())
                {
                    if (diagnostic.Severity == DiagnosticSeverity.Error || 
                        diagnostic.Severity == DiagnosticSeverity.Warning)
                    {
                        var lineSpan = diagnostic.Location.GetLineSpan();
                        diagnostics.Add(new DiagnosticInfo
                        {
                            Message = diagnostic.GetMessage(),
                            Line = lineSpan.StartLinePosition.Line + 1,
                            Column = lineSpan.StartLinePosition.Character + 1,
                            Severity = diagnostic.Severity.ToString()
                        });
                    }
                }
            }
            catch (Exception)
            {
                // Ignore parsing errors
            }

            return diagnostics;
        }
    }

    public class DiagnosticInfo
    {
        public string Message { get; set; } = string.Empty;
        public int Line { get; set; }
        public int Column { get; set; }
        public string Severity { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"Line {Line}, Column {Column}: {Severity} - {Message}";
        }
    }
}