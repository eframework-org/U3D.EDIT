// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using EFramework.Utility;

namespace EFramework.Editor
{
    public partial class XEditor
    {
        public partial class Tasks
        {
            /// <summary>
            /// 执行指定的任务。
            /// </summary>
            /// <param name="worker">任务工作者实例</param>
            /// <param name="arguments">任务参数字典</param>
            /// <returns>任务执行报告</returns>
            /// <exception cref="ArgumentNullException">当 worker 为 null 时抛出</exception>
            /// <exception cref="Exception">当任务为单例且已在执行时抛出</exception>
            /// <remarks>
            /// <code>
            /// 执行流程：
            /// 1. 参数验证和准备
            ///    - 检查任务实例有效性
            ///    - 处理单例任务限制
            ///    - 准备执行环境
            /// 
            /// 2. 任务执行阶段
            ///    - 预处理（Preprocess）
            ///    - 处理前置任务（Pre handlers）
            ///    - 主处理过程（Process）
            ///    - 处理后置任务（Post handlers）
            ///    - 后处理（Postprocess）
            /// 
            /// 3. 状态管理
            ///    - 记录执行时间
            ///    - 更新执行状态
            ///    - 处理异常情况
            /// 
            /// 使用示例：
            ///     var worker = new MyTask();
            ///     var args = new Dictionary<string, string> {
            ///         { "param1", "value1" }
            ///     };
            ///     var report = Execute(worker, args);
            ///     if (report.Result == Result.Succeeded) {
            ///         // 处理成功
            ///     }
            /// </code>
            /// </remarks>
            public static Report Execute(IWorker worker, Dictionary<string, string> arguments = null)
            {
                if (worker == null) throw new ArgumentNullException("worker");

                var wtype = worker.GetType();
                var wtag = $"[{worker.ID}]";
                if (worker.Singleton)
                {
                    lock (Singletons)
                    {
                        if (Singletons.Contains(worker.ID)) throw new Exception($"{wtag}: is a singleton task.");
                        else Singletons.Add(worker.ID);
                    }
                }

                var proceed = false;
                Report report = null;
                XLoom.RunInMain(() => { proceed = Prepare(worker, ref arguments, out report); }).Wait();

                void defer()
                {
                    var stack = "";
                    foreach (var phase in report.Phases)
                    {
                        var time = phase.Elapsed < 60 ? $"{phase.Elapsed}s" : $"{phase.Elapsed / 60}min {phase.Elapsed % 60}s";
                        stack += $"\n  [{phase.Name}] [Elapsed: {time}]";
                        if (!string.IsNullOrEmpty(phase.Error)) stack += $" [Error: {phase.Error}]";
                    }
                    if (report.Result == Result.Succeeded)
                    {
                        XLog.Debug("{0}: execute {1} phase(s) succeeded, elapsed {2}, details: {3}",
                            wtag,
                            report.Phases.Count,
                            report.Elapsed < 60 ? $"{report.Elapsed}s" : $"{report.Elapsed / 60}min {report.Elapsed % 60}s",
                            stack);
                    }
                    else
                    {
                        XLog.Error("{0}: execute {1} phase(s) failed with {2} error(s), elapsed {3}, details: {4}",
                            wtag,
                            report.Phases.Count, report.Phases.Count(phase => !string.IsNullOrEmpty(phase.Error)),
                            report.Elapsed < 60 ? $"{report.Elapsed}s" : $"{report.Elapsed / 60}min {report.Elapsed % 60}s",
                            stack);
                    }

                    lock (Singletons)
                    {
                        if (Singletons.Contains(worker.ID)) Singletons.Remove(worker.ID);
                    }

                    if (!worker.Batchmode) XLoom.RunInMain(() =>
                    {
                        EditorUtility.ClearProgressBar();
                    });
                }

                if (proceed == false)
                {
                    report.Task = new Task(() => { });
                    report.Task.RunSynchronously();
                    defer();
                }
                else
                {
                    XLog.Debug("{0}: start to execute task.", wtag);

                    var time = XTime.GetTimestamp();
                    var now = 0;

                    void perform()
                    {
                        try
                        {
                            if (!worker.Batchmode) XLoom.RunInMain(() => EditorUtility.DisplayProgressBar(wtag, "Performing task preprocess...", 0.2f));

                            report.Current = new Phase { Name = $"{worker.ID}/Preprocess" };
                            try { worker.Preprocess(report); } catch (Exception e) { XLog.Panic(e); report.Current.Error = e.Message; }
                            if (report.Current.Result == Result.Unknown) report.Current.Result = string.IsNullOrEmpty(report.Current.Error) ? Result.Succeeded : Result.Failed;
                            if (report.Current.Result != Result.Succeeded) XLog.Error("{0}: {1}", wtag, report.Current.Error);
                            now = XTime.GetTimestamp();
                            report.Current.Elapsed = now - time;
                            time = now;

                            if (report.Current.Result == Result.Succeeded)
                            {
                                List<Type> ipres = new(), iposts = new();
                                Type btype = wtype;
                                // while (btype != null)
                                if (btype != null)
                                {
                                    var tpres = btype.GetCustomAttributes<Pre>();
                                    var tposts = btype.GetCustomAttributes<Post>();
                                    foreach (var tpre in tpres)
                                    {
                                        if (tpre.Handler == null || typeof(Event.Callback).IsAssignableFrom(tpre.Handler) == false) continue;
                                        if (ipres.Contains(tpre.Handler) == false) ipres.Add(tpre.Handler);
                                        else XLog.Warn("{0}: dumplicated pre handler: {1}.", wtag, tpre.Handler.FullName);
                                    }
                                    foreach (var tpost in tposts)
                                    {
                                        if (tpost.Handler == null || typeof(Event.Callback).IsAssignableFrom(tpost.Handler) == false) continue;
                                        if (iposts.Contains(tpost.Handler) == false) iposts.Add(tpost.Handler);
                                        else XLog.Warn("{0}: dumplicated post handler: {1}.", wtag, tpost.Handler.FullName);
                                    }
                                    btype = btype.BaseType;
                                }

                                var sig = true;
                                if (ipres.Count > 0)
                                {
                                    if (!worker.Batchmode) XLoom.RunInMain(() => EditorUtility.DisplayProgressBar(wtag, "Performing task preprocess handler(s)...", 0.3f));

                                    foreach (var ipre in ipres)
                                    {
                                        sig = Handle(ipre, worker, report);
                                        if (sig == false)
                                        {
                                            XLog.Error("{0}: {1}", wtag, report.Current.Error);
                                            break;
                                        }
                                    }
                                }

                                if (sig)
                                {
                                    if (!worker.Batchmode) XLoom.RunInMain(() => EditorUtility.DisplayProgressBar(wtag, "Performing task process...", 0.5f));

                                    report.Current = new Phase { Name = $"{worker.ID}/Process" };
                                    try { worker.Process(report); } catch (Exception e) { XLog.Panic(e); report.Current.Error = e.Message; }
                                    if (report.Current.Result == Result.Unknown) report.Current.Result = string.IsNullOrEmpty(report.Current.Error) ? Result.Succeeded : Result.Failed;
                                    if (report.Current.Result != Result.Succeeded) XLog.Error("{0}: {1}", wtag, report.Current.Error);
                                    now = XTime.GetTimestamp();
                                    report.Current.Elapsed = now - time;
                                    time = now;
                                }

                                if (iposts.Count > 0)
                                {
                                    if (!worker.Batchmode) XLoom.RunInMain(() => EditorUtility.DisplayProgressBar(wtag, "Performing task postprocess handler(s)...", 0.8f));

                                    foreach (var ipost in iposts)
                                    {
                                        if (Handle(ipost, worker, report) == false)
                                        {
                                            XLog.Error("{0}: {1}", wtag, report.Current.Error);
                                        }
                                    }
                                }
                            }

                            if (!worker.Batchmode) XLoom.RunInMain(() => EditorUtility.DisplayProgressBar(wtag, "Performing task postprocess...", 1f));

                            report.Current = new Phase { Name = $"{worker.ID}/Postprocess" };
                            try { worker.Postprocess(report); } catch (Exception e) { XLog.Panic(e); report.Current.Error = e.Message; }
                            if (report.Current.Result == Result.Unknown) report.Current.Result = string.IsNullOrEmpty(report.Current.Error) ? Result.Succeeded : Result.Failed;
                            if (report.Current.Result != Result.Succeeded) XLog.Error("{0}: {1}", wtag, report.Current.Error);
                            now = XTime.GetTimestamp();
                            report.Current.Elapsed = now - time;
                            time = now;
                        }
                        catch (Exception e)
                        {
                            XLog.Panic(e);
                            report.Current.Error = e.Message;
                            now = XTime.GetTimestamp();
                            report.Current.Elapsed = now - time;
                            time = now;
                        }
                    }
                    report.Task = new Task(() =>
                    {
                        perform();
                        defer();
                    });
                    if (worker.Runasync)
                    {
                        report.Task = XLoom.RunAsync(() =>
                        {
                            perform();
                            defer();
                        });
                    }
                    else
                    {
                        report.Task = XLoom.RunInMain(() =>
                        {
                            perform();
                            defer();
                        });
                    }
                }

                return report;
            }

