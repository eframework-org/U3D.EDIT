// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using EFramework.Utility;
using EFramework.Editor;

/// <summary>
/// XEditor.Tasks.Run 模块的单元测试类
/// 
/// 功能特性
/// 1. 任务执行系统测试
///    - 同步/异步任务执行机制
///    - 单例任务管理
///    - 处理器回调时序
///    - 错误处理机制
/// 
/// 2. 批处理系统测试
///    - 命令行参数解析
///    - 任务查找和执行
///    - 参数读取优先级
///    - 结果输出处理
/// 
/// 执行流程
/// 1. 基础任务执行测试
///    - 同步任务执行验证
///    - 异步任务执行验证
///    - 单例任务限制验证
/// 
/// 2. 处理器机制测试
///    - 处理器执行时序验证
///    - 错误处理和中断验证
///    - 状态管理验证
/// 
/// 3. 批处理功能测试
///    - 参数解析验证
///    - 多任务执行验证
///    - 结果输出验证
/// </summary>
public class TestXEditorTasksRun
{
    #region Test Class and Handlers

    /// <summary>
    /// 测试用任务类
    /// 
    /// 功能特性
    /// 1. 支持同步/异步执行配置
    /// 2. 支持单例模式设置
    /// 3. 包含Pre/Post处理器
    /// 4. 支持自定义参数传递
    /// </summary>
    [XEditor.Tasks.Pre(typeof(TestPreHandler1))]
    [XEditor.Tasks.Pre(typeof(TestPreHandler2))]
    [XEditor.Tasks.Post(typeof(TestPostHandler1))]
    [XEditor.Tasks.Post(typeof(TestPostHandler2))]
    public class TestTask : XEditor.Tasks.Worker
    {
        [XEditor.Tasks.Param("Test Param", defval: "Test Value")]
        internal string testParam;

        internal int testThread = -1;

        public override void Process(XEditor.Tasks.Report report)
        {
            report.Extras = testParam;
            testThread = Thread.CurrentThread.ManagedThreadId;
            if (Runasync) try { Thread.Sleep(1000); } catch { }
        }
    }

    /// <summary>
    /// Pre处理器接口和实现类定义
    /// PreHandler1: 优先级0，单例模式
    /// PreHandler2: 优先级1，非单例
    /// </summary>
    internal interface TestPreHandler1 : XEditor.Event.Callback { void Process(params object[] args); }

    public class TestPreProcessor1 : TestPreHandler1
    {
        public int Priority => 0;

        public bool Singleton => true;

        internal static TestPreProcessor1 Instance = new();

        internal static TestPreProcessor1 instance;

        internal static XEditor.Tasks.Worker worker;

        internal static XEditor.Tasks.Report report;

        internal static bool panic;

        void TestPreHandler1.Process(params object[] args)
        {
            instance = this;
            XEditor.Event.Decode(out worker, out report, args);
            if (panic) report.Error = "Error occurred in TestPreprocessor1";
        }
    }

    internal interface TestPreHandler2 : XEditor.Event.Callback { void Process(params object[] args); }

    public class TestPreProcessor2 : TestPreHandler2
    {
        public int Priority => 1;

        public bool Singleton => false;

        internal static XEditor.Tasks.Worker worker;

        internal static XEditor.Tasks.Report report;

        internal static bool panic;

        void TestPreHandler2.Process(params object[] args)
        {
            XEditor.Event.Decode(out worker, out report, args);
            if (panic) report.Error = "Error occurred in TestPreProcessor2";
        }
    }

    /// <summary>
    /// Post处理器接口和实现类定义
    /// PostHandler1: 优先级0，单例模式
    /// PostHandler2: 优先级1，非单例
    /// </summary>
    internal interface TestPostHandler1 : XEditor.Event.Callback { void Process(params object[] args); }

    public class TestPostProcessor1 : TestPostHandler1
    {
        public int Priority => 0;

        public bool Singleton => true;

        internal static TestPostProcessor1 Instance = new();

        internal static TestPostProcessor1 instance;

        internal static XEditor.Tasks.Worker worker;

        internal static XEditor.Tasks.Report report;

        internal static bool panic;

        void TestPostHandler1.Process(params object[] args)
        {
            instance = this;
            XEditor.Event.Decode(out worker, out report, args);
            if (panic) report.Error = "Error occurred in TestPostProcessor1";
        }
    }

    internal interface TestPostHandler2 : XEditor.Event.Callback { void Process(params object[] args); }

