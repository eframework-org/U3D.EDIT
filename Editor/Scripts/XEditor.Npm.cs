// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using System.Collections.Generic;
using UnityEditor;
using EFramework.Utility;

namespace EFramework.Editor
{
    public partial class XEditor
    {
        /// <summary>
        /// XEditor.Npm 提供了在 Unity 编辑器中调用和执行 NPM 脚本的工具，支持异步执行、参数传递和错误处理。
        /// </summary>
        /// <remarks>
        /// <code>
        /// 功能特性
        /// - 支持在 Unity 编辑器中异步执行 NPM 脚本：通过后台任务执行不阻塞主线程
        /// - 支持传递参数给 NPM 脚本：灵活配置脚本执行行为
        /// - 支持错误处理和日志输出：可靠捕获和展示执行结果
        /// - 支持单例模式避免重复执行：优化资源使用
        /// - 自动刷新 AssetDatabase 以同步资源变更：保持项目资源状态一致
        /// 
        /// 使用手册
        /// 1. 命令执行
        /// 
        /// 1.1 创建 NPM 任务
        ///     创建一个 NPM 任务实例，指定任务 ID、脚本名称和执行选项：
        ///     
        ///     // 创建 NPM 任务，指定 ID、脚本名称、同步执行和工作目录
        ///     var npm = new XEditor.Npm(
        ///         id: "my-task",         // 任务唯一标识符
        ///         script: "my-task",     // package.json 中定义的脚本名称
        ///         runasync: false,       // 是否异步执行
        ///         cwd: "path/to/dir"     // 工作目录路径
        ///     );
        /// 
        /// 1.2 传递参数并执行任务
        ///     通过字典传递参数并执行 NPM 任务：
        ///     
        ///     // 准备参数字典
        ///     var args = new Dictionary&lt;string, string&gt;
        ///     {
        ///         { "param1", "value1" },
        ///         { "param2", "value2" }
        ///     };
        ///     
        ///     // 执行任务并传递参数
        ///     var report = XEditor.Tasks.Execute(npm, args);
        ///     
        ///     // 等待任务完成（如果是同步任务，可以省略此步骤）
        ///     report.Task.Wait();
        /// 
        /// 2. 结果处理
        /// 
        /// 2.1 验证执行结果
        ///     检查任务是否成功执行：
        ///     
        ///     // 检查任务执行结果
        ///     if (report.Result == XEditor.Tasks.Result.Succeeded)
        ///     {
        ///         // 任务成功执行
        ///         Debug.Log("NPM 任务执行成功");
        ///     }
        ///     else
        ///     {
        ///         // 任务执行失败
        ///         Debug.LogError($"NPM 任务执行失败: {report.Error}");
        ///     }
        /// 
        /// 2.2 获取命令输出
        ///     从任务报告中获取命令执行的详细输出：
        ///     
        ///     // 获取命令执行结果
        ///     var cmdResult = report.Extras as XEditor.Cmd.Result;
        ///     if (cmdResult != null)
        ///     {
        ///         // 输出命令执行的标准输出
        ///         Debug.Log($"命令输出: {cmdResult.Data}");
        ///         
        ///         // 检查退出码
        ///         Debug.Log($"退出码: {cmdResult.Code}");
        ///     }
        /// </code>
        /// 更多信息请参考模块文档。
        /// </remarks>
        public class Npm : Tasks.Worker
        {
            /// <summary>
            /// NPM 命令执行的输出结构。
            /// </summary>
            /// <remarks>
            /// 该结构用于存储 NPM 命令执行的结果信息，包括日志输出和错误状态。
            /// 在内部处理 NPM 命令执行结果时使用。
            /// </remarks>
            internal struct Output
            {
                /// <summary>
                /// NPM 命令执行的日志输出，包含标准输出和错误输出。
                /// </summary>
                /// <remarks>
                /// 包含 NPM 命令执行过程中的标准输出和错误输出。
                /// </remarks>
                internal string Log;

                /// <summary>
                /// NPM 命令执行是否发生错误。
                /// </summary>
                /// <remarks>
                /// 当值为 true 时，表示 NPM 命令执行过程中发生了错误。
                /// </remarks>
                internal bool Error;
            }

