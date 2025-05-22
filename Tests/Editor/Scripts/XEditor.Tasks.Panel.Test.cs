// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using EFramework.Editor;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using EFramework.Utility;

/// <summary>
/// XEditor.Tasks.Panel 模块的单元测试类。
/// </summary>
/// <remarks>
/// <code>
/// 测试范围
/// 1. 面板数据管理
///    - 数据结构重置
///    - 任务分组排序
///    - 状态持久化处理
/// 
/// 2. 生命周期管理
///    - OnEnable 回调处理
///    - OnDisable 回调处理
///    - OnDestroy 回调处理
/// 
/// 3. 任务执行机制
///    - 同步任务执行
///    - 异步任务执行
///    - 多任务混合执行
///    - 参数传递机制
/// 
/// 测试策略
/// 1. 隔离测试：每个测试用例独立运行，不互相影响
/// 2. 环境清理：测试前后恢复系统状态
/// 3. 异常处理：验证错误场景的处理
/// 4. 状态验证：确保所有状态变更符合预期
/// </code>
/// </remarks>
public class TestXEditorTasksPanel
{
    #region Test Class and Handlers

    /// <summary>
    /// 测试用任务类，用于验证面板功能。
    /// </summary>
    /// <remarks>
    /// 功能特性
    /// - 实现所有面板回调接口
    /// - 记录回调执行状态
    /// - 提供参数传递验证
    /// - 支持执行状态跟踪
    /// 
    /// 使用方式
    /// 1. 创建实例并配置
    /// 2. 注册到任务系统
    /// 3. 执行相关测试
    /// 4. 验证执行结果
    /// </remarks>
    public class TestVisualTask : XEditor.Tasks.Worker,
        XEditor.Tasks.Panel.IOnEnable,
        XEditor.Tasks.Panel.IOnGUI,
        XEditor.Tasks.Panel.IOnDisable,
        XEditor.Tasks.Panel.IOnDestroy
    {
        /// <summary>OnEnable 回调是否被调用</summary>
        internal static bool onEnableCalled;
        /// <summary>OnGUI 回调是否被调用</summary>
        internal static bool onGUICalled;
        /// <summary>OnDisable 回调是否被调用</summary>
        internal static bool onDisableCalled;
        /// <summary>OnDestroy 回调是否被调用</summary>
        internal static bool onDestroyCalled;
        /// <summary>最后接收到的参数值</summary>
        internal static string lastParam;
        /// <summary>任务是否被执行</summary>
        internal static bool executed;

        /// <summary>
        /// 任务处理逻辑。
        /// </summary>
        /// <param name="report">任务执行报告，包含参数和状态信息</param>
        /// <remarks>
        /// 执行步骤：
        /// 1. 记录执行状态
        /// 2. 保存接收到的参数
        /// </remarks>
        public override void Process(XEditor.Tasks.Report report)
        {
            executed = true;
            if (report.Arguments != null && report.Arguments.ContainsKey("testParam"))
            {
                lastParam = report.Arguments["testParam"];
            }
        }

        // 面板生命周期回调实现
        void XEditor.Tasks.Panel.IOnEnable.OnEnable() { onEnableCalled = true; }
        void XEditor.Tasks.Panel.IOnGUI.OnGUI() { onGUICalled = true; }
        void XEditor.Tasks.Panel.IOnDisable.OnDisable() { onDisableCalled = true; }
        void XEditor.Tasks.Panel.IOnDestroy.OnDestroy() { onDestroyCalled = true; }

        /// <summary>
        /// 重置所有静态状态。
        /// </summary>
        /// <remarks>
        /// 在每个测试用例执行前调用，确保测试环境的干净。
        /// 重置内容：
        /// - 回调状态标记
        /// - 参数记录
        /// - 执行状态标记
        /// </remarks>
        internal static void Reset()
        {
            onEnableCalled = false;
            onGUICalled = false;
            onDisableCalled = false;
            onDestroyCalled = false;
            lastParam = null;
            executed = false;
        }
    }

    #endregion

    #region Test Cases

