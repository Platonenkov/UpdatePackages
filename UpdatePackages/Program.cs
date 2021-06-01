using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UpdatePackages.Classes;

namespace UpdatePackages
{
    class Program
    {
        private static readonly DirectoryInfo CurrentDirectory = new DirectoryInfo(Environment.CurrentDirectory);
        private static Dictionary<string, int> FileChanges = new();

        private static UpdatingPackage Package = new()
        {
            Packages = new[]
            {
                new Package()
                {
                    Library = "!!!LibraryNameHere!!!", OldVersion = "1.1.1.4", NewVersion = "1.1.1.5"
                },
                new Package()
                {
                    Library = "!!!LibraryNameHere!!!", OldVersion = "1.0.0.23", NewVersion = "1.0.0.24"
                },
            },
            Sections = new[]{
                new Section(){
                    FileMask = "*.csproj",
                    Regular = new [] {
                        "{Name}, Version={version}",
                        "{Name}\" Version=\"{version}",
                        "{name}\\{version}",
                        "{Name}.{version}"
                    }
                },
                new Section(){
                    FileMask = "packages.config",
                    Regular = new [] {
                        "{Name}\" Version=\"{version}",
                        "{Name}\" version=\"{version}",
                    }
                },
            }
        };

        private const string SettingFileName = "UpdatePackagesScheme.json";
        static async Task Main(string[] args)
        {
            //await JsonInFile.SaveToFileAsync(SettingFileName, Sections);
            Console.WriteLine("Read configuration file");
            if (!File.Exists(SettingFileName))
            {
                "Configuration file not found".ConsoleRed();
                try
                {
                    "Attempt to create a configuration file".ConsoleYellow();
                    await JsonInFile.SaveToFileAsync(SettingFileName, Package);
                    $"Please enter configuration to the file - {SettingFileName}".PrintMessgeAndWaitEnter();
                    return;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Console.ReadLine();
                    return;
                }
            }
            UpdatingPackage settings_package;
            try
            {
                var data = await JsonInFile.LoadFromFile<UpdatingPackage>(SettingFileName);
                if (data is null)
                {
                    "Configuration is not correct".ConsoleRed();
                    "press any Enter to close programm".PrintMessgeAndWaitEnter();
                    return;
                }

                settings_package = data;
            }
            catch (Exception e)
            {
                $"Error to read configuration - {e.Message}".ConsoleRed();
                Console.WriteLine(e);
                "press any Enter to close programm".PrintMessgeAndWaitEnter();
                return;
            }
            var currDir = CurrentDirectory;

            var watcher = new Stopwatch();
            watcher.Start();

            TakeFilesFromProjects(currDir, settings_package);
            Console.WriteLine($"Completed in {GetStringTime(watcher.Elapsed)}");
            lock (FileChanges)
            {
                if (FileChanges.Count == 0)
                {
                    "No changes".ConsoleRed();
                }
                else
                    $"{FileChanges.Count} file(s) was changed".ConsoleYellow();

            }
            "press any Enter to close programm".PrintMessgeAndWaitEnter();
        }

        private static void TakeFilesFromProjects(DirectoryInfo directory, UpdatingPackage settings)
        {
            if (!directory.Exists)
            {
                "No Directory".ConsoleRed();
                return;
            }
            Console.WriteLine($"Find package files...");

            var tasks = new List<Task>();
            var packages = settings.Packages.ToArray();
            foreach (var section in settings.Sections)
            {
                var files = directory.EnumerateFiles(section.FileMask, SearchOption.AllDirectories).ToArray();
                if (files.Length == 0)
                {
                    $"No files in mask ".ConsoleYellow();
                    section.FileMask.ConsoleRed();
                    continue;
                }

                var end = files.Length > 1 ? "s" : "";
                $"Found {files.Length} file{end} with mask - {section.FileMask}".ConsoleGreen();

                var updating = CreateMask(packages, section.Regular.ToArray());

                foreach (var file in files)
                {
                    tasks.Add(UpdatePackageInFileAsync2(file.FullName, updating));
                }
            }
            if (tasks.Count == 0)
                return;


            Task.WaitAll(tasks.ToArray());
        }

