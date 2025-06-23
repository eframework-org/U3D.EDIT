// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using EFramework.Editor;
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
    [Test]
    public void Sort()
    {
        // 准备测试任务
        var task1 = new TestVisualTask { ID = "Test Group1/Test Visual Task1", Priority = 1 };
        var task1Meta = new XEditor.Tasks.WorkerAttribute("Test Visual Task1", "Test Group1", "Test Visual Task 1");
        XEditor.Tasks.Metas[task1.ID] = task1Meta;
        XEditor.Tasks.Workers[task1.ID] = task1;

        var task2 = new TestVisualTask { ID = "Test Group2/Test Visual Task2", Priority = 0 };
        var task2Meta = new XEditor.Tasks.WorkerAttribute("Test Visual Task2", "Test Group2", "Test Visual Task 2");
        XEditor.Tasks.Metas[task2.ID] = task2Meta;
        XEditor.Tasks.Workers[task2.ID] = task2;

        var task3 = new TestVisualTask { ID = "Test Group2/Test Visual Task3", Priority = 2 };
        var task3Meta = new XEditor.Tasks.WorkerAttribute("Test Visual Task3", "Test Group2", "Test Visual Task 3");
        XEditor.Tasks.Metas[task3.ID] = task3Meta;
        XEditor.Tasks.Workers[task3.ID] = task3;

        var panel = ScriptableObject.CreateInstance<TaskRunner>();

        try
        {
            // 执行重置
            panel.OnEnable();

            // 验证任务分组
            var group1 = panel.taskGroups.FirstOrDefault(g => g[0].StartsWith("Test Group1"));
            var group2 = panel.taskGroups.FirstOrDefault(g => g[0].StartsWith("Test Group2"));

            // 验证分组存在
            Assert.That(group1, Is.Not.Null);
            Assert.That(group2, Is.Not.Null);

            // 比较Group1和Group2的优先级
            Assert.That(panel.taskGroups.FindIndex(g => g[0].StartsWith("Test Group1")), Is.GreaterThan(panel.taskGroups.FindIndex(g => g[0].StartsWith("Test Group2"))));

            // 验证Group1中的任务
            Assert.That(group1.Count, Is.EqualTo(1));
            Assert.That(XEditor.Tasks.Workers[group1[0]].Priority, Is.EqualTo(1));
            Assert.That(group1[0], Is.EqualTo("Test Group1/Test Visual Task1"));

            // 验证Group2中的任务优先级排序
            Assert.That(group2.Count, Is.EqualTo(2));
            Assert.That(group2[0], Is.EqualTo("Test Group2/Test Visual Task2"));
            Assert.That(group2[1], Is.EqualTo("Test Group2/Test Visual Task3"));
            Assert.That(XEditor.Tasks.Workers[group2[0]].Priority, Is.EqualTo(0));
            Assert.That(XEditor.Tasks.Workers[group2[1]].Priority, Is.EqualTo(2));

            // 验证默认状态
            Assert.That(panel.foldoutAll, Is.True);
            Assert.That(panel.selectAll, Is.False);
        }
        finally
        {
            // 清理测试环境
            if (XEditor.Tasks.Metas.ContainsKey(task1.ID)) XEditor.Tasks.Metas.Remove(task1.ID);
            if (XEditor.Tasks.Workers.ContainsKey(task1.ID)) XEditor.Tasks.Workers.Remove(task1.ID);
            if (XEditor.Tasks.Metas.ContainsKey(task2.ID)) XEditor.Tasks.Metas.Remove(task2.ID);
            if (XEditor.Tasks.Workers.ContainsKey(task2.ID)) XEditor.Tasks.Workers.Remove(task2.ID);
            if (XEditor.Tasks.Metas.ContainsKey(task3.ID)) XEditor.Tasks.Metas.Remove(task3.ID);
            if (XEditor.Tasks.Workers.ContainsKey(task3.ID)) XEditor.Tasks.Workers.Remove(task3.ID);

            // 恢复面板状态
            Object.DestroyImmediate(panel);
            XEditor.Tasks.Panel.Reset();
        }
    }

    /// <summary>
    /// 测试面板生命周期。
    /// </summary>
    [Test]
    public void Panel()
    {
        // 重置数据
        TestVisualTask.Reset();

        // 准备测试任务
        var task = new TestVisualTask { ID = "Test/Test Visual Task" };
        var taskMeta = new XEditor.Tasks.WorkerAttribute("Test Visual Task", "Test", "Test Visual Task");
        XEditor.Tasks.Metas[task.ID] = taskMeta;
        XEditor.Tasks.Workers[task.ID] = task;

        var panel = ScriptableObject.CreateInstance<TaskRunner>();

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
            if (XEditor.Tasks.Metas.ContainsKey(task.ID)) XEditor.Tasks.Metas.Remove(task.ID);
            if (XEditor.Tasks.Workers.ContainsKey(task.ID)) XEditor.Tasks.Workers.Remove(task.ID);

            // 恢复面板状态
            Object.DestroyImmediate(panel);
            XEditor.Tasks.Panel.Reset();
        }
    }