    /// <summary>
    /// 测试面板重置功能。
    /// </summary>
    /// <remarks>
    /// 测试内容：
    /// 1. 数据结构重置
    ///    - 验证所有集合正确清空
    ///    - 验证默认状态正确设置
    /// 
    /// 2. 任务分组重组织
    ///    - 验证分组正确创建
    ///    - 验证组内任务排序
    ///    - 验证分组间排序
    /// 
    /// 3. 状态一致性
    ///    - 验证面板状态标记
    ///    - 验证任务状态对应
    /// </remarks>
    [Test]
    public void Sort()
    {
        // 准备测试任务
        var task1 = new TestVisualTask { ID = "Test Group1/Test Visual Task1", Priority = 1 };
        var task1Meta = new XEditor.Tasks.WorkerAttribute("Test Visual Task1", "Test Group1", "Test Visual Task 1");
        XEditor.Tasks.Workers[task1Meta] = task1;

        var task2 = new TestVisualTask { ID = "Test Group2/Test Visual Task2", Priority = 0 };
        var task2Meta = new XEditor.Tasks.WorkerAttribute("Test Visual Task2", "Test Group2", "Test Visual Task 2");
        XEditor.Tasks.Workers[task2Meta] = task2;

        var task3 = new TestVisualTask { ID = "Test Group2/Test Visual Task3", Priority = 2 };
        var task3Meta = new XEditor.Tasks.WorkerAttribute("Test Visual Task3", "Test Group2", "Test Visual Task 3");
        XEditor.Tasks.Workers[task3Meta] = task3;

        var panel = ScriptableObject.CreateInstance<TasksPanel>();

        try
        {
            // 添加一些初始数据
            panel.taskArguments[task1Meta] = new Dictionary<XEditor.Tasks.Param, string>();
            panel.groupFoldouts["Test Group1"] = true;
            panel.groupSelects["Test Group1"] = true;
            panel.taskFoldouts[task1Meta] = true;
            panel.taskSelects[task1Meta] = true;
            panel.taskOrders.Add(task1Meta);

            // 执行重置
            panel.OnEnable();

            // 验证数据结构重置
            // 验证数据清空
            Assert.That(panel.taskArguments, Is.Empty);
            Assert.That(panel.groupFoldouts, Is.Empty);
            Assert.That(panel.groupSelects, Is.Empty);
            Assert.That(panel.taskFoldouts, Is.Empty);
            Assert.That(panel.taskSelects, Is.Empty);
            Assert.That(panel.taskOrders, Is.Empty);

            // 执行重置
            panel.OnEnable();

            // 验证任务分组
            var group1 = panel.taskGroups.FirstOrDefault(g => g[0].Group == "Test Group1");
            var group2 = panel.taskGroups.FirstOrDefault(g => g[0].Group == "Test Group2");

            // 验证分组存在
            Assert.That(group1, Is.Not.Null);
            Assert.That(group2, Is.Not.Null);

            // 比较Group1和Group2的优先级
            Assert.That(panel.taskGroups.FindIndex(g => g[0].Group == "Test Group1"), Is.GreaterThan(panel.taskGroups.FindIndex(g => g[0].Group == "Test Group2")));

            // 验证Group1中的任务
            Assert.That(group1.Count, Is.EqualTo(1));
            Assert.That(XEditor.Tasks.Workers[group1[0]].Priority, Is.EqualTo(1));
            Assert.That(group1[0].Name, Is.EqualTo("Test Visual Task1"));

            // 验证Group2中的任务优先级排序
            Assert.That(group2.Count, Is.EqualTo(2));
            Assert.That(group2[0].Name, Is.EqualTo("Test Visual Task2"));
            Assert.That(group2[1].Name, Is.EqualTo("Test Visual Task3"));
            Assert.That(XEditor.Tasks.Workers[group2[0]].Priority, Is.EqualTo(0));
            Assert.That(XEditor.Tasks.Workers[group2[1]].Priority, Is.EqualTo(2));

            // 验证默认状态
            Assert.That(panel.foldoutAll, Is.True);
            Assert.That(panel.selectAll, Is.False);
        }
        finally
        {
            // 清理测试环境
            if (XEditor.Tasks.Workers.ContainsKey(task1Meta)) XEditor.Tasks.Workers.Remove(task1Meta);
            if (XEditor.Tasks.Workers.ContainsKey(task2Meta)) XEditor.Tasks.Workers.Remove(task2Meta);
            if (XEditor.Tasks.Workers.ContainsKey(task3Meta)) XEditor.Tasks.Workers.Remove(task3Meta);

            if (XEditor.Tasks.Panel.Instance != null) // 恢复面板状态
            {
                XEditor.Tasks.Panel.Instance.OnEnable();
            }
        }
    }

