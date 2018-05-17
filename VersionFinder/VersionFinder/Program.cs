using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace VersionFinder
{
    class Program
    {
        static void Main(string[] args)
        {
            Regex regex = new Regex(@"openapi-type:[\s\S]+?tag: ([\s\S]+?)```[\s\S]+?### Tag", RegexOptions.Compiled);

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
                        file.WriteLine(tag);

                        //var temp = Split(content, "### Basic Information").Last();
                        //temp = Split(temp, "tag: ").Last();
                        //var tag = Split(temp, "```").First().TrimEnd('\r', '\n');
                        //Split(temp, $"``` yaml $(tag) == '{tag}'");

                        Regex rx = new Regex(@"``` yaml \$\(tag\) == '" + tag + @"'([\s\S]+?)```", RegexOptions.Compiled | RegexOptions.IgnoreCase);

                        var inoutFile = rx.Match(content).Groups[1].Value;
                        file.WriteLine(inoutFile);
                        file.WriteLine("-----------------------------------");
                    }
                }
            }
        }

        static string[] Split(string str, string splitTerm)
        {
            return str.Split(new[] { splitTerm }, StringSplitOptions.None);
        }
    }
}
