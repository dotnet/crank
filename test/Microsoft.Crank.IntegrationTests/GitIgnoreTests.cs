// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Crank.Controller.Ignore;
using Xunit;

namespace Microsoft.Crank.IntegrationTests
{
    public class GitIgnoreTests : IDisposable
    {
        string _tempFolder;

        public GitIgnoreTests()
        {
            do
            {
                _tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            } while (Directory.Exists(_tempFolder));

            Directory.CreateDirectory(_tempFolder);
        }

        public void Dispose()
        {
            if (!string.IsNullOrEmpty(_tempFolder) && Directory.Exists(_tempFolder))
            {
                Directory.Delete(_tempFolder, true);
                _tempFolder = null;
            }
        }

        [Fact]
        public void IgnoresBlankLinesAndComments()
        {
            CreateFile(".gitignore", "", " ", "\t", "# this is a comment");

            var ignoreFile = IgnoreFile.Parse(_tempFolder);

            Assert.Empty(ignoreFile.Rules);
        }

        [Fact]
        public void IgnoresGitFolders()
        {
            CreateFile(".gitignore", "# this is a comment");
            CreateFile(".git/file.txt");

            var ignoreFile = IgnoreFile.Parse(_tempFolder);

            var files = ignoreFile.ListDirectory(_tempFolder);

            AssertHasFile(".gitignore", files);
            AssertNotHasFile(".git/file.txt", files);
        }

        [Fact]
        public void EscapesStartHash()
        {
            CreateFile(".gitignore", @"\#.txt");
            CreateFile("#.txt");
            CreateFile("#file.txt");

            var ignoreFile = IgnoreFile.Parse(_tempFolder);

            var files = ignoreFile.ListDirectory(_tempFolder);

            AssertHasFile("#file.txt", files);
            AssertNotHasFile("#.txt", files);
        }

        [Fact]
        public void EscapesStartBang()
        {
            CreateFile(".gitignore", @"\!important!.txt");
            CreateFile("!important!.txt");
            CreateFile("important!.txt");

            var ignoreFile = IgnoreFile.Parse(_tempFolder);

            var files = ignoreFile.ListDirectory(_tempFolder);

            AssertHasFile("important!.txt", files);
            AssertNotHasFile("!important!.txt", files);
        }

        [SkipOnWindows("File System ignore spaces in extension")]
        public void EscapesTrailingSpace()
        {
            CreateFile(".gitignore", @"file.txt\   ");
            CreateFile("file.txt ");
            CreateFile("file.txt  ");

            var ignoreFile = IgnoreFile.Parse(_tempFolder);

            var files = ignoreFile.ListDirectory(_tempFolder);

            AssertHasFile("file.txt  ", files);
            AssertNotHasFile("file.txt ", files);
        }

        [Fact]
        public void AsteriskMatchesAnything()
        {
            // An asterisk "*" matches anything except a slash.

            CreateFile(".gitignore", @"*.txt");
            CreateFile("file.png");
            CreateFile("file.txt");
            CreateFile("a/file.png");
            CreateFile("a/file.txt");

            var ignoreFile = IgnoreFile.Parse(_tempFolder);

            var files = ignoreFile.ListDirectory(_tempFolder);

            AssertHasFile("file.png", files);
            AssertNotHasFile("file.txt", files);
            AssertHasFile("a/file.png", files);
            AssertNotHasFile("a/file.txt", files);
        }

        [Fact]
        public void QuestionMarkMatchesAnyOneCharacter()
        {
            // The character "?" matches any one character except "/"

            CreateFile(".gitignore", @"?.txt");
            CreateFile("f.png");
            CreateFile("f.txt");
            CreateFile("ff.txt");
            CreateFile("a/f.png");
            CreateFile("a/f.txt");
            CreateFile("a/ff.txt");

            var ignoreFile = IgnoreFile.Parse(_tempFolder);

            var files = ignoreFile.ListDirectory(_tempFolder);

            AssertHasFile("f.png", files);
            AssertNotHasFile("f.txt", files);
            AssertHasFile("ff.txt", files);
            AssertHasFile("a/f.png", files);
            AssertNotHasFile("a/f.txt", files);
            AssertHasFile("a/ff.txt", files);
        }

