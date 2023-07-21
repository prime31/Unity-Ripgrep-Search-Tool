using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Editor.Tools.Ripgrep
{
    public class Ripgrep
    {
        public struct Options
        {
#if UNITY_EDITOR_WIN
            const string QUOTE = "\"";
            const string RIPGREP_CMD = "rg ";
#else
            const string QUOTE = "'";
            const string RIPGREP_CMD = "/usr/local/bin/rg ";
#endif
            public string SearchTerm;
            public string WorkingDir;
            public bool LiteralStringSearch; // -F, disables regex
            public bool IgnoreCase; // -i
            public bool Multiline; // --multiline
            public string[] IncludedFileTypes; // -g '*.{asset,unity}' or -g 'Assets/**/*.{asset}'
            public string[] ExcludedFileTypes; // -g '!*.{asset}' or -g '!Assets/**/*.{asset}'
            public string[] SearchFolders;
            public string[] AdditionalArgs;
            public int ThreadCount;
            public Action<int> OnResultParsed;

            internal string GetCommand()
            {
                AdditionalArgs ??= Array.Empty<string>();
                Array.Resize(ref AdditionalArgs, AdditionalArgs.Length + 1);
                AdditionalArgs[^1] = "--json";

                if (ThreadCount > 1)
                {
                    Array.Resize(ref AdditionalArgs, AdditionalArgs.Length + 2);
                    AdditionalArgs[^2] = "-j";
                    AdditionalArgs[^1] = ThreadCount.ToString();
                }

                if (IgnoreCase)
                {
                    Array.Resize(ref AdditionalArgs, AdditionalArgs.Length + 1);
                    AdditionalArgs[^1] = "-i";
                }
                else // default to Smart Case, -S
                {
                    Array.Resize(ref AdditionalArgs, AdditionalArgs.Length + 1);
                    AdditionalArgs[^1] = "-S";
                }
                
                if (Multiline)
                {
                    Array.Resize(ref AdditionalArgs, AdditionalArgs.Length + 1);
                    AdditionalArgs[^1] = "--multiline";
                }

                if (LiteralStringSearch)
                {
                    Array.Resize(ref AdditionalArgs, AdditionalArgs.Length + 1);
                    AdditionalArgs[^1] = "-F";
                }

                // add our folders including file type filters if present. This is done for each SearchFolder if we have any
                if (SearchFolders?.Length > 0)
                {
                    var includeExtensions = IncludedFileTypes?.Length > 0 ? string.Join(',', IncludedFileTypes) : "*";
                    var includeTypeFilter = $"*.{{{includeExtensions}}}";
                    foreach (var folder in SearchFolders)
                    {
                        Array.Resize(ref AdditionalArgs, AdditionalArgs.Length + 2);
                        AdditionalArgs[^2] = "-g";
                        AdditionalArgs[^1] = $"{QUOTE}{folder}/**/{includeTypeFilter}{QUOTE}";
                    }

                    if (ExcludedFileTypes?.Length > 0)
                    {
                        var excludeTypeFilter = $"*.{{{string.Join(',', ExcludedFileTypes)}}}";
                        foreach (var folder in SearchFolders)
                        {
                            Array.Resize(ref AdditionalArgs, AdditionalArgs.Length + 2);
                            AdditionalArgs[^2] = "-g";
                            AdditionalArgs[^1] = $"{QUOTE}!{folder}/**/{excludeTypeFilter}{QUOTE}";
                        }
                    }
                }
                else 
                {
                    if (IncludedFileTypes?.Length > 0) // no folders but we have filetypes to glob for so add them
                    {
                        Array.Resize(ref AdditionalArgs, AdditionalArgs.Length + 2);
                        AdditionalArgs[^2] = "-g";
                        AdditionalArgs[^1] = $"{QUOTE}*.{{{string.Join(',', IncludedFileTypes)}}}{QUOTE}";
                    }
                    
                    if (ExcludedFileTypes?.Length > 0)
                    {
                        Array.Resize(ref AdditionalArgs, AdditionalArgs.Length + 2);
                        AdditionalArgs[^2] = "-g";
                        AdditionalArgs[^1] = $"{QUOTE}*.{{{string.Join(',', ExcludedFileTypes)}}}{QUOTE}";
                    }
                }
                
                var command = new StringBuilder(RIPGREP_CMD);
                var searchStrings = SearchTerm.Split(' ');
                for (var i = 0; i < searchStrings.Length; i++)
                    command.Append($"-e {QUOTE}{searchStrings[i]}{QUOTE} ");

                if (AdditionalArgs != null)
                    command.AppendJoin(' ', AdditionalArgs);
                return command.ToString();
            }

            public void RestrictToUnityAssetFiles() => IncludedFileTypes = new[] { "asset", "prefab", "unity" };

            public void AddIncludedFileType(string extension)
            {
                IncludedFileTypes ??= Array.Empty<string>();
                Array.Resize(ref IncludedFileTypes, IncludedFileTypes.Length + 1);
                IncludedFileTypes[^1] = extension;
            }
            
            public void AddExcludedFileType(string extension)
            {
                ExcludedFileTypes ??= Array.Empty<string>();
                Array.Resize(ref ExcludedFileTypes, ExcludedFileTypes.Length + 1);
                ExcludedFileTypes[^1] = extension;
            }

            public void AddSearchFolder(string folder)
            {
                SearchFolders ??= Array.Empty<string>();
                Array.Resize(ref SearchFolders, SearchFolders.Length + 1);
                SearchFolders[^1] = folder;
            }
        }

        public class Results
        {
            public string Text;
            public int LineNumber = -1;
            public List<Results> Children = new();
        }
        
        public class Future
        {
            double StartTime;
            Action<Results[]> onComplete;
            internal CancellationTokenSource cancellationTokenSource = new();
            public Future() => StartTime = EditorApplication.timeSinceStartup;
            public double GetElapsedTime() => EditorApplication.timeSinceStartup - StartTime;
            public void WhenComplete(Action<Results[]> action) => onComplete = action;
            public void SetResults(Results[] value) => onComplete?.Invoke(value);
            public void Cancel() => cancellationTokenSource.Cancel();
        }

        /// <summary>Runs the command in a Task. The Future will trigger on the main thread.</summary>
        public static Future RunAsync(Options opts, CancellationToken? token = null)
        {
            var future = new Future();
            Task.Run(() =>
            {
                var res = Run(opts, future.cancellationTokenSource.Token);
                EditorApplication.delayCall += () => future.SetResults(res);
            }, future.cancellationTokenSource.Token);
            return future;
        }

        /// <summary>Runs the Ripgrep command and parses the results</summary>
        public static Results[] Run(Options opts, CancellationToken? token = null)
        {
            var processStartInfo = GetProcessStartInfo(opts);
            var process = Process.Start(processStartInfo) ?? throw new Exception("Process failed to start");

            var results = new List<Results>();
            Results currentResult = default;
            string json;
            while ((json = process.StandardOutput.ReadLine()) != null)
            {
                if (token.HasValue && token.Value.IsCancellationRequested)
                {
                    process.Kill();
                    process.Dispose();
                    return results.ToArray();
                }

                var jsonLine = JsonUtility.FromJson<JsonLine>(json);
                switch (jsonLine.type)
                {
                    case "begin":
                        var beginData = JsonUtility.FromJson<JsonBegin>(json).data;
                        currentResult = new Results
                        {
                            Text = beginData.path.text
                        };
                        results.Add(currentResult);
                        
                        if (opts.OnResultParsed != null)
                        {
                            var cnt = results.Count;
                            EditorApplication.delayCall += () => opts.OnResultParsed(cnt);
                        }
                        break;
                    case "match":
                        var matchData = JsonUtility.FromJson<JsonMatch>(json).data;
                        
                        currentResult.Children.Add(new Results
                        {
                            LineNumber = matchData.line_number,
                            Text = matchData.lines.text.Trim()
                        });
                        break;
                    case "end":
                        currentResult = null;
                        break;
                    case "summary": // stats, not very useful
                        // var summaryData = JsonUtility.FromJson<JsonSummary>(json).data;
                        goto LoopDone; // Windows never exits the loop so we use this to jump out when we hit a summary
                    case "context": // never seen it returned
                        break;
                }
            }

            LoopDone:

            while ((json = process.StandardError.ReadLine()) != null)
                UnityEngine.Debug.LogError(json);

            process.WaitForExit();
            process.Dispose();

            return results.ToArray();
        }

        static ProcessStartInfo GetProcessStartInfo(Options opts)
        {
            opts.WorkingDir ??= Environment.CurrentDirectory;

            var command = opts.GetCommand();

#if UNITY_EDITOR_OSX
            return new ProcessStartInfo
            {
                FileName = "/bin/zsh",
                Arguments = $"-c \"{command}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = opts.WorkingDir
            };
#else
            var workingDirectory = opts.WorkingDir.Replace("/", "\\");
            return new ProcessStartInfo
            {
                FileName = "CMD.EXE",
                Arguments = $"/K \" {command} \"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = opts.WorkingDir
            };
#endif
        }
    }
}