            /// <summary>
            /// 获取或设置要执行的 NPM 脚本名称。
            /// </summary>
            /// <remarks>
            /// 该属性对应 package.json 文件中 scripts 部分定义的脚本名称。
            /// 例如，如果 package.json 中有 "scripts": { "build": "webpack" }，
            /// 则可以设置 Script = "build" 来执行该脚本。
            /// </remarks>
            public string Script { get; set; }

            /// <summary>
            /// 获取执行的目录。
            /// </summary>
            public string Cwd { get; set; }

            /// <summary>
            /// 初始化 NPM 脚本执行器的新实例。
            /// </summary>
            /// <param name="id">任务唯一标识符，用于在任务系统中标识该任务</param>
            /// <param name="script">要执行的 NPM 脚本名称，对应 package.json 中的 scripts 定义</param>
            /// <param name="singleton">是否以单例模式运行，避免重复执行，默认为 true</param>
            /// <param name="runasync">是否异步执行，默认为 true</param>
            /// <param name="batchmode">是否在批处理模式下执行，默认为 false</param>
            /// <param name="priority">任务优先级，数值越大优先级越高，默认为 0</param>
            /// <param name="cwd">获取执行的目录，默认为项目根目录</param>
            /// <exception cref="ArgumentNullException">当 id 或 script 为空时抛出</exception>
            /// <remarks>
            /// 创建 NPM 任务实例后，需要通过 XEditor.Tasks.Add 方法将其添加到任务系统中执行，
            /// 或者直接调用 Process 方法立即执行。
            /// </remarks>
            public Npm(string id, string script, bool singleton = true, bool runasync = true, bool batchmode = false, int priority = 0, string cwd = "")
            {
                if (string.IsNullOrEmpty(id)) throw new ArgumentNullException("id");
                if (string.IsNullOrEmpty(script)) throw new ArgumentNullException("script");
                ID = id;
                Script = script;
                Singleton = singleton;
                Runasync = runasync;
                Batchmode = batchmode;
                Priority = priority;
                Cwd = string.IsNullOrEmpty(cwd) ? XEnv.ProjectPath : cwd;
            }

            /// <summary>
            /// 执行 NPM 脚本任务。
            /// </summary>
            /// <param name="report">任务报告对象，用于传递参数和记录执行结果</param>
            /// <exception cref="ArgumentNullException">当 Script 为空时抛出</exception>
            /// <remarks>
            /// <para>执行步骤：</para>
            /// <list type="number">
            /// <item>验证 Script 属性是否有效</item>
            /// <item>刷新 AssetDatabase 以同步资源</item>
            /// <item>构建 NPM 命令参数列表</item>
            /// <item>执行 NPM 命令</item>
            /// <item>处理执行结果，如有错误则记录到 report.Error 中</item>
            /// <item>再次刷新 AssetDatabase 以同步可能的资源变更</item>
            /// </list>
            /// 
            /// <para>参数传递说明：</para>
            /// <list type="bullet">
            /// <item>通过 report.Arguments 传递参数给 NPM 脚本</item>
            /// <item>参数将以 --key=value 的形式传递</item>
            /// <item>如果参数键以 "-" 开头，则会以 -key value 的形式传递</item>
            /// </list>
            /// </remarks>
            public override void Process(Tasks.Report report)
            {
                try
                {
                    if (string.IsNullOrEmpty(Script)) throw new ArgumentNullException("script");

                    XLoom.RunInMain(() => AssetDatabase.Refresh()).Wait(); // Sync assets.

                    var narguments = new List<string>
                    {
                        "run",
                        Script
                    };

                    if (report.Arguments.Count > 0) narguments.Add("--"); // Pass arguments to npm script.

                    foreach (var argument in report.Arguments)
                    {
                        if (argument.Key.StartsWith("-"))
                        {
                            narguments.Add(argument.Key);
                            narguments.Add(argument.Value);
                        }
                        else narguments.Add($"--{argument.Key}={argument.Value}");
                    }

                    var task = Cmd.Run(bin: Cmd.Find("npm"), cwd: Cwd, args: narguments.ToArray());
                    task.Wait();

                    if (task.Result.Code != 0)
                    {
                        report.Error = $"Npm run {Script} error: {task.Result}";
                    }
                    report.Extras = task.Result;
                }
                catch (Exception e)
                {
                    XLog.Panic(e);
                    report.Error = e.Message;
                }
                finally
                {
                    XLoom.RunInMain(() => AssetDatabase.Refresh()).Wait(); // Sync assets again.
                }
            }
        }
    }
}
