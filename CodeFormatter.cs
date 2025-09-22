using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RawrZDesktop
{
    /// <summary>
    /// Provides advanced code formatting capabilities for the RawrZ IDE
    /// </summary>
    public static class CodeFormatter
    {
        public static string FormatCode(string code)
        {
            try
            {
                // Use basic formatting since Microsoft.CodeAnalysis.Formatting is not available
                return BasicFormat(code);
            }
            catch (Exception)
            {
                // If formatting fails, return original code
                return code;
            }
        }

        public static string BasicFormat(string code)
        {
            var lines = code.Split('\n');
            var formattedLines = new List<string>();
            int indentLevel = 0;
            bool inMultiLineComment = false;
            bool inString = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // Skip empty lines but preserve them
                if (string.IsNullOrWhiteSpace(trimmedLine))
                {
                    formattedLines.Add(string.Empty);
                    continue;
                }

                // Handle multi-line comments
                if (trimmedLine.Contains("/*"))
                    inMultiLineComment = true;
                if (trimmedLine.Contains("*/"))
                    inMultiLineComment = false;

                // Don't format inside comments or strings
                if (inMultiLineComment || trimmedLine.StartsWith("//"))
                {
                    formattedLines.Add(new string(' ', Math.Max(0, indentLevel * 4)) + trimmedLine);
                    continue;
                }

                // Handle string literals
                var charArray = trimmedLine.ToCharArray();
                for (int i = 0; i < charArray.Length; i++)
                {
                    if (charArray[i] == '"' && (i == 0 || charArray[i - 1] != '\\'))
                        inString = !inString;
                }

                if (inString)
                {
                    formattedLines.Add(new string(' ', Math.Max(0, indentLevel * 4)) + trimmedLine);
                    continue;
                }

                // Decrease indent for closing braces
                if (trimmedLine.Contains("}") && !trimmedLine.Contains("{"))
                {
                    indentLevel = Math.Max(0, indentLevel - 1);
                }

                // Add the formatted line
                var formattedLine = new string(' ', indentLevel * 4) + trimmedLine;
                
                // Add proper spacing around operators and keywords
                formattedLine = AddSpacing(formattedLine);
                
                formattedLines.Add(formattedLine);

                // Increase indent for opening braces
                if (trimmedLine.Contains("{") && !trimmedLine.Contains("}"))
                {
                    indentLevel++;
                }
                
                // Handle special cases like switch statements
                if (trimmedLine.Contains("case ") || trimmedLine.Contains("default:"))
                {
                    if (!trimmedLine.Contains("{"))
                        indentLevel++;
                }
                else if (trimmedLine.Contains("break;") || trimmedLine.Contains("return;"))
                {
                    if (indentLevel > 0)
                        indentLevel--;
                }
            }

            return string.Join("\n", formattedLines);
        }

        private static string AddSpacing(string line)
        {
            var result = line;

            // Add space around operators
            var operators = new[] { "=", "+", "-", "*", "/", "%", "==", "!=", "<=", ">=", "<", ">", "&&", "||" };
            foreach (var op in operators)
            {
                result = result.Replace(op, $" {op} ");
            }

            // Clean up multiple spaces
            while (result.Contains("  "))
            {
                result = result.Replace("  ", " ");
            }

            // Fix spacing for method calls and arrays
            result = result.Replace(" (", "(");
            result = result.Replace(" [", "[");
            result = result.Replace("( ", "(");
            result = result.Replace("[ ", "[");

            return result;
        }

        public static string OptimizeUsings(string code)
        {
            try
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var root = syntaxTree.GetRoot();

                // Get all using directives
                var usingDirectives = root.DescendantNodes()
                    .OfType<UsingDirectiveSyntax>()
                    .ToList();

                // Remove duplicates and sort
                var uniqueUsings = usingDirectives
                    .Select(u => u.Name?.ToString())
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Distinct()
                    .OrderBy(name => name)
                    .ToList();

                // Find referenced types in the code
                var referencedTypes = GetReferencedTypes(root);
                
                // Filter usings to only include those that are actually used
                var usedUsings = uniqueUsings
                    .Where(usingName => IsUsingUsed(usingName!, referencedTypes))
                    .ToList();

                // Rebuild the code with optimized usings
                var codeWithoutUsings = RemoveUsings(code);
                var optimizedUsings = string.Join("\n", usedUsings.Select(u => $"using {u};"));
                
                return string.IsNullOrEmpty(optimizedUsings) ? codeWithoutUsings : optimizedUsings + "\n\n" + codeWithoutUsings;
            }
            catch (Exception)
            {
                return code; // Return original code if optimization fails
            }
        }

        private static HashSet<string> GetReferencedTypes(SyntaxNode root)
        {
            var types = new HashSet<string>();

            // Get all identifier names
            var identifiers = root.DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Select(i => i.Identifier.ValueText);

            types.UnionWith(identifiers);

            // Get all qualified names
            var qualifiedNames = root.DescendantNodes()
                .OfType<QualifiedNameSyntax>()
                .Select(q => q.ToString());

            types.UnionWith(qualifiedNames);

            return types;
        }

        private static bool IsUsingUsed(string usingName, HashSet<string> referencedTypes)
        {
            // Simple heuristic to check if a using is used
            var namespaceParts = usingName.Split('.');
            
            foreach (var part in namespaceParts)
            {
                if (referencedTypes.Contains(part))
                    return true;
            }

            // Common system namespaces that are often used implicitly
            var commonNamespaces = new[]
            {
                "System",
                "System.Collections.Generic",
                "System.Linq",
                "System.Text",
                "System.Threading.Tasks"
            };

            return commonNamespaces.Contains(usingName);
        }

        private static string RemoveUsings(string code)
        {
            var lines = code.Split('\n');
            var result = new List<string>();
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (!trimmedLine.StartsWith("using ") || trimmedLine.StartsWith("using ("))
                {
                    result.Add(line);
                }
            }

            // Remove empty lines at the beginning
            while (result.Count > 0 && string.IsNullOrWhiteSpace(result[0]))
            {
                result.RemoveAt(0);
            }

            return string.Join("\n", result);
        }

        public static string AddRegions(string code)
        {
            try
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var root = syntaxTree.GetRoot();

                var result = new StringBuilder();
                var lines = code.Split('\n');

                bool hasFields = root.DescendantNodes().OfType<FieldDeclarationSyntax>().Any();
                bool hasProperties = root.DescendantNodes().OfType<PropertyDeclarationSyntax>().Any();
                bool hasConstructors = root.DescendantNodes().OfType<ConstructorDeclarationSyntax>().Any();
                bool hasMethods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Any();

                bool inClass = false;
                bool fieldsRegionAdded = false;
                bool propertiesRegionAdded = false;
                bool constructorsRegionAdded = false;
                bool methodsRegionAdded = false;

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();

                    if (trimmedLine.Contains("class ") && trimmedLine.Contains("{"))
                    {
                        inClass = true;
                        result.AppendLine(line);
                        continue;
                    }

                    if (inClass && trimmedLine.StartsWith("}") && !trimmedLine.Contains("{"))
                    {
                        // Close any open regions before class closing
                        if (methodsRegionAdded || constructorsRegionAdded || propertiesRegionAdded || fieldsRegionAdded)
                        {
                            result.AppendLine("        #endregion");
                        }
                        inClass = false;
                    }

                    if (inClass)
                    {
                        // Add regions for different member types
                        if (hasFields && !fieldsRegionAdded && (trimmedLine.Contains("private ") || trimmedLine.Contains("protected ") || trimmedLine.Contains("public ")) && !trimmedLine.Contains("("))
                        {
                            result.AppendLine("        #region Fields");
                            fieldsRegionAdded = true;
                        }
                        else if (hasProperties && !propertiesRegionAdded && trimmedLine.Contains("{ get") || trimmedLine.Contains("{ set"))
                        {
                            if (fieldsRegionAdded)
                            {
                                result.AppendLine("        #endregion");
                            }
                            result.AppendLine("        #region Properties");
                            propertiesRegionAdded = true;
                        }
                        else if (hasConstructors && !constructorsRegionAdded && trimmedLine.Contains("public ") && trimmedLine.Contains("(") && !trimmedLine.Contains("void"))
                        {
                            if (propertiesRegionAdded || fieldsRegionAdded)
                            {
                                result.AppendLine("        #endregion");
                            }
                            result.AppendLine("        #region Constructors");
                            constructorsRegionAdded = true;
                        }
                        else if (hasMethods && !methodsRegionAdded && (trimmedLine.Contains("void ") || trimmedLine.Contains("string ") || trimmedLine.Contains("int ")) && trimmedLine.Contains("("))
                        {
                            if (constructorsRegionAdded || propertiesRegionAdded || fieldsRegionAdded)
                            {
                                result.AppendLine("        #endregion");
                            }
                            result.AppendLine("        #region Methods");
                            methodsRegionAdded = true;
                        }
                    }

                    result.AppendLine(line);
                }

                return result.ToString();
            }
            catch (Exception)
            {
                return code; // Return original code if region addition fails
            }
        }

        public static string RemoveRegions(string code)
        {
            var lines = code.Split('\n');
            var result = new List<string>();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (!trimmedLine.StartsWith("#region") && !trimmedLine.StartsWith("#endregion"))
                {
                    result.Add(line);
                }
            }

            return string.Join("\n", result);
        }
    }
}