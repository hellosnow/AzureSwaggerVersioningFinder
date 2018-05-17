using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

namespace VersionFinder
{
    class Program
    {
        static void Main(string[] args)
        {
            Regex regex = new Regex(@"openapi-type:[\s\S]+?tag: ([\s\S]+?)(```|[\r\n]+)[\s\S]+?### Tag", RegexOptions.Compiled);
            using (StreamWriter file = new StreamWriter(@"D:/AzureVersioning.txt"))
            {
                foreach (var fileName in Directory.EnumerateFiles(@"D:/Code/azure-rest-api-specs/specification", "*readme.md", SearchOption.AllDirectories))
                {
                    var content = File.ReadAllText(fileName);
                    if (content.Contains("### Basic Information"))
                    {
                        file.WriteLine(fileName);
                        Console.WriteLine(fileName);
                        var tag = regex.Match(content).Groups[1].Value.TrimEnd('\r', '\n');
                        file.WriteLine("Tag: " + tag);
                        file.WriteLine();

                        Regex rx = new Regex(@"``` yaml \$\(tag\) == '" + tag + @"'([\s\S]+?)```", RegexOptions.Compiled | RegexOptions.IgnoreCase);

                        var inputFile = rx.Match(content).Groups[1].Value;

                        var stableFiles = new HashSet<string>();
                        var previewFiles = new HashSet<string>();
                        foreach (var line in Split(inputFile, Environment.NewLine))
                        {
                            if (line.StartsWith("input-file:") || string.IsNullOrWhiteSpace(line))
                            {
                                continue;
                            }
                            else if (line.StartsWith("directive:") || line.StartsWith("#") || line.StartsWith("title:")) //sql
                            {
                                break;
                            }
                            var swagger = line.TrimStart('-', ' ','\"').TrimEnd('\"');

                            if (!swagger.Contains("stable") && !swagger.Contains("preview"))
                            {
                                if (swagger.Contains("/v"))
                                {
                                    file.WriteLine("OOps there's no stable/preview but v* version");
                                    file.WriteLine(swagger);
                                }
                                else
                                {
                                    throw new InvalidOperationException("path not expected" + swagger);
                                }
                            }
                            else if (swagger.Contains("stable"))
                            {
                                stableFiles.Add(swagger);
                            }
                            else if (swagger.Contains("preview"))
                            {
                                previewFiles.Add(swagger);
                            }
                        }

                        Print(stableFiles, file, "stable");
                        Print(previewFiles, file, "preview");

                        Infer(previewFiles, fileName, file, "preview");
                        Infer(stableFiles, fileName, file, "stable");

                        file.WriteLine();
                        file.WriteLine("----------------------------------------------------------------------");
                    }
                }
            }
        }

        static void Infer(HashSet<string> set, string topDir, StreamWriter writer, string category)
        {
            Regex calendarRegex = new Regex(@"\d{4}-\d{2}-\d{2}(-preview)?", RegexOptions.Compiled);
            Regex numberRegex = new Regex(@"(v?)\d(.?)(\d?)", RegexOptions.Compiled);
            if (set.Count == 0)
            {
                var dir = Path.GetDirectoryName(topDir);
                foreach (var dirName in Directory.EnumerateDirectories(dir, $"*{category}*", SearchOption.AllDirectories))
                {
                    var versionset = new HashSet<string>();
                    if (!dirName.EndsWith($"-{category}")) // skip "calendar-preview"
                    {
                        var subFolders = Directory.EnumerateDirectories(dirName);
                        if (subFolders.Any())
                        {
                            writer.WriteLine();
                            writer.WriteLine($"#####Inferred {category} versions#####");
                        }
                        foreach (var subDirName in subFolders)
                        {
                            var dirShortName = Path.GetFileName(subDirName);
                            if (calendarRegex.Match(dirShortName).Success)
                            {
                                versionset.Add(dirShortName);
                                writer.WriteLine(dirShortName);
                            }
                            else if (numberRegex.Match(dirShortName).Success)
                            {
                                versionset.Add(dirShortName);
                                writer.WriteLine(dirShortName);
                            }
                            else
                            {
                                throw new InvalidOperationException("directory not expected" + subDirName);
                            }
                        }
                    }
                    // Print the possible files
                    if (versionset.Count > 0)
                    {
                        var top = versionset.Last();
                        var filePath = Path.Combine(dirName, top);
                        writer.WriteLine();
                        writer.WriteLine($"#####Inferred {category} files#####");
                        foreach (var item in Directory.EnumerateFiles(filePath))
                        {
                            var itemWithoutPrefix = item.TrimStart(dir.ToCharArray()).Replace(@"\", @"/");
                            writer.WriteLine(itemWithoutPrefix);
                        }
                    }
                }
            }
        }

        static string[] Split(string str, string splitTerm)
        {
            return str.Split(new[] { splitTerm }, StringSplitOptions.RemoveEmptyEntries);
        }

        static void Print(HashSet<string> set, StreamWriter writer, string category)
        {
            if (set.Count > 0)
            {
                writer.WriteLine("******" + category + "******");
            }
            foreach (var str in set)
            {
                writer.WriteLine(str);
            }
        }
    }
}
