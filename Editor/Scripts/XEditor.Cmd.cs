// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using EFramework.Utility;

namespace EFramework.Editor
{
    public partial class XEditor
    {
        /// <summary>
        /// XEditor.Cmd 是一个用于在编辑器中执行命令行操作的工具模块，提供了命令查找和执行功能，支持跨平台操作。
        /// </summary>
        /// <remarks>
        /// <code>
        /// 功能特性
        /// - 支持 Windows/Linux/macOS 跨平台：自动适配不同系统的命令路径
        /// - 提供命令路径查找：支持在系统 PATH、环境变量和自定义路径中查找命令
        /// - 实现异步命令执行：支持命令执行进度显示和取消操作
        /// - 支持 UTF-8 编码：自动处理命令输出的编码问题
        /// 
        /// 使用手册
        /// 1. 命令查找
        /// 
        /// 1.1 查找系统命令
        /// 
        ///     // 在系统PATH中查找git命令
        ///     string gitPath = XEditor.Cmd.Find("git");
        ///     if (!string.IsNullOrEmpty(gitPath))
        ///     {
        ///         Debug.Log($"找到git命令：{gitPath}");
        ///     }
        /// 
        /// 1.2 查找自定义路径命令
        /// 
        ///     // 在指定路径中查找命令
        ///     string customPath = XEditor.Cmd.Find("custom.exe", "C:/Tools", "D:/Apps");
        /// 
        /// 2. 命令执行
        /// 
        /// 2.1 基本执行
        /// 
        ///     // 执行git status命令
        ///     var result = await XEditor.Cmd.Run("git", XEnv.ProjectPath, false, "status");
        ///     if (result.Code == 0)
        ///     {
        ///         Debug.Log($"命令输出：{result.Data}");
        ///     }
        ///     else
        ///     {
        ///         Debug.LogError($"命令错误：{result.Error}");
        ///     }
        /// 
        /// 2.2 静默执行
        /// 
        ///     // 静默执行命令（不显示进度条）
        ///     var result = await XEditor.Cmd.Run("git", XEnv.ProjectPath, true, "pull");
        /// </code>
        /// 更多信息请参考模块文档。
        /// </remarks>
        public class Cmd
        {
            /// <summary>
            /// 命令输出数据结构。
            /// </summary>
            internal class Output
            {
                /// <summary>
                /// 输出内容。
                /// </summary>
                internal string Data;

                /// <summary>
                /// 是否为错误输出。
                /// </summary>
                internal bool Error;
            }

            /// <summary>
            /// 命令执行结果数据结构。
            /// </summary>
            public class Result
            {
                /// <summary>
                /// 命令返回码，-1表示未执行。
                /// </summary>
                public int Code = -1;

                /// <summary>
                /// 标准输出内容。
                /// </summary>
                public string Data = string.Empty;

                /// <summary>
                /// 错误输出内容。
                /// </summary>
                public string Error = string.Empty;
            }

            /// <summary>
            /// 是否处于批处理模式。
            /// </summary>
            internal static bool batchMode;

            /// <summary>
            /// 初始化命令执行环境。
            /// </summary>
            [InitializeOnLoadMethod]
            internal static void OnInit() { batchMode = Application.isBatchMode; }

            /// <summary>
            /// 查找命令的完整路径。支持在系统 PATH、环境变量和自定义路径中查找，自动适配不同操作系统的命令后缀。
            /// </summary>
            /// <param name="cmd">要查找的命令名称，如 git、python 等。</param>
            /// <param name="extras">额外的查找路径列表。</param>
            /// <returns>命令的完整路径，如果未找到则返回空字符串。</returns>
            /// <remarks>
            /// Windows 下会自动添加 .cmd 和 .exe 后缀进行查找。
            /// macOS 下会自动添加 .app 后缀进行查找。
            /// </remarks>
            public static string Find(string cmd, params string[] extras)
            {
                if (string.IsNullOrEmpty(cmd)) return string.Empty;

                var names = new string[] { cmd };
                if (!cmd.Contains("."))
                {
                    if (Application.platform == RuntimePlatform.WindowsEditor)
                    {
                        names = new string[] { cmd + ".cmd", cmd + ".exe" };
                    }
                    else if (Application.platform == RuntimePlatform.OSXEditor)
                    {
                        names = new string[] { cmd, cmd + ".app" };
                    }
                }

                // 在系统 PATH 中查找命令
#if UNITY_EDITOR_WIN
                var path = Environment.GetEnvironmentVariable("PATH");
#else
                // 在 macOS 上，Environment.GetEnvironmentVariable("PATH") 可能无法获取到完整的 PATH 环境变量，
                // 特别是像 /usr/local/bin 这样的目录，这通常是因为 Unity 或 Mono（Unity 使用的 .NET 框架）在启动时可能不会完全继承系统的环境变量。
                // 这个问题在多个平台上都可能出现，但 macOS 尤其显著，因为 macOS 的环境变量处理方式与其他 Unix-like 系统（如 Linux）有所不同。
                // 在 macOS 上，应用程序（包括通过 Unity 编辑器运行的应用程序）可能不会默认继承终端会话中的环境变量。
                // by：文心一言
                var path = Environment.GetEnvironmentVariable("PATH") +
                    Path.PathSeparator + "/usr/local/bin" +
                    Path.PathSeparator + "/usr/local/share/dotnet";
#endif
                var paths = path.Split(Path.PathSeparator);
                foreach (var part in paths)
                {
                    foreach (var name in names)
                    {
                        var file = XFile.PathJoin(part, name);
                        if (XFile.HasFile(file)) return file;
                    }
                }

                // 在环境变量中查找命令
                foreach (DictionaryEntry kvp in Environment.GetEnvironmentVariables())
                {
                    if (kvp.Value == null) continue;
                    var part = kvp.Value.ToString();
                    foreach (var name in names)
                    {
                        var file = XFile.PathJoin(part, name);
                        if (XFile.HasFile(file)) return file;
                    }
                }

                // 在自定义路径中查找命令
                foreach (var part in extras)
                {
                    foreach (var name in names)
                    {
                        var file = XFile.PathJoin(part, name);
                        if (XFile.HasFile(file)) return file;
                    }
                }
                return cmd;
            }

