// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.RegularExpressions;

namespace Microsoft.Crank.Controller.Ignore
{
    public interface IGitFile
    {
        bool IsDirectory { get; }
        string Path { get; }
    }

    public class GitFile : IGitFile
    {
        public GitFile(string path)
        {
            Path = path.Replace('\\', '/');
        }

        public bool IsDirectory => false;

        public string Path { get; }

        public override string ToString()
        {
            return Path;
        }
    }

    public class GitDirectory : IGitFile
    {
        public GitDirectory(string path)
        {
            Path = path.Replace('\\', '/');
        }

        public bool IsDirectory => true;

        public string Path { get; }
        public override string ToString()
        {
            return Path;
        }
    }

    public class IgnoreRule
    {
        // Temporary marker to prevent '.*' from being processed as '*'
        private const string DotStar = "\\DOT_STAR\\";

        public string Rule { get; private set; }

        private string _basePath;
        private bool _matchDir = true;
        private string _pattern;
        private Regex _regex;
        public bool Negate = false;

        public bool Match(IGitFile file)
        {
            if (!_matchDir && file.IsDirectory)
            {
                return false;
            }

            if (!file.Path.StartsWith(_basePath))
            {
                return false;
            }

            var localPath = file.Path.Substring(_basePath.Length);

            return _regex.IsMatch(localPath);
        }

        private IgnoreRule()
        {
        }

        public static IgnoreRule Parse(string basePath, string rule)
        {
            var ignoreRule = new IgnoreRule
            {
                _basePath = basePath,
                Rule = rule
            };

            if (string.IsNullOrEmpty(rule))
            {
                throw new ArgumentException("Invalid empty rule");
            }

            var firstChar = rule[0];

            if (firstChar == '\\')
            {
                if (rule.Length > 1)
                {
                    rule = rule.Substring(1);
                }
                else
                {
                    return null;
                }
            }

            // Detect escaped '!' character by checking it there is a backslash ("\") in front.
            if (rule.StartsWith(@"\!"))
            {
                rule = rule[1..];
            }
            else
            {
                if (firstChar == '!')
                {
                    ignoreRule.Negate = true;
                    rule = rule[1..];
                }
            }

            rule = rule.Replace('\\', '/');

            rule = rule.Replace(".", "\\.");

            // A leading slash matches the beginning of the pathname. For example, "/*.c" matches "cat-file.c" but not "mozilla-sha1/sha1.c".
            if (rule.StartsWith("/"))
            {
                rule = "^" + rule[1..];
            }
            else if (rule.StartsWith("**/"))
            {
                rule = $"(/|^){DotStar}{rule[3..]}";
            }
            else 
            {
                rule = "(/|^)" + rule;
            }

            if (rule.EndsWith("/*"))
            {
                rule += "$";
            }

            // Spec: If there is a separator at the end of the pattern then the pattern will only match directories,
            // otherwise the pattern can match both files and directories.

            // i.e., if the pattern ends with '/', we keep it in the regex so we can match a directory name,
            // otherwise we use the pattern either for a folder name or the filename (end of string)
            if (!rule.EndsWith('/'))
            {
                // match the whole word, either before / or the end of the string
                // i.e., "foo" matches "/foo", "bar/foo" but not "foobar"
                rule += "(/|$)";
            }

            // A trailing "/**" matches everything inside.
            if (rule.EndsWith("/**"))
            {
                ignoreRule._matchDir = false;
            }

            // A double asterisk matches zero or more directories. 
            // The pattern is to look for any chars, including /
            rule = rule.Replace("/**/", $"{DotStar}/{DotStar}");

            // "*" matches anything except "/"
            rule = rule.Replace("*", @"[^/]*");

            // "?" matches any one character except "/"
            rule = rule.Replace("?", @"[^/]");

            rule = rule.Replace(DotStar, ".*");
            ignoreRule._pattern = rule;

            ignoreRule._regex = new Regex(ignoreRule._pattern, RegexOptions.Compiled);

            return ignoreRule;
        }

        public override string ToString()
        {
            return _pattern;
        }
    }
}