// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.Crank.Controller.Ignore
{
    public class IgnoreFile
    {
        private readonly string _gitIgnorePath;

        private IgnoreFile(string gitIgnorePath)
        {
            _gitIgnorePath = gitIgnorePath;
        }

        public List<IgnoreRule> Rules { get; } = new List<IgnoreRule>();

        /// <summary>
        /// Parses a gitignore file.
        /// </summary>
        /// <param name="path">The path of the file to parse.</param>
        /// <param name="ignoreParentDirectories">If <c>true</c>, the gitignore files in parent directories will be included.</param>
        /// <returns>A list of <see cref="IgnoreRule"/> instances.</returns>
        public static IgnoreFile Parse(string path, bool includeParentDirectories = false)
        {
            var ignoreFile = new IgnoreFile(path);

            var currentDir = new DirectoryInfo(Path.GetDirectoryName(path));
            
            while (currentDir != null && currentDir.Exists)
            {
                var gitIgnoreFilename = Path.Combine(currentDir.FullName, ".gitignore");

                if (File.Exists(gitIgnoreFilename))
                {
                    var basePath = currentDir.FullName.Replace("\\", "/") + "/";

                    var localRules = new List<IgnoreRule>();

                    // Ignore .git folders by default
                    localRules.Add(IgnoreRule.Parse(basePath, ".git/"));

                    // Don't process parent folder if we are at the repository level
                    if (Directory.Exists(Path.Combine(currentDir.FullName, ".git")))
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

                    currentDir = currentDir?.Parent;
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

        private void ListDirectory(string path, List<IGitFile> accumulator)
        {
            foreach (var filename in Directory.EnumerateFiles(path))
            {
                var gitFile = new GitFile(filename);

                var ignore = false;

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

            foreach (var directoryName in Directory.EnumerateDirectories(path))
            {
                var gitFile = new GitDirectory(directoryName);

                var ignore = false;

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
                    ListDirectory(directoryName, accumulator);
                }
            }
        }
    }
}
