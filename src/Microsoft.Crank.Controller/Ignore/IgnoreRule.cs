﻿// Licensed to the .NET Foundation under one or more agreements.
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
        private bool _matchFile = true;
        private bool _matchDir = true;
        private string _pattern;
        private string _rule;
        private Regex _regex;
        public bool Negate = false;

        public bool Match(string basePath, IGitFile file)
        {
            if (!_matchDir && file.IsDirectory)
            {
                return false;
            }

            if (!_matchFile && !file.IsDirectory)
            {
                return false;
            }

            if (!file.Path.StartsWith(basePath))
            {
                return false;
            }

            var localPath = file.Path.Substring(basePath.Length);

            return _regex.IsMatch(localPath);
        }

        private IgnoreRule()
        {
        }

        public static IgnoreRule Parse(string rule)
        {
            var ignoreRule = new IgnoreRule();

            ignoreRule._rule = rule;

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

            rule = rule.Replace('\\', '/');

            if (firstChar == '!')
            {
                ignoreRule.Negate = true;
            }

            rule = rule.Replace(".", "\\.");

            // A leading slash matches the beginning of the pathname. For example, "/*.c" matches "cat-file.c" but not "mozilla-sha1/sha1.c".
            if (rule.StartsWith("/"))
            {
                rule = "^" + rule;
            }
            else
            {
                rule = "(/|^)" + rule;
            }

            if (rule.EndsWith('/'))
            {
                ignoreRule._matchFile = false;
            }
            else
            {
                // match the whole word, either before / or the end of the string
                // i.e., "foo" matches "/foo", "bar/foo" but not "foobar"
                rule = rule + "(/|$)";
            }

            // A trailing "/**" matches everything inside.
            if (rule.EndsWith("/**"))
            {
                ignoreRule._matchDir = false;
            }

            // A double asterisk matches zero or more directories. 
            // The pattern is to look for any chars, including /
            rule = rule.Replace("**", ".*");

            // "*" matches anything except "/"
            rule = rule.Replace("*", @"[^/]*");

            // "?" matches any one character except "/"
            rule = rule.Replace("?", @"[^/]");

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