        [Fact]
        public void BangNegates()
        {
            // An optional prefix "!" which negates the pattern; any matching
            // file excluded by a previous pattern will become included again.

            CreateFile(".gitignore", @"*.txt", @"!important.txt");
            CreateFile("foo.txt");
            CreateFile("a/foo.txt");
            CreateFile("important.txt");
            CreateFile("a/important.txt");

            var ignoreFile = IgnoreFile.Parse(_tempFolder);

            var files = ignoreFile.ListDirectory(_tempFolder);

            AssertNotHasFile("foo.txt", files);
            AssertNotHasFile("a/foo.txt", files);
            AssertHasFile("important.txt", files);
            AssertHasFile("a/important.txt", files);
        }

        [Fact]
        public void RangeNotationMatchesAnyOneCharacterInRange()
        {
            // The range notation, e.g. [a-zA-Z], can be used to match one of the characters in a range

            CreateFile(".gitignore", @"[a-cR-Z].txt");
            CreateFile("a.png");
            CreateFile("a.txt");
            CreateFile("aa.txt");
            CreateFile("R.png");
            CreateFile("R.txt");
            CreateFile("RR.txt");
            CreateFile("f.png");
            CreateFile("f.txt");
            CreateFile("ff.txt");

            var ignoreFile = IgnoreFile.Parse(_tempFolder);

            var files = ignoreFile.ListDirectory(_tempFolder);

            AssertHasFile("a.png", files);
            AssertNotHasFile("a.txt", files);
            AssertHasFile("aa.txt", files);
            AssertHasFile("R.png", files);
            AssertNotHasFile("R.txt", files);
            AssertHasFile("RR.txt", files);
            AssertHasFile("f.png", files);
            AssertHasFile("f.txt", files);
            AssertHasFile("ff.txt", files);
        }

        [Fact]
        public void LeadingConsecutiveAsterisksBeforeSlashMatchAllDirectories()
        {
            // A leading "**" followed by a slash means match in all directories.
            // For example, "**/foo" matches file or directory "foo" anywhere, the
            // same as pattern "foo". "**/foo/bar" matches file or directory "bar"
            // anywhere that is directly under directory "foo".

            CreateFile(".gitignore", @"**/foo", @"**/bar/baz");
            CreateFile("foo");
            CreateFile("a/foo");
            CreateFile("a/b/foo");
            CreateFile("bar/baz");
            CreateFile("a/bar/baz");

            var ignoreFile = IgnoreFile.Parse(_tempFolder);

            var files = ignoreFile.ListDirectory(_tempFolder);

            AssertNotHasFile("foo", files);
            AssertNotHasFile("a/foo.txt", files);
            AssertNotHasFile("a/b/foo", files);
            AssertNotHasFile("bar/baz", files);
            AssertNotHasFile("a/bar/baz", files);
        }

        [Fact]
        public void TrailingConsecutiveAsterisksMatchesEverythingInside()
        {
            // A trailing "/**" matches everything inside. For example, "abc/**"
            // matches all files inside directory "abc", relative to the location
            // of the .gitignore file, with infinite depth.

            CreateFile(".gitignore", @"abc/**");
            CreateFile("foo");
            CreateFile("abc/foo");
            CreateFile("abc/def/foo");

            var ignoreFile = IgnoreFile.Parse(_tempFolder);

            var files = ignoreFile.ListDirectory(_tempFolder);

            AssertHasFile("foo", files);
            AssertNotHasFile("abc/foo", files);
            AssertNotHasFile("abc/def/foo", files);
        }

        [Fact]
        public void SlashConsecutiveAsterisksSlashMatchesZeroOrMoreDirectories()
        {
            // A slash followed by two consecutive asterisks then a slash matches
            // zero or more directories. For example, "a/**/b" matches "a/b",
            // "a/x/b", "a/x/y/b" and so on.

            CreateFile(".gitignore", @"a/**/b");
            CreateFile("ab");
            CreateFile("a/b");
            CreateFile("a/x/b");
            CreateFile("a/x/y/b");

            var ignoreFile = IgnoreFile.Parse(_tempFolder);

            var files = ignoreFile.ListDirectory(_tempFolder);

            AssertHasFile("ab", files);
            AssertNotHasFile("a/b", files);
            AssertNotHasFile("a/x/b", files);
            AssertNotHasFile("a/x/y/b", files);
        }