    /// <summary>
    /// 测试面板生命周期。
    /// </summary>
    /// <remarks>
    /// 测试内容：
    /// 1. OnEnable 处理
    ///    - 验证实例正确设置
    ///    - 验证回调正确触发
    /// 
    /// 2. OnDisable 处理
    ///    - 验证任务正确通知
    ///    - 验证状态正确保存
    /// 
    /// 3. OnDestroy 处理
    ///    - 验证资源正确清理
    ///    - 验证实例正确销毁
    /// </remarks>
    [Test]
    public void Panel()
    {
        // 重置数据
        TestVisualTask.Reset();

        // 准备测试任务
        var task = new TestVisualTask { ID = "Test/Test Visual Task" };
        var taskMeta = new XEditor.Tasks.WorkerAttribute("Test Visual Task", "Test", "Test Visual Task");
        XEditor.Tasks.Workers[taskMeta] = task;

        var panel = ScriptableObject.CreateInstance<TasksPanel>();

        try
        {
            // 测试 OnEnable
            panel.OnEnable();
            Assert.That(TestVisualTask.onEnableCalled, Is.True);

            // 测试 OnDisable
            panel.OnDisable();
            Assert.That(TestVisualTask.onDisableCalled);

            // 测试 OnDestroy
            panel.OnDestroy();
            Assert.That(TestVisualTask.onDestroyCalled);
        }
        finally
        {
            // 清理测试环境
            if (XEditor.Tasks.Workers.ContainsKey(taskMeta)) XEditor.Tasks.Workers.Remove(taskMeta);

            if (XEditor.Tasks.Panel.Instance != null) // 恢复面板状态
            {
                XEditor.Tasks.Panel.Instance.OnEnable();
            }
        }
    }