    public class TestPostProcessor2 : TestPostHandler2
    {
        public int Priority => 1;

        public bool Singleton => false;

        internal static XEditor.Tasks.Worker worker;

        internal static XEditor.Tasks.Report report;

        internal static bool panic;

        void TestPostHandler2.Process(params object[] args)
        {
            XEditor.Event.Decode(out worker, out report, args);
            if (panic) report.Error = "Error occurred in TestPostProcessor2";
        }
    }

    #endregion

    #region Test Cases

    [SetUp]
    public void Setup()
    {
        // 重置处理器状态
        TestPreProcessor1.panic = false;
        TestPreProcessor2.panic = false;
        TestPostProcessor1.panic = false;
        TestPostProcessor2.panic = false;
    }

    /// <summary>
    /// 测试同步任务执行
    /// 
    /// 验证内容：
    /// 1. 任务在主线程执行
    /// 2. 参数正确传递
    /// 3. 执行结果正确返回
    /// 
    /// 执行流程：
    /// 1. 创建同步任务实例
    /// 2. 设置测试参数
    /// 3. 执行任务
    /// 4. 验证执行结果
    /// </summary>
    [Test]
    public void Sync()
    {
        // 创建任务
        var worker = new TestTask
        {
            ID = "Test/TestTask",
            Runasync = false,
            Batchmode = Application.isBatchMode
        };

        // 执行任务
        var args = new Dictionary<string, string> { { "Test Param", "test" } }; // 携带和字段同名的参数
        var report = XEditor.Tasks.Execute(worker, args);
        report.Task.Wait();

        // 验证结果
        Assert.That(worker.testThread, Is.EqualTo(Thread.CurrentThread.ManagedThreadId));
        Assert.That(report.Result, Is.EqualTo(XEditor.Tasks.Result.Succeeded));
        Assert.That(report.Arguments["Test Param"], Is.EqualTo("test"));
        Assert.That(report.Extras.ToString(), Is.EqualTo("test"));
    }

    /// <summary>
    /// 测试异步任务执行
    /// 
    /// 验证内容：
    /// 1. 任务在工作线程执行
    /// 2. 异步执行成功完成
    /// 3. 默认参数正确应用
    /// 
    /// 执行流程：
    /// 1. 创建异步任务实例
    /// 2. 执行任务
    /// 3. 等待任务完成
    /// 4. 验证执行结果
    /// </summary>
    [Test]
    public void Async()
    {
        // 创建任务
        var worker = new TestTask
        {
            ID = "Test/TestTask",
            Runasync = true,
            Batchmode = Application.isBatchMode
        };

        // 执行任务
        var report = XEditor.Tasks.Execute(worker);
        report.Task.Wait();

        // 验证结果
        Assert.That(worker.testThread, Is.Not.EqualTo(Thread.CurrentThread.ManagedThreadId));
        Assert.That(report.Result, Is.EqualTo(XEditor.Tasks.Result.Succeeded));
        Assert.That(worker.testParam, Is.EqualTo("Test Value")); // 包含默认值的自定义参数
        Assert.That(report.Extras, Is.EqualTo(worker.testParam));
    }

    /// <summary>
    /// 测试单例任务管理
    /// 
    /// 验证内容：
    /// 1. 单例任务首次执行成功
    /// 2. 重复执行时抛出异常
    /// 3. 异常信息符合预期
    /// 
    /// 执行流程：
    /// 1. 创建单例任务实例
    /// 2. 首次执行任务
    /// 3. 尝试重复执行
    /// 4. 验证异常处理
    /// </summary>
    [Test]
    public void Singleton()
    {
        // 创建任务
        var worker = new TestTask
        {
            ID = "Test/TestTask",
            Runasync = true,
            Singleton = true,
            Batchmode = Application.isBatchMode
        };

        // 执行第一个任务
        var report = XEditor.Tasks.Execute(worker);

        // 验证第二次执行会抛出单例任务异常
        var ex = Assert.Throws<Exception>(() => XEditor.Tasks.Execute(worker));
        Assert.That(ex.Message, Is.EqualTo("[Test/TestTask]: is a singleton task."));

        // 验证第一个任务成功
        report.Task.Wait();
        Assert.That(report.Result, Is.EqualTo(XEditor.Tasks.Result.Succeeded), "First task should succeed");
    }