        [Fact]
        public void Example1()
        {
            // The pattern hello.* matches any file or directory whose name
            // begins with hello. If one wants to restrict this only to the
            // directory and not in its subdirectories, one can prepend the
            // pattern with a slash, i.e. /hello.*; the pattern now matches
            // hello.txt, hello.c but not a/hello.java.

            CreateFile(".gitignore", @"/hello.*");
            CreateFile("hello.txt");
            CreateFile("hello.c");
            CreateFile("a/hello.java");

            var ignoreFile = IgnoreFile.Parse(_tempFolder);

            var files = ignoreFile.ListDirectory(_tempFolder);

            AssertNotHasFile("hello.txt", files);
            AssertNotHasFile("hello.c", files);
            AssertHasFile("a/hello.java", files);
        }

        [Fact]
        public void Example2a()
        {
            // The pattern foo/ will match a directory foo and paths underneath
            // it, but will not match a regular file or a symbolic link foo

            CreateFile(".gitignore", @"foo/");
            CreateFile("foo");

            var ignoreFile = IgnoreFile.Parse(_tempFolder);

            var files = ignoreFile.ListDirectory(_tempFolder);

            AssertHasFile("foo", files);
        }

        [Fact]
        public void Example2b()
        {
            // The pattern foo/ will match a directory foo and paths underneath
            // it, but will not match a regular file or a symbolic link foo

            CreateFile(".gitignore", @"foo/");
            CreateFile("baz/foo");
            CreateFile("foo/bar");
            CreateFile("bar/foo/baz");

            var ignoreFile = IgnoreFile.Parse(_tempFolder);

            var files = ignoreFile.ListDirectory(_tempFolder);

            AssertHasFile("baz/foo", files);
            AssertNotHasFile("foo/bar", files);
            AssertNotHasFile("bar/foo/baz", files);
        }

        [Fact]
        public void Example4()
        {
            // The pattern "foo/*", matches "foo/test.json" (a regular file),
            // "foo/bar" (a directory), but it does not match
            // "foo/bar/hello.c" (a regular file), as the asterisk in the
            // pattern does not match "bar/hello.c" which has a slash in it.

            CreateFile(".gitignore", @"foo/*");
            //CreateFile("foo/test.json");
            //CreateDirectory("foo/bar");
            CreateFile("foo/bar/hello.c");

            var ignoreFile = IgnoreFile.Parse(_tempFolder);

            var files = ignoreFile.ListDirectory(_tempFolder);

            //AssertNotHasFile("foo/test.json", files);
            //AssertNotHasFile("foo/bar", files);
            AssertHasFile("foo/bar/hello.c", files);
        }

        private void CreateFile(string filePath, params string[] lines)
        {
            Directory.CreateDirectory(Normalize(Path.GetDirectoryName(filePath)));
            File.Delete(Normalize(filePath));
            File.AppendAllLines(Normalize(filePath), lines);
        }

        private void CreateDirectory(string directory, params string[] lines)
        {
            Directory.CreateDirectory(Normalize(directory));
        }

        private string Normalize(string filePath) => Path.GetFullPath(_tempFolder + Path.AltDirectorySeparatorChar + filePath).Replace("\\", "/");

        private void AssertHasFile(string localFilePath, IEnumerable<IGitFile> files)
        {
            foreach (var file in files)
            {
                if (string.Equals(Normalize(localFilePath), file.Path))
                {
                    return;
                }
            }

            Assert.Fail($"Expected '{localFilePath}' in files");
        }

        private void AssertNotHasFile(string localFilePath, IEnumerable<IGitFile> files)
        {
            foreach (var file in files)
            {
                if (string.Equals(Normalize(localFilePath), file.Path))
                {
                    Assert.Fail($"Found '{localFilePath}' in files");
                }
            }            
        }
    }
}
