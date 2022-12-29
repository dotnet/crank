// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Microsoft.Crank.Controller.Ignore
{
    public class IgnoreFile
    {
        public List<IgnoreRule> Rules { get; } = new List<IgnoreRule>();

        /// <summary>
        /// Parses a gitignore file.
        /// </summary>
        /// <param name="path">The path of the file to parse.</param>
        /// <param name="ignoreParentDirectories">If <c>true</c>, the gitignore files in parent directories will be included.</param>
        /// <returns>A list of <see cref="IgnoreRule"/> instances.</returns>
        public static IgnoreFile Parse(string path, bool includeParentDirectories = false)
        {
            var ignoreFile = new IgnoreFile();

            var currentDir = Directory.Exists(path) ? new DirectoryInfo(path) : new DirectoryInfo(Path.GetDirectoryName(path));
            
            while (currentDir != null && currentDir.Exists)
            {
                var gitIgnoreFilename = Path.GetFullPath(currentDir.FullName + "/.gitignore");

                if (File.Exists(gitIgnoreFilename))
                {
                    var basePath = currentDir.FullName.Replace("\\", "/") + "/";

                    var localRules = new List<IgnoreRule>();

                    // Don't process parent folder if we are at the repository level
                    if (Directory.Exists(Path.GetFullPath(currentDir.FullName + "/.git")))
                    {
                        currentDir = null;
                    }

                    using (var stream = File.OpenText(gitIgnoreFilename))
                    {
                        string rule = null;

                        while (null != (rule = stream.ReadLine()))
                        {
                            // A blank line matches no files, so it can serve as a separator for readability.
                            if (string.IsNullOrWhiteSpace(rule))
                            {
                                continue;
                            }

                            // A line starting with # serves as a comment. 
                            if (rule.StartsWith('#'))
                            {
                                continue;
                            }

                            // Put a backslash ("\") in front of the first hash for patterns that begin with a hash.
                            if (rule.StartsWith(@"\#"))
                            {
                                rule = rule[1..];
                            }

                            // Trailing spaces are ignored unless they are quoted with backslash ("\").
                            if (rule.EndsWith(" "))
                            {
                                var index = rule.LastIndexOf('\\');
                                var trimmed = rule.TrimEnd();
                                if (index == -1 || trimmed[^1] != '\\')
                                {
                                    // Not an escape sequence
                                    rule = trimmed;
                                }
                                else
                                {
                                    rule = rule.Substring(0, index) + rule.Substring(index + 1);
                                }
                            }

                            var ignoreRule = IgnoreRule.Parse(basePath, rule);

                            if (ignoreRule != null)
                            {
                                localRules.Add(ignoreRule);
                            }
                        }
                    }

                    // Insert the rules from this folder at the top of the list, while preserving the file order
                    for (var i = 0; i < localRules.Count; i++)
                    {
                        ignoreFile.Rules.Insert(i, localRules[i]);
                    }
                }

                if (includeParentDirectories)
                {
                    currentDir = currentDir?.Parent;
                }
                else
                {
                    break;
                }
            }

            return ignoreFile;
        }

        /// <summary>
        /// Lists all the matching files.
        /// </summary>
        public IList<IGitFile> ListDirectory(string path)
        {
            var result = new List<IGitFile>();
            ListDirectory(path, result);

            return result;
        }

        private static readonly Regex GitFolderRegex = new(@"(/|^)\.git/", RegexOptions.Compiled);

        private void ListDirectory(string path, List<IGitFile> accumulator)
        {
            foreach (var filename in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
            {
                var gitFile = new GitFile(filename);

                var ignore = false;

                // Ignore .git folders content by default
                if (GitFolderRegex.IsMatch(gitFile.Path))
                {
                    ignore = true;
                }

                foreach (var rule in Rules)
                {
                    if (rule.Match(gitFile))
                    {
                        ignore = true;

                        if (rule.Negate)
                        {
                            ignore = false;
                        }
                    }
                }

                if (!ignore)
                {
                    accumulator.Add(gitFile);
                }
            }
        }
    }
}