    /// <summary>
    /// 测试处理器执行机制
    /// 
    /// 验证内容：
    /// 1. 处理器执行时序（按Priority排序）
    /// 2. 单例处理器实例管理
    /// 3. Pre处理器错误中断后续执行
    /// 4. Post处理器错误不影响执行完整性
    /// 
    /// 执行流程：
    /// 1. 正常执行流程测试
    ///    - 验证处理器执行顺序
    ///    - 验证单例处理器状态
    /// 2. Pre处理器错误测试
    ///    - 触发Pre处理器错误
    ///    - 验证执行中断
    /// 3. Post处理器错误测试
    ///    - 触发Post处理器错误
    ///    - 验证错误处理
    /// </summary>
    [Test]
    public void Handler()
    {
        // 重置处理器状态
        TestPreProcessor1.instance = null;
        TestPreProcessor2.worker = null;
        TestPostProcessor1.instance = null;
        TestPostProcessor2.worker = null;
        TestPreProcessor1.panic = false;
        TestPreProcessor2.panic = false;
        TestPostProcessor1.panic = false;
        TestPostProcessor2.panic = false;
        // 创建任务
        var worker = new TestTask
        {
            ID = "Test/TestTask",
            Runasync = false,
            Batchmode = Application.isBatchMode
        };
        // 测试正常执行时的回调时序
        var report = XEditor.Tasks.Execute(worker);
        report.Task.Wait();

        // 验证处理器执行顺序（按Priority排序）和单例特性
        Assert.That(report.Result, Is.EqualTo(XEditor.Tasks.Result.Succeeded));
        Assert.That(report.Phases.Count, Is.EqualTo(8), "Should have 8 phases"); // Prepare + Preprocess + Pre1 + Pre2 + Process + Post1 + Post2 + Postprocess

        // 验证阶段执行顺序
        Assert.That(report.Phases[0].Name, Does.EndWith("Prepare"));
        Assert.That(report.Phases[1].Name, Does.EndWith("Preprocess"));
        Assert.That(report.Phases[2].Name, Does.EndWith("TestPreHandler1"));
        Assert.That(report.Phases[3].Name, Does.EndWith("TestPreHandler2"));
        Assert.That(report.Phases[4].Name, Does.EndWith("Process"));
        Assert.That(report.Phases[5].Name, Does.EndWith("TestPostHandler1"));
        Assert.That(report.Phases[6].Name, Does.EndWith("TestPostHandler2"));
        Assert.That(report.Phases[7].Name, Does.EndWith("Postprocess"));

        // 验证单例和调用状态
        Assert.That(TestPreProcessor1.instance, Is.Not.Null);
        Assert.That(TestPreProcessor2.worker, Is.EqualTo(worker));
        Assert.That(TestPostProcessor1.instance, Is.Not.Null);
        Assert.That(TestPostProcessor2.worker, Is.EqualTo(worker));

        // 测试Pre处理器异常中断
        LogAssert.Expect(LogType.Error, new Regex("Error occurred in"));
        LogAssert.Expect(LogType.Error, new Regex("execute \\d+ phase\\(s\\) failed with \\d+ error\\(s\\)"));
        TestPreProcessor1.panic = true;
        report = XEditor.Tasks.Execute(worker);
        report.Task.Wait();

        // 验证Pre处理器异常后的状态
        Assert.That(report.Result, Is.EqualTo(XEditor.Tasks.Result.Failed));
        Assert.That(report.Error, Is.Not.Null);
        Assert.That(report.Phases.Count, Is.LessThan(8));
        Assert.That(report.Phases[2].Name, Does.EndWith("TestPreHandler1"));
        Assert.That(report.Phases[2].Error, Is.Not.Null);

        // 重置状态
        TestPreProcessor1.panic = false;
        TestPostProcessor1.panic = true;

        // 测试Post处理器异常
        LogAssert.Expect(LogType.Error, new Regex("Error occurred in"));
        LogAssert.Expect(LogType.Error, new Regex("execute \\d+ phase\\(s\\) failed with \\d+ error\\(s\\)"));
        report = XEditor.Tasks.Execute(worker);
        report.Task.Wait();

        // 验证Post处理器异常后的状态
        Assert.That(report.Result, Is.EqualTo(XEditor.Tasks.Result.Failed));
        Assert.That(report.Error, Is.Not.Null);
        Assert.That(report.Phases.Count, Is.EqualTo(8));
        Assert.That(report.Phases[5].Name, Does.EndWith("TestPostHandler1"));
        Assert.That(report.Phases[5].Error, Is.Not.Null);
    }

#if UNITY_6000_0_OR_NEWER
    /// <summary>
    /// 测试批处理任务执行
    /// 
    /// 验证内容：
    /// 1. 命令行参数解析
    /// 2. 任务查找和执行
    /// 3. 参数读取优先级（命令行 > XPrefs > 默认值）
    /// 4. 结果输出处理
    /// 
    /// 测试场景：
    /// - Single: 单任务执行
    /// - Mixed: 混合同步异步任务
    /// - Sync: 全同步任务
    /// - Async: 全异步任务
    /// - Nonexist: 不存在的任务
    /// - Params: 参数优先级
    /// </summary>
    [TestCase("Single")]
    [TestCase("Mixed")]
    [TestCase("Sync")]
    [TestCase("Async")]
    [TestCase("Nonexist")]
    [TestCase("Params")]
    public async Task Batch(string testCase)
    {
        // 准备测试任务
        var task1 = new TestTask { ID = "Test/Test Task1", Runasync = false };
        var task1Meta = new XEditor.Tasks.WorkerAttribute("Test Task1", "Test", "Test Task 1")
        {
            Params = new List<XEditor.Tasks.Param> {
                new("Test Param", "Test Param", "Test Value") {
                    ID = $"Task/{XEnv.Platform}/{task1.ID}/Test Param@Editor"
                } }
        };
        XEditor.Tasks.Workers[task1Meta] = task1;

        var task2 = new TestTask { ID = "Test/Test Task2", Runasync = true };
        var task2Meta = new XEditor.Tasks.WorkerAttribute("Test Task2", "Test", "Test Task 2")
        {
            Params = new List<XEditor.Tasks.Param> {
                new("Test Param", "Test Param", "Test Value") {
                    ID = $"Task/{XEnv.Platform}/{task2.ID}/Test Param@Editor"
                } }
        };
        XEditor.Tasks.Workers[task2Meta] = task2;

        var batchResultDir = XFile.PathJoin(XEnv.ProjectPath, "Temp", "TestXEditorTasksRun");
        if (!XFile.HasDirectory(batchResultDir)) XFile.CreateDirectory(batchResultDir);

        var batchReport = XFile.PathJoin(batchResultDir, $"BatchReport-{testCase}.json");
        var batchHandler = new XEditor.Tasks.Batch();

        try
        {
            switch (testCase)
            {
                case "Single":
                    {
                        XEnv.ParseArgs(reset: true, new[] {
                            "-runTasks",
                            "-taskResults", batchReport,
                            "-taskID", "Test/Test Task1",
                            "--Test Param=test1"
                        });

                        await batchHandler.Process();

                        Assert.That(task1.testThread, Is.EqualTo(Thread.CurrentThread.ManagedThreadId));
                        Assert.That(task1.testParam, Is.EqualTo("test1"));
                        Assert.That(XFile.HasFile(batchReport), Is.True);
                    }
                    break;
                case "Mixed":
                    {
                        XEnv.ParseArgs(reset: true, new[] {
                            "-runTasks",
                            "-taskResults", batchReport,
                            "-taskID", "Test/Test Task1",
                            "--Test Param=test1",
                            "-taskID", "Test/Test Task2",
                            "-runAsync",
                            "--Test Param=test2"
                        });

                        task1.testThread = -1;
                        task2.testThread = -1;

                        await batchHandler.Process();

                        Assert.That(task1.testThread, Is.EqualTo(Thread.CurrentThread.ManagedThreadId));
                        Assert.That(task2.testThread, Is.Not.EqualTo(Thread.CurrentThread.ManagedThreadId));
                        Assert.That(task1.testParam, Is.EqualTo("test1"));
                        Assert.That(task2.testParam, Is.EqualTo("test2"));
                        Assert.That(XFile.HasFile(batchReport), Is.True);
                    }
                    break;
                case "Sync":
                    {
                        XEnv.ParseArgs(reset: true, new[] {
                            "-runTasks",
                            "-taskResults", batchReport,
                            "-taskID", "Test/Test Task1",
                            "--Test Param=sync1",
                            "-taskID", "Test/Test Task2",
                            "--Test Param=sync2"
                        });

                        task1.testThread = -1;
                        task2.testThread = -1;

                        await batchHandler.Process();

                        Assert.That(task1.testThread, Is.EqualTo(Thread.CurrentThread.ManagedThreadId));
                        Assert.That(task2.testThread, Is.EqualTo(Thread.CurrentThread.ManagedThreadId));
                        Assert.That(task1.testParam, Is.EqualTo("sync1"));
                        Assert.That(task2.testParam, Is.EqualTo("sync2"));
                        Assert.That(XFile.HasFile(batchReport), Is.True);
                    }
                    break;
                case "Async":
                    {
                        XEnv.ParseArgs(reset: true, new[] {
                            "-runTasks",
                            "-taskResults", batchReport,
                            "-taskID", "Test/Test Task1",
                            "-runAsync",
                            "--Test Param=async1",
                            "-taskID", "Test/Test Task2",
                            "-runAsync",
                            "--Test Param=async2"
                        });

                        task1.testThread = -1;
                        task2.testThread = -1;

                        await batchHandler.Process();

                        var mainThreadId = Thread.CurrentThread.ManagedThreadId;
                        Assert.That(task1.testThread, Is.Not.EqualTo(mainThreadId));
                        Assert.That(task2.testThread, Is.Not.EqualTo(mainThreadId));
                        Assert.That(task1.testParam, Is.EqualTo("async1"));
                        Assert.That(task2.testParam, Is.EqualTo("async2"));
                        Assert.That(XFile.HasFile(batchReport), Is.True);
                    }
                    break;
                case "Nonexist":
                    {
                        XEnv.ParseArgs(reset: true, new[] {
                            "-runTasks",
                            "-taskID", "Test/NonExistTask"
                        });

                        LogAssert.Expect(LogType.Exception, new Regex("XEditor.Tasks.Batch: task of .* was not found."));
                        await batchHandler.Process();
                    }
                    break;
                case "Params":
                    {
                        // 设置 XPrefs 中的参数值
                        XPrefs.Asset.Set(task1Meta.Params[0].ID, "prefs1");
                        XPrefs.Asset.Set(task2Meta.Params[0].ID, "prefs2");

                        // 测试场景1：完全使用 XPrefs 值
                        XEnv.ParseArgs(reset: true, new[] {
                            "-runTasks",
                            "-taskResults", batchReport,
                            "-taskID", "Test/Test Task1",
                            "-taskID", "Test/Test Task2"
                        });

                        await batchHandler.Process();

                        Assert.That(task1.testParam, Is.EqualTo("prefs1"), "Should use value from XPrefs");
                        Assert.That(task2.testParam, Is.EqualTo("prefs2"), "Should use value from XPrefs");

                        // 测试场景2：命令行参数覆盖 XPrefs 值
                        XEnv.ParseArgs(reset: true, new[] {
                            "-runTasks",
                            "-taskResults", batchReport,
                            "-taskID", "Test/Test Task1",
                            "--Test Param=cmd1",
                            "-taskID", "Test/Test Task2",
                            "--Test Param=cmd2"
                        });

                        await batchHandler.Process();

                        Assert.That(task1.testParam, Is.EqualTo("cmd1"), "Command line should override XPrefs");
                        Assert.That(task2.testParam, Is.EqualTo("cmd2"), "Command line should override XPrefs");

                        // 测试场景3：混合使用（一个用命令行，一个用XPrefs）
                        XEnv.ParseArgs(reset: true, new[] {
                            "-runTasks",
                            "-taskResults", batchReport,
                            "-taskID", "Test/Test Task1",
                            "--Test Param=cmd1",
                            "-taskID", "Test/Test Task2"
                        });

                        await batchHandler.Process();

                        Assert.That(task1.testParam, Is.EqualTo("cmd1"), "Should use command line value");
                        Assert.That(task2.testParam, Is.EqualTo("prefs2"), "Should fallback to XPrefs value");

                        // 清理 XPrefs 测试数据
                        XPrefs.Asset.Unset(task1Meta.Params[0].ID);
                        XPrefs.Asset.Unset(task2Meta.Params[0].ID);
                    }
                    break;
            }
        }
        finally
        {
            // 清理测试环境
            if (XEditor.Tasks.Workers.ContainsKey(task1Meta)) XEditor.Tasks.Workers.Remove(task1Meta);
            if (XEditor.Tasks.Workers.ContainsKey(task2Meta)) XEditor.Tasks.Workers.Remove(task2Meta);
            if (XFile.HasFile(batchReport)) XFile.DeleteFile(batchReport);
            if (XFile.HasDirectory(batchResultDir)) XFile.DeleteDirectory(batchResultDir);

            // 恢复原始环境
            XEnv.ParseArgs(reset: true);
        }
    }
#endif
    #endregion
}
#endif