            /// <summary>
            /// 异步执行指定的命令。支持实时输出、进度显示和取消操作。
            /// </summary>
            /// <param name="bin">命令的完整路径。</param>
            /// <param name="cwd">执行命令的工作目录，默认为项目路径。</param>
            /// <param name="print">是否打印日志。</param>
            /// <param name="progress">是否显示进度条。</param>
            /// <param name="args">命令行参数列表。</param>
            /// <returns>包含命令执行结果的异步任务。</returns>
            /// <exception cref="ArgumentNullException">当 bin 参数为空时抛出。</exception>
            /// <remarks>
            /// - 所有输出均使用 UTF-8 编码。
            /// - 支持通过进度条取消执行（非批处理模式）。
            /// - 自动移除输出中的 ANSI 转义序列。
            /// </remarks>
            public static Task<Result> Run(string bin, string cwd = "", bool print = true, bool progress = true, params string[] args)
            {
                if (string.IsNullOrEmpty(bin)) throw new ArgumentNullException("bin");

                var name = Path.GetFileName(bin);
                var info = new ProcessStartInfo
                {
                    FileName = bin,
                    WorkingDirectory = string.IsNullOrEmpty(cwd) ? XEnv.ProjectPath : cwd,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                    StandardInputEncoding = Encoding.UTF8,
                    Arguments = args != null && args.Length > 0 ? string.Join(" ", args) : ""
                };
#if !UNITY_EDITOR_WIN
                info.Environment["PATH"] = info.Environment["PATH"] +
                    Path.PathSeparator + "/usr/local/bin" +
                    Path.PathSeparator + "/usr/local/share/dotnet";
#endif

                if (print) XLog.Debug("XEditor.Cmd.Run: start {0} with arguments: {1}", name, info.Arguments);

                return Task.Run(() =>
                {
                    var cancel = false;
                    var stdout = new StringBuilder();
                    var stderr = new StringBuilder();
                    var outputs = new Queue<Output>();
                    var done = false;

                    using var proc = new Process { StartInfo = info };
                    proc.Start();
                    proc.OutputDataReceived += (_, evt) => // 使用异步监听的方式，避免阻塞
                    {
                        if (evt.Data == null) return;
                        lock (outputs) outputs.Enqueue(new Output() { Data = Regex.Replace(evt.Data, @"\x1b\[[0-9;]*[a-zA-Z]", ""), Error = false });
                    };
                    proc.ErrorDataReceived += (_, evt) =>
                    {
                        if (evt.Data == null) return;
                        lock (outputs) outputs.Enqueue(new Output() { Data = Regex.Replace(evt.Data, @"\x1b\[[0-9;]*[a-zA-Z]", ""), Error = true });
                    };
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();

                    var printTask = Task.Run(() =>
                    {
                        while (!done)
                        {
                            lock (outputs)
                            {
                                while (outputs.Count > 0)
                                {
                                    var output = outputs.Dequeue();
                                    if (output.Error)
                                    {
                                        if (print) XLog.Error(output.Data);
                                        stderr.AppendLine(output.Data);
                                    }
                                    else
                                    {
                                        if (print) XLog.Debug(output.Data);
                                        stdout.AppendLine(output.Data);
                                    }

                                    if (!batchMode && print && progress && !cancel) XLoom.RunInMain(() =>
                                    {
                                        // 在日志数量过多的情况下可能刷新不及时
                                        if (!cancel)
                                        {
                                            if (EditorUtility.DisplayCancelableProgressBar($"Run {name}", output.Data, 0.6f))
                                            {
                                                cancel = true;
                                                EditorUtility.ClearProgressBar();
                                            }
                                        }
                                    });
                                }
                            }
                        }
                        if (!batchMode && print && progress) XLoom.RunInMain(() => EditorUtility.ClearProgressBar());
                    });

                    proc.WaitForExit();
                    done = true;
                    Task.WaitAll(printTask);

                    if (print)
                    {
                        if (proc.ExitCode != 0) XLog.Error("XEditor.Cmd.Run: finish {0} with code: {1}", name, proc.ExitCode);
                        else XLog.Debug("XEditor.Cmd.Run: finish {0} with code: {1}", name, proc.ExitCode);
                    }

                    var result = new Result { Code = proc.ExitCode, Data = stdout.ToString() };
                    if (stderr.Length > 0) result.Error = stderr.ToString();
                    else if (result.Code != 0) result.Error = result.Data;
                    return result;
                });
            }
        }
    }
}
