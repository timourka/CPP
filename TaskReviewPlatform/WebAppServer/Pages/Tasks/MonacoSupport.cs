using System;
using System.Collections.Generic;
using System.Linq;

namespace WebAppServer.Pages.Tasks
{
    internal static class MonacoSupport
    {
        public static readonly HashSet<string> AllowedPreviewExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".txt",
            ".cs",
            ".js",
            ".json",
            ".md",
            ".xml",
            ".html",
            ".css",
            ".sql",
            ".py",
            ".java",
            ".cpp",
            ".c",
            ".ts",
            ".cshtml"
        };

        public static readonly Dictionary<string, string> MonacoLanguageByExtension = new(StringComparer.OrdinalIgnoreCase)
        {
            [".txt"] = "plaintext",
            [".md"] = "markdown",
            [".cs"] = "csharp",
            [".js"] = "javascript",
            [".ts"] = "typescript",
            [".json"] = "json",
            [".xml"] = "xml",
            [".html"] = "html",
            [".css"] = "css",
            [".sql"] = "sql",
            [".py"] = "python",
            [".java"] = "java",
            [".cpp"] = "cpp",
            [".c"] = "c",
            [".cshtml"] = "razor"
        };

        private static readonly Lazy<IReadOnlyList<string>> _monacoSupportedExtensions = new(() => MonacoLanguageByExtension
            .Keys
            .OrderBy(e => e)
            .ToList());

        public static IReadOnlyList<string> MonacoSupportedExtensions => _monacoSupportedExtensions.Value;
    }
}