#if UNITY_6000_0_OR_NEWER
    /// <summary>
    /// 测试任务执行功能。
    /// </summary>
    [Test]
    public async Task Run()
    {
        // 创建同步测试任务
        var syncTask = new TestVisualTask { ID = "Test/Test Sync Visual Task", Priority = 1, Runasync = false };
        var syncMeta = new XEditor.Tasks.WorkerAttribute("Test Sync Visual Task", "Test", "Test Sync Visual Task");
        var syncParam = new XEditor.Tasks.Param("testParam", "Test Param", "Test Param Description")
        {
            ID = $"Task/{XEnv.Platform}/{syncTask.ID}/Test Param@Editor" // 使用标准格式构造参数ID
        };
        syncMeta.Params = new List<XEditor.Tasks.Param> { syncParam };
        XEditor.Tasks.Metas[syncTask.ID] = syncMeta;
        XEditor.Tasks.Workers[syncTask.ID] = syncTask;

        // 创建异步测试任务
        var asyncTask = new TestVisualTask { ID = "Test/Test Async Visual Task", Priority = 0, Runasync = true };
        var asyncMeta = new XEditor.Tasks.WorkerAttribute("Test Async Visual Task", "Test", "Test Async Visual Task");
        var asyncParam = new XEditor.Tasks.Param("testParam", "Test Param", "Test Param Description")
        {
            ID = $"Task/{XEnv.Platform}/{asyncTask.ID}/Test Param@Editor" // 使用标准格式构造参数ID
        };
        asyncMeta.Params = new List<XEditor.Tasks.Param> { asyncParam };
        XEditor.Tasks.Metas[asyncTask.ID] = asyncMeta;
        XEditor.Tasks.Workers[asyncTask.ID] = asyncTask;

        var panel = ScriptableObject.CreateInstance<TaskRunner>();

        try
        {
            // 场景1：测试单个同步任务执行
            TestVisualTask.Reset();
            panel.taskArguments[syncTask.ID] = new Dictionary<XEditor.Tasks.Param, string> { { syncParam, "sync_value" } };
            await panel.Run(new List<XEditor.Tasks.IWorker> { syncTask });
            Assert.That(TestVisualTask.executed, Is.True, "同步任务未执行");
            Assert.That(TestVisualTask.lastParam, Is.EqualTo("sync_value"), "同步任务参数传递错误");
            Assert.That(XFile.HasFile(XFile.PathJoin(TaskRunner.reportRoot, syncTask.ID.MD5())), Is.True, "同步任务结果缓存应当存在");

            // 场景2：测试单个异步任务执行
            TestVisualTask.Reset();
            panel.taskArguments[asyncTask.ID] = new Dictionary<XEditor.Tasks.Param, string> { { asyncParam, "async_value" } };
            await panel.Run(new List<XEditor.Tasks.IWorker> { asyncTask });
            Assert.That(TestVisualTask.executed, Is.True, "异步任务未执行");
            Assert.That(TestVisualTask.lastParam, Is.EqualTo("async_value"), "异步任务参数传递错误");
            Assert.That(XFile.HasFile(XFile.PathJoin(TaskRunner.reportRoot, asyncTask.ID.MD5())), Is.True, "异步任务结果缓存应当存在");

            // 场景3：测试多任务混合执行
            TestVisualTask.Reset();
            panel.taskArguments[syncTask.ID] = new Dictionary<XEditor.Tasks.Param, string> { { syncParam, "sync_multi" } };
            panel.taskArguments[asyncTask.ID] = new Dictionary<XEditor.Tasks.Param, string> { { asyncParam, "async_multi" } };
            var workers = new List<XEditor.Tasks.IWorker> { asyncTask, syncTask };
            await panel.Run(workers);
            Assert.That(TestVisualTask.executed, Is.True, "多任务执行失败");

            // 验证异步任务的执行模式转换
            Assert.That(asyncTask.Runasync, Is.False, "存在同步任务时，异步任务应该被转换为同步执行");
            Assert.That(syncTask.Runasync, Is.False, "同步任务状态不应改变");

            // 场景4：测试持久化参数处理
            TestVisualTask.Reset();
            syncParam.Persist = true;
            XPrefs.Asset.Set(syncParam.ID, "persist_value");
            panel.taskArguments.Remove(syncTask.ID); // 移除直接参数，强制使用持久化值
            await panel.Run(new List<XEditor.Tasks.IWorker> { syncTask });
            Assert.That(TestVisualTask.lastParam, Is.EqualTo("persist_value"), "持久化参数读取错误");
        }
        finally
        {
            if (XEditor.Tasks.Metas.ContainsKey(syncTask.ID)) XEditor.Tasks.Metas.Remove(syncTask.ID);
            if (XEditor.Tasks.Workers.ContainsKey(syncTask.ID)) XEditor.Tasks.Workers.Remove(syncTask.ID);
            if (XEditor.Tasks.Metas.ContainsKey(asyncTask.ID)) XEditor.Tasks.Metas.Remove(asyncTask.ID);
            if (XEditor.Tasks.Workers.ContainsKey(asyncTask.ID)) XEditor.Tasks.Workers.Remove(asyncTask.ID);
            if (syncParam.Persist) XPrefs.Asset.Unset(syncParam.ID);

            Object.DestroyImmediate(panel);
            XEditor.Tasks.Panel.Reset();
        }
    }
#endif
    #endregion
}
#endif
