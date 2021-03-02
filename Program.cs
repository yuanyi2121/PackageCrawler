using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PackageCrawler
{
    class Program
    {
        private static IDictionary<string, string> packages = new Dictionary<string, string>();

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Please specify the root folder");
                return;
            }

            var projectFiles = Directory.EnumerateFiles(args[0], "*.csproj", SearchOption.AllDirectories);
            foreach (var projectFile in projectFiles)
            {
                ParseCsproj(projectFile);
            }

            foreach (var package in packages.OrderBy(i => i.Key))
            {
                Console.WriteLine($"<PackageReference Update=\"{package.Key}\" Version=\"[{package.Value}]\" />");
            }
        }

        private static void ParseCsproj(string fileName)
        {
            var csproj = new Project(fileName);

            int packageRefNo = 0;
            foreach (var item in csproj.Items)
            {
                if (item.ItemType == "PackageReference")
                {
                    packageRefNo++;

                    var packageName = item.EvaluatedInclude;
                    var packageVer = item.Metadata.FirstOrDefault()?.EvaluatedValue;

                    if (string.IsNullOrEmpty(packageVer))
                    {
                        packageVer = item.Metadata.FirstOrDefault()?.UnevaluatedValue;
                        Console.WriteLine($"Warning: {fileName} missing version info for {packageName}");
                    }

                    if (packages.TryGetValue(packageName, out var existingVersion))
                    {
                        int compareResult = string.Compare(existingVersion, packageVer);
                        if (compareResult != 0)
                        {
                            Console.WriteLine($"Warning: {fileName} conflict version found for {packageName}, existing {existingVersion}, new {packageVer}");
                            if (compareResult < 0)
                            {
                                // Tend to use the newer version
                                packages[packageName] = packageVer;
                            }
                        }
                    }
                    else
                    {
                        packages[packageName] = packageVer;
                    }
                }
            }

            Console.WriteLine($"Scanned {fileName}, {packageRefNo} package reference found");
        }
    }
}