            /// <summary>
            /// 准备任务执行环境。
            /// </summary>
            /// <param name="worker">任务工作者实例</param>
            /// <param name="arguments">任务参数字典，如果为 null 则创建新字典</param>
            /// <param name="report">输出的任务报告</param>
            /// <returns>是否准备成功</returns>
            /// <remarks>
            /// <code>
            /// 准备步骤：
            /// 1. 创建任务报告
            /// 2. 初始化参数字典
            /// 3. 处理字段参数
            ///    - 读取字段特性
            ///    - 设置参数值
            ///    - 处理默认值
            /// 
            /// 参数处理规则：
            /// 1. 优先使用传入的参数值
            /// 2. 如果参数未提供，使用默认值
            /// 3. 记录参数设置错误
            /// </code>
            /// </remarks>
            internal static bool Prepare(IWorker worker, ref Dictionary<string, string> arguments, out Report report)
            {
                report = new Report { Current = new Phase { Name = $"{worker.ID}/Prepare" } };

                arguments ??= new Dictionary<string, string>();
                report.Arguments = arguments;

                var wtype = worker.GetType();
                var fields = wtype.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                foreach (var field in fields)
                {
                    try
                    {
                        var pattr = field.GetCustomAttribute<Param>();
                        if (pattr != null && !string.IsNullOrEmpty(pattr.Name))
                        {
                            if (arguments.TryGetValue(pattr.Name, out var fvalue))
                            {
                                field.SetValue(worker, fvalue);
                            }
                            else if (!string.IsNullOrEmpty(pattr.Default))
                            {
                                field.SetValue(worker, pattr.Default);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        XLog.Panic(e);
                        report.Error = $"Set worker param: {field.Name} with error: {e.Message}";
                        return false;
                    }
                }

                report.Result = Result.Succeeded;

                return true;
            }

            /// <summary>
            /// 处理任务的预处理或后处理。
            /// </summary>
            /// <param name="type">处理器类型</param>
            /// <param name="worker">任务工作者实例</param>
            /// <param name="report">任务报告</param>
            /// <returns>是否处理成功</returns>
            /// <exception cref="ArgumentNullException">当任何参数为 null 时抛出</exception>
            /// <remarks>
            /// <code>
            /// 处理流程：
            /// 1. 参数验证
            /// 2. 执行处理器
            /// 3. 记录执行时间
            /// 4. 更新执行状态
            /// 5. 处理异常情况
            /// 
            /// 状态处理：
            /// - Unknown：初始状态
            /// - Succeeded：执行成功且无错误
            /// - Failed：执行失败或有错误
            /// </code>
            /// </remarks>
            internal static bool Handle(Type type, IWorker worker, Report report)
            {
                if (type == null) throw new ArgumentNullException("type");
                if (worker == null) throw new ArgumentNullException("worker");
                if (report == null) throw new ArgumentNullException("report");

                try
                {
                    var time = XTime.GetTimestamp();
                    report.Current = new Phase { Name = $"{worker.ID}/{type.Name}" };
                    Event.Notify(type, worker, report);
                    report.Current.Elapsed = XTime.GetTimestamp() - time;
                    if (report.Current.Result == Result.Unknown) report.Current.Result = string.IsNullOrEmpty(report.Current.Error) ? Result.Succeeded : Result.Failed;
                    if (report.Current.Result == Result.Failed) return false;
                }
                catch (Exception e)
                {
                    XLog.Panic(e);
                    report.Error = e.Message;
                    return false;
                }

                return true;
            }
        }

        public partial class Tasks
        {
            /// <summary>
            /// 批量任务执行器，支持命令行批处理模式。
            /// </summary>
            /// <remarks>
            /// <code>
            /// 功能特性
            /// - 支持多任务批量执行
            /// - 提供命令行参数解析
            /// - 支持任务参数配置
            /// - 处理执行结果报告
            /// 
            /// 命令行参数
            /// - runTasks：启用批处理模式
            /// - taskID：指定任务标识
            /// - runAsync：设置异步执行
            /// - taskResults：结果文件路径
            /// 
            /// 使用示例
            /// Unity.exe -batchmode -projectPath /path/to/project 
            ///          -runTasks 
            ///          -taskID "MyTask" 
            ///          -param1 "value1" 
            ///          -taskResults "results.json"
            /// </code>
            /// </remarks>
            internal class Batch : Event.Internal.OnEditorLoad
            {
                /// <summary>
                /// 事件处理优先级。
                /// </summary>
                int Event.Callback.Priority => 10000;

                /// <summary>
                /// 是否为单例事件处理器。
                /// </summary>
                bool Event.Callback.Singleton => true;

                async void Event.Internal.OnEditorLoad.Process(params object[] _) { await Process(); }

                /// <summary>
                /// 处理批量任务执行。
                /// </summary>
                /// <remarks>
                /// <code>
                /// 执行流程：
                /// 1. 参数解析
                ///    - 检查批处理模式
                ///    - 解析任务参数
                ///    - 处理执行选项
                /// 
                /// 2. 任务准备
                ///    - 查找任务实例
                ///    - 配置执行参数
                ///    - 验证任务有效性
                /// 
                /// 3. 任务执行
                ///    - 按序执行任务
                ///    - 收集执行报告
                ///    - 等待任务完成
                /// 
                /// 4. 结果处理
                ///    - 保存执行报告
                ///    - 处理执行异常
                ///    - 设置退出代码
                /// </code>
                /// </remarks>
                internal async Task Process()
                {
                    var tasks = new List<string>();
                    var taskAsyncs = new List<int>();
                    var taskParams = new List<Dictionary<string, string>>(); // 为每个任务存储独立的参数
                    var resultFile = "";

                    var args = XEnv.GetArgs();
                    var batchMode = Application.isBatchMode;
                    var testMode = args.Exists(kvp => kvp.Key == "runTests");
                    if (!args.Exists(kvp => kvp.Key == "runTasks")) return;
                    else
                    {
                        // 解析任务相关参数
                        var btask = false;
                        var tempParams = new Dictionary<string, string>(); // 当前任务的参数集合

                        foreach (var kvp in args)
                        {
                            var name = kvp.Key;
                            var value = kvp.Value;

                            if (name == "taskID")
                            {
                                if (btask) // 如果已经有一个任务在处理中，保存其参数
                                {
                                    taskParams.Add(new Dictionary<string, string>(tempParams));
                                    tempParams.Clear();
                                }

                                tasks.Add(value);
                                taskAsyncs.Add(-1);
                                btask = true;
                            }
                            else if (name == "runAsync" && btask)
                            {
                                taskAsyncs[tasks.Count - 1] = 1;
                                tempParams[name] = value; // 保存到当前任务参数
                            }
                            else if (name == "taskResults")
                            {
                                resultFile = value;
                            }
                            else if (btask && name != "runTasks" && name != "projectPath" && name != "batchMode")
                            {
                                // 将参数添加到当前任务的参数集合中
                                tempParams[name] = value;
                            }
                        }

                        // 保存最后一个任务的参数
                        if (btask && tasks.Count > taskParams.Count)
                        {
                            taskParams.Add(new Dictionary<string, string>(tempParams));
                        }
                    }

                    var proceed = false;
                    var workers = new List<IWorker>();
                    try
                    {
                        foreach (var task in tasks)
                        {
                            var sig = false;
                            foreach (var kvp in Workers)
                            {
                                var meta = kvp.Key;
                                var worker = kvp.Value;
                                if (worker.ID == task)
                                {
                                    // if (workers.Contains(worker)) throw new Exception($"XEditor.Tasks.Batch: dumplicated task of {task}.");
                                    var sasync = taskAsyncs[workers.Count];
                                    if (sasync == -1) taskAsyncs[workers.Count] = meta.Runasync ? 1 : 0;
                                    // 初始化参数
                                    var tempParams = taskParams[workers.Count];
                                    foreach (var param in meta.Params)
                                    {
                                        if (tempParams.ContainsKey(param.Name)) continue;
                                        else if (param.Persist) tempParams[param.Name] = XPrefs.GetString(param.ID, param.Default);
                                        else tempParams[param.Name] = param.Default;
                                    }
                                    workers.Add(worker);
                                    sig = true;
                                    break;
                                }
                            }
                            if (sig == false) throw new Exception($"XEditor.Tasks.Batch: task of {task} was not found.");
                        }
                        proceed = true;
                    }
                    catch (Exception e)
                    {
                        XLog.Panic(e);
                        if (batchMode && !testMode) EditorApplication.Exit(1);
                    }
                    if (!proceed) return;

                    try
                    {
                        var reports = new Dictionary<string, Report>();

                        for (var i = 0; i < workers.Count; i++)
                        {
                            var worker = workers[i];
                            var meta = Workers.First(kvp => kvp.Value == worker).Key;
                            worker.Runasync = taskAsyncs[i] == 1;
                            try
                            {
                                reports[worker.ID] = Execute(worker, arguments: taskParams[i]);
                            }
                            catch (Exception e)
                            {
                                XLog.Panic(e);
                                if (batchMode && !testMode) EditorApplication.Exit(1);
                            }
                        }

                        // 使用 await 避免阻塞主线程
                        foreach (var pairs in reports) await pairs.Value.Task;

                        if (string.IsNullOrEmpty(resultFile)) XLog.Warn("XEditor.Tasks.Batch: report file path is null.");
                        else
                        {
                            var dir = Path.GetDirectoryName(resultFile);
                            if (!XFile.HasDirectory(dir)) XFile.CreateDirectory(dir);

                            XFile.SaveText(resultFile, XObject.ToJson(reports, true));
                        }

                        var succeeded = 0;
                        var failed = 0;
                        foreach (var pairs in reports)
                        {
                            if (pairs.Value.Result == Result.Succeeded) succeeded++;
                            else failed++;
                        }

                        XLog.Debug("XEditor.Tasks.Batch: finish to execute {0} task(s) with {1} succeeded and {2} failed.", workers.Count, succeeded, failed);
                        if (batchMode && !testMode) EditorApplication.Exit(failed > 0 ? 1 : 0);
                    }
                    catch (Exception e)
                    {
                        XLog.Panic(e);
                        XLog.Error("XEditor.Tasks.Batch: execute {0} task(s) with error: {1}", workers.Count, e.Message);
                        if (batchMode && !testMode) EditorApplication.Exit(1);
                    }
                }
            }
        }
    }
}