        private static Dictionary<string, string> CreateMask(IEnumerable<Package> packages, string[] masks)
        {
            var dic = new Dictionary<string, string>();
            foreach (var package in packages)
                foreach (var mask in masks)
                {
                    try
                    {
                        if (mask.Contains("{Name}"))
                        {
                            var library = mask.Replace("{Name}", package.Library);
                            var old = library.Replace("{version}", package.OldVersion);

                            var new_lbr = library.Replace("{version}", package.NewVersion);
                            dic.Add(old, new_lbr);
                        }
                        else if (mask.Contains("{name}"))
                        {
                            var library = mask.Replace("{name}", package.Library.ToLower());
                            var old = library.Replace("{version}", package.OldVersion);

                            var new_lbr = library.Replace("{version}", package.NewVersion);
                            dic.Add(old, new_lbr);
                        }
                    }
                    catch (ArgumentNullException e)
                    {
                        Console.WriteLine($"Check Regular for null in {mask} - {e.Message}");
                        throw;
                    }

                    catch (ArgumentException e)
                    {
                        Console.WriteLine($"Check duplicates in regular - {mask}\n {e.Message}");
                        throw;
                    }

                }
            return dic;

        }

        private static async Task<bool> UpdatePackageInFileAsync2(string filePath, Dictionary<string, string> Updating)
        {
            try
            {
                var changes = 0;
                var text = await File.ReadAllTextAsync(filePath);
                text = Updating.Aggregate(text, (current, package) =>
                {
                    if (current.Contains(package.Key))
                        changes++;
                    return current.Replace(package.Key, package.Value);
                });
                if (changes > 0)
                    lock (FileChanges)
                    {
                        FileChanges.Add(filePath, changes);
                        $"File - {filePath}\nChange - {changes} row(s)".ConsoleYellow();
                    }
                else
                    return false;
                await File.WriteAllTextAsync(filePath, text);
                return true;
            }
            catch (Exception e)
            {
                $"File - {filePath}\n{e.Message}\n".ConsoleRed();
                return false;
            }
        }

        private static async Task<bool> UpdatePackageInFileAsync(string filePath, Dictionary<string,string> Updating)
        {
            try
            {
                var new_file_name = Path.ChangeExtension(filePath, "packageTemp");
                File.Copy(filePath, new_file_name, true);
                var changes = 0;
                using (var streame = new StreamReader(filePath))
                {
                    await using var sw = new StreamWriter(new_file_name, false);
                    while (!streame.EndOfStream)
                    {
                        if (await streame.ReadLineAsync() is { Length: > 0 } text)
                        {
                            var flag = false;
                            foreach (var (old_data, new_data) in Updating)
                                if (text.Contains(old_data))
                                {
                                    await sw.WriteLineAsync(text.Replace(old_data, new_data));
                                    changes++;
                                    flag = true;
                                    break;
                                }
                            if (!flag)
                                await sw.WriteLineAsync(text);
                        }
                        else
                            await sw.WriteLineAsync();
                    }
                }
                //File.Delete(filePath);
                //File.Replace(new_file_name, Path.ChangeExtension(filePath, Path.GetExtension(filePath)), $"C:\\Temp\\UptatePackages\\{Path.GetFileName(filePath)}");
                //File.Replace(filePath, new_file_name, $"C:\\Temp\\UptatePackages\\{Path.GetFileName(filePath)}");
                File.Move(new_file_name, filePath, true);
                if(changes > 0)
                    lock (FileChanges)
                    {
                        FileChanges.Add(filePath, changes);
                        $"File - {filePath}\nChange - {changes} row(s)".ConsoleYellow();
                    }
                return true;
            }
            catch (Exception e)
            {
                $"File - {filePath}\n{e.Message}\n".ConsoleRed();
                return false;
            }

        }
        /// <summary>
        /// Get time in string format
        /// </summary>
        private static string GetStringTime(TimeSpan time)
        {
            return time.Days > 0 ? time.ToString(@"d\.hh\:mm\:ss") :
                time.Hours > 0 ? time.ToString(@"hh\:mm\:ss") :
                time.Minutes > 0 ? time.ToString(@"mm\:ss") :
                time.Seconds > 0 ? time.ToString(@"g") : $"{Math.Round(time.TotalMilliseconds, 0)} ms";
        }
    }
}