    /// <summary>
    /// 测试任务执行功能。
    /// </summary>
    /// <remarks>
    /// 测试内容：
    /// 1. 同步任务执行
    ///    - 验证任务正确执行
    ///    - 验证参数正确传递
    /// 
    /// 2. 异步任务执行
    ///    - 验证任务正确执行
    ///    - 验证参数正确传递
    /// 
    /// 3. 多任务混合执行
    ///    - 验证同步和异步任务的协同
    ///    - 验证异步任务在存在同步任务时的行为转换
    /// 
    /// 4. 参数处理机制
    ///    - 验证直接参数传递
    ///    - 验证持久化参数（XPrefs）读取
    ///    - 验证默认参数处理
    /// </remarks>
    [Test]
    public void Run()
    {
        // 创建同步测试任务
        // - Priority = 1：较低优先级
        // - Runasync = false：同步执行
        var syncTask = new TestVisualTask { ID = "Test/Test Sync Visual Task", Priority = 1, Runasync = false };
        var syncMeta = new XEditor.Tasks.WorkerAttribute("Test Sync Visual Task", "Test", "Test Sync Visual Task");
        var syncParam = new XEditor.Tasks.Param("testParam", "Test Param", "Test Param Description")
        {
            ID = $"Task/{XEnv.Platform}/{syncTask.ID}/Test Param@Editor" // 使用标准格式构造参数ID
        };
        syncMeta.Params = new List<XEditor.Tasks.Param> { syncParam };
        XEditor.Tasks.Workers[syncMeta] = syncTask;

        // 创建异步测试任务
        // - Priority = 0：较高优先级
        // - Runasync = true：异步执行
        var asyncTask = new TestVisualTask { ID = "Test/Test Async Visual Task", Priority = 0, Runasync = true };
        var asyncMeta = new XEditor.Tasks.WorkerAttribute("Test Async Visual Task", "Test", "Test Async Visual Task");
        var asyncParam = new XEditor.Tasks.Param("testParam", "Test Param", "Test Param Description")
        {
            ID = $"Task/{XEnv.Platform}/{asyncTask.ID}/Test Param@Editor" // 使用标准格式构造参数ID
        };
        asyncMeta.Params = new List<XEditor.Tasks.Param> { asyncParam };
        XEditor.Tasks.Workers[asyncMeta] = asyncTask;

        var panel = ScriptableObject.CreateInstance<TasksPanel>();

        try
        {
            // 场景1：测试单个同步任务执行
            // 验证点：
            // - 任务成功执行
            // - 参数正确传递
            TestVisualTask.Reset();
            panel.taskArguments[syncMeta] = new Dictionary<XEditor.Tasks.Param, string> { { syncParam, "sync_value" } };
            panel.Run(new List<XEditor.Tasks.IWorker> { syncTask });
            Assert.That(TestVisualTask.executed, Is.True, "同步任务未执行");
            Assert.That(TestVisualTask.lastParam, Is.EqualTo("sync_value"), "同步任务参数传递错误");
            Assert.That(XFile.HasFile(XFile.PathJoin(XEnv.ProjectPath, TasksPanel.ReportCachePath, "Test Sync Visual Task.json")), Is.True, "同步任务结果缓存应当存在");

            // 场景2：测试单个异步任务执行
            // 验证点：
            // - 任务成功执行
            // - 参数正确传递
            TestVisualTask.Reset();
            panel.taskArguments[asyncMeta] = new Dictionary<XEditor.Tasks.Param, string> { { asyncParam, "async_value" } };
            panel.Run(new List<XEditor.Tasks.IWorker> { asyncTask });
            Assert.That(TestVisualTask.executed, Is.True, "异步任务未执行");
            Assert.That(TestVisualTask.lastParam, Is.EqualTo("async_value"), "异步任务参数传递错误");
            Assert.That(XFile.HasFile(XFile.PathJoin(XEnv.ProjectPath, TasksPanel.ReportCachePath, "Test Async Visual Task.json")), Is.True, "异步任务结果缓存应当存在");

            // 场景3：测试多任务混合执行
            // 验证点：
            // - 多任务成功执行
            // - 异步任务在有同步任务时转为同步执行
            TestVisualTask.Reset();
            panel.taskArguments[syncMeta] = new Dictionary<XEditor.Tasks.Param, string> { { syncParam, "sync_multi" } };
            panel.taskArguments[asyncMeta] = new Dictionary<XEditor.Tasks.Param, string> { { asyncParam, "async_multi" } };
            var workers = new List<XEditor.Tasks.IWorker> { asyncTask, syncTask };
            panel.Run(workers);
            Assert.That(TestVisualTask.executed, Is.True, "多任务执行失败");

            // 验证异步任务的执行模式转换
            Assert.That(asyncTask.Runasync, Is.False, "存在同步任务时，异步任务应该被转换为同步执行");
            Assert.That(syncTask.Runasync, Is.False, "同步任务状态不应改变");

            // 场景4：测试持久化参数处理
            // 验证点：
            // - 从XPrefs正确读取持久化参数
            // - 当taskArguments中没有参数时使用持久化值
            TestVisualTask.Reset();
            syncParam.Persist = true;
            XPrefs.Asset.Set(syncParam.ID, "persist_value");
            panel.taskArguments.Remove(syncMeta); // 移除直接参数，强制使用持久化值
            panel.Run(new List<XEditor.Tasks.IWorker> { syncTask });
            Assert.That(TestVisualTask.lastParam, Is.EqualTo("persist_value"), "持久化参数读取错误");
        }
        finally
        {
            // 清理测试环境
            // - 移除测试任务
            // - 清理持久化参数
            // - 重置面板状态
            if (XEditor.Tasks.Workers.ContainsKey(syncMeta)) XEditor.Tasks.Workers.Remove(syncMeta);
            if (XEditor.Tasks.Workers.ContainsKey(asyncMeta)) XEditor.Tasks.Workers.Remove(asyncMeta);
            if (syncParam.Persist) XPrefs.Asset.Unset(syncParam.ID);

            panel.OnEnable();
        }
    }

    /// <summary>
    /// 测试任务结果的加载。
    /// </summary>
    /// <remarks>
    /// 测试内容：
    /// 1. 任务状态加载
    ///    - 验证状态正确恢复
    ///    - 验证数据一致性
    ///    - 验证所有状态类型的处理
    /// 
    /// 2. 异常处理
    ///    - 验证文件不存在时的处理
    /// </remarks>
    [TestCase("Task1", XEditor.Tasks.Result.Succeeded)]
    [TestCase("Task2", XEditor.Tasks.Result.Failed)]
    public void LoadReportCache(string taskID, XEditor.Tasks.Result result)
    {
        var panel = ScriptableObject.CreateInstance<TasksPanel>();
        // 备份缓存文件
        var bakPath = TasksPanel.ReportCachePath + "bak";
        if (XFile.HasDirectory(TasksPanel.ReportCachePath))
        {
            XFile.CopyDirectory(TasksPanel.ReportCachePath, bakPath);
            XFile.DeleteDirectory(TasksPanel.ReportCachePath);
        }

        try
        {
            var report = new XEditor.Tasks.Report();
            report.Phases.Add(new XEditor.Tasks.Phase() { Name = "Test Phase", Result = result });

            // 保存缓存文件
            var reportFile = XFile.PathJoin(XEnv.ProjectPath, TasksPanel.ReportCachePath, taskID + ".json");
            var reportJson = XObject.ToJson(report, true);
            XFile.SaveText(reportFile, reportJson);

            // 场景1：加载任务状态
            // 验证点：
            // - 正确读取缓存文件
            // - 状态正确恢复
            var readReport = panel.LoadReportCache(taskID);

            // 验证状态恢复
            Assert.That(readReport.Result, Is.EqualTo(result));

            // 场景2：加载不存在的缓存文件
            // 验证点：
            // - 正确处理文件不存在的情况
            XFile.DeleteFile(reportFile);
            var emptyPanel = ScriptableObject.CreateInstance<TasksPanel>();
            var emptyReport = emptyPanel.LoadReportCache(taskID);
            Assert.That(emptyReport, Is.Null, "不存在缓存文件时应返回空");
        }
        finally
        {
            // 清理测试环境
            var cachePath = XFile.PathJoin(XEnv.ProjectPath, TasksPanel.ReportCachePath);
            if (XFile.HasDirectory(cachePath))
            {
                XFile.DeleteDirectory(cachePath);
            }
            // 还原备份文件
            if (XFile.HasDirectory(bakPath))
            {
                XFile.CopyDirectory(bakPath, TasksPanel.ReportCachePath);
                XFile.DeleteDirectory(bakPath);
            }

            Object.DestroyImmediate(panel);
        }
    }

    /// <summary>
    /// 测试获取任务结果图标。
    /// </summary>
    /// <remarks>
    /// 测试内容：
    /// 1. 成功状态图标
    ///    - 验证成功状态返回正确图标
    ///    - 验证图标资源加载正确
    /// 
    /// 2. 失败状态图标
    ///    - 验证失败状态返回正确图标
    ///    - 验证图标资源加载正确
    /// 
    /// 3. 其他状态图标
    ///    - 验证未知状态返回成功图标
    ///    - 验证取消状态返回失败图标
    /// </remarks>
    [TestCase("Task1", XEditor.Tasks.Result.Succeeded, "d_console.infoicon.sml@2x")]
    [TestCase("Task2", XEditor.Tasks.Result.Failed, "d_console.erroricon.sml@2x")]
    [TestCase("Task3", XEditor.Tasks.Result.Unknown, "d_console.infoicon.sml@2x")]
    [TestCase("Task4", XEditor.Tasks.Result.Cancelled, "d_console.erroricon.sml@2x")]
    public void GetLogButtonIcon(string metaName, XEditor.Tasks.Result resultType, string iconName)
    {
        var panel = ScriptableObject.CreateInstance<TasksPanel>();
        // 备份缓存文件
        var bakPath = TasksPanel.ReportCachePath + "bak";
        if (XFile.HasDirectory(TasksPanel.ReportCachePath))
        {
            XFile.CopyDirectory(TasksPanel.ReportCachePath, bakPath);
            XFile.DeleteDirectory(TasksPanel.ReportCachePath);
        }

        try
        {
            var report = new XEditor.Tasks.Report();
            report.Phases.Add(new XEditor.Tasks.Phase() { Name = "Test Phase", Result = resultType });

            var reportFile = XFile.PathJoin(XEnv.ProjectPath, TasksPanel.ReportCachePath, metaName + ".json");
            var reportJson = XObject.ToJson(report, true);
            XFile.SaveText(reportFile, reportJson);

            var icon = panel.GetLogButtonIcon(metaName);
            Assert.That(icon, Is.EqualTo(XEditor.Icons.GetIcon(iconName)?.image), "状态图标应当正确");
        }
        finally
        {
            // 清理测试环境
            var cachePath = XFile.PathJoin(XEnv.ProjectPath, TasksPanel.ReportCachePath);
            if (XFile.HasDirectory(cachePath))
            {
                XFile.DeleteDirectory(cachePath);
            }
            // 还原备份文件
            if (XFile.HasDirectory(bakPath))
            {
                XFile.CopyDirectory(bakPath, TasksPanel.ReportCachePath);
                XFile.DeleteDirectory(bakPath);
            }
            Object.DestroyImmediate(panel);
        }
    }

    #endregion
}
#endif
