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

            // 场景2：测试单个异步任务执行
            // 验证点：
            // - 任务成功执行
            // - 参数正确传递
            TestVisualTask.Reset();
            panel.taskArguments[asyncMeta] = new Dictionary<XEditor.Tasks.Param, string> { { asyncParam, "async_value" } };
            panel.Run(new List<XEditor.Tasks.IWorker> { asyncTask });
            Assert.That(TestVisualTask.executed, Is.True, "异步任务未执行");
            Assert.That(TestVisualTask.lastParam, Is.EqualTo("async_value"), "异步任务参数传递错误");

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
    /// 测试获取任务组状态图标。
    /// </summary>
    /// <remarks>
    /// 测试内容：
    /// 1. 组内有未知状态的任务
    ///    - 验证图标获取正确
    ///    - 应当获取到未知图标
    /// 
    /// 2. 组内有失败的任务
    ///    - 验证图标获取正确
    ///    - 应当获取到失败图标
    /// 
    /// 3. 组内所有任务都成功
    ///    - 验证图标获取正确
    ///    - 应当获取到成功图标
    /// </remarks>
    [TestCase(XEditor.Tasks.Result.Succeeded, XEditor.Tasks.Result.Unknown, XEditor.Tasks.Result.Succeeded, TasksPanel.UnknowIcon)]
    [TestCase(XEditor.Tasks.Result.Succeeded, XEditor.Tasks.Result.Failed, XEditor.Tasks.Result.Succeeded, TasksPanel.FailIcon)]
    [TestCase(XEditor.Tasks.Result.Succeeded, XEditor.Tasks.Result.Succeeded, XEditor.Tasks.Result.Succeeded, TasksPanel.SuccessIcon)]
    public void GetGroupStatusIcon(XEditor.Tasks.Result result1, XEditor.Tasks.Result result2, XEditor.Tasks.Result result3, string expected)
    {
        var panel = ScriptableObject.CreateInstance<TasksPanel>();

        // 准备测试任务元数据
        var task1Meta = new XEditor.Tasks.WorkerAttribute("Task1", "TestGroup", "Test Task 1");
        var task2Meta = new XEditor.Tasks.WorkerAttribute("Task2", "TestGroup", "Test Task 2");
        var task3Meta = new XEditor.Tasks.WorkerAttribute("Task3", "TestGroup", "Test Task 3");
        var group = new List<XEditor.Tasks.WorkerAttribute> { task1Meta, task2Meta, task3Meta };

        try
        {
            panel.taskInfoList.Add(new XEditor.Tasks.TaskInfo(task1Meta.Name, result1.ToString()));
            panel.taskInfoList.Add(new XEditor.Tasks.TaskInfo(task2Meta.Name, result2.ToString()));
            panel.taskInfoList.Add(new XEditor.Tasks.TaskInfo(task3Meta.Name, result3.ToString()));
            var icon = panel.GetGroupStatusIcon(group);
            Assert.That(icon, Is.EqualTo(XEditor.Icons.GetIcon(expected)?.image), "状态图标应当正确");
        }
        finally
        {
            // 清理测试环境
            Object.DestroyImmediate(panel);
        }
    }

    /// <summary>
    /// 测试任务状态的保存和加载功能。
    /// </summary>
    /// <remarks>
    /// 测试内容：
    /// 1. 任务状态保存
    ///    - 验证状态正确写入文件
    ///    - 验证数据格式完整性
    ///    - 验证文件创建成功
    /// 
    /// 2. 任务状态加载
    ///    - 验证状态正确恢复
    ///    - 验证数据一致性
    ///    - 验证所有状态类型的处理
    /// 
    /// 3. 异常处理
    ///    - 验证文件不存在时的处理
    /// </remarks>
    [Test]
    public void LoadAndSaveTaskStatus()
    {
        var panel = ScriptableObject.CreateInstance<TasksPanel>();
        // 备份缓存文件
        var bakPath = TasksPanel.TaskInfoCachePath + ".bak";
        if (XFile.HasFile(TasksPanel.TaskInfoCachePath))
        {
            XFile.CopyFile(TasksPanel.TaskInfoCachePath, bakPath);
            XFile.DeleteFile(TasksPanel.TaskInfoCachePath);
        }

        try
        {
            // 场景1：保存任务状态
            // 验证点：
            // - 状态正确保存到文件
            // - 数据格式正确
            panel.taskInfoList.Add(new XEditor.Tasks.TaskInfo("Task1", XEditor.Tasks.Result.Succeeded.ToString(), "Log Test1"));
            panel.taskInfoList.Add(new XEditor.Tasks.TaskInfo("Task2", XEditor.Tasks.Result.Failed.ToString(), "Log Test2"));
            panel.taskInfoList.Add(new XEditor.Tasks.TaskInfo("Task3", XEditor.Tasks.Result.Unknown.ToString(), "Log Test3"));

            panel.SaveTaskInfoCache();

            // 验证文件存在
            var cachePath = XFile.PathJoin(XEnv.ProjectPath, TasksPanel.TaskInfoCachePath);
            Assert.That(XFile.HasFile(cachePath), Is.True, "缓存文件应当创建");

            // 场景2：加载任务状态
            // 验证点：
            // - 正确读取缓存文件
            // - 状态正确恢复
            panel.taskInfoList.Clear();
            panel.LoadTaskInfoCache();

            // 验证状态恢复
            Assert.That(panel.taskInfoList.Find(item => item.Name == "Task1").Result, Is.EqualTo(XEditor.Tasks.Result.Succeeded.ToString()), "Task1状态应当为Succeeded");
            Assert.That(panel.taskInfoList.Find(item => item.Name == "Task2").Result, Is.EqualTo(XEditor.Tasks.Result.Failed.ToString()), "Task2状态应当为Failed");
            Assert.That(panel.taskInfoList.Find(item => item.Name == "Task3").Result, Is.EqualTo(XEditor.Tasks.Result.Unknown.ToString()), "Task3状态应当为Unknown");
            Assert.That(panel.taskInfoList.Find(item => item.Name == "Task1").Log, Is.EqualTo("Log Test1"), "Task1的日志应当为Log Test1");
            Assert.That(panel.taskInfoList.Find(item => item.Name == "Task2").Log, Is.EqualTo("Log Test2"), "Task2的日志应当为Log Test2");
            Assert.That(panel.taskInfoList.Find(item => item.Name == "Task3").Log, Is.EqualTo("Log Test3"), "Task3的日志应当为Log Test3");


            // 场景3：加载不存在的缓存文件
            // 验证点：
            // - 优雅处理文件不存在的情况
            XFile.DeleteFile(cachePath);
            var emptyPanel = ScriptableObject.CreateInstance<TasksPanel>();
            emptyPanel.LoadTaskInfoCache();
            Assert.That(emptyPanel.taskInfoList, Is.Empty, "不存在缓存文件时应返回空状态");
        }
        finally
        {
            // 清理测试环境
            var cachePath = XFile.PathJoin(XEnv.ProjectPath, TasksPanel.TaskInfoCachePath);
            if (XFile.HasFile(cachePath))
            {
                XFile.DeleteFile(cachePath);
            }
            // 还原备份文件
            if (XFile.HasFile(bakPath))
            {
                XFile.CopyFile(bakPath, TasksPanel.TaskInfoCachePath);
                XFile.DeleteFile(bakPath);
            }

            Object.DestroyImmediate(panel);
        }
    }

    /// <summary>
    /// 测试获取任务状态图标。
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
    ///    - 验证未知状态返回正确图标
    ///    - 验证取消状态返回正确图标
    ///    - 验证默认状态处理正确
    /// </remarks>
    [TestCase("Task1", XEditor.Tasks.Result.Succeeded, "TestPassed")]
    [TestCase("Task2", XEditor.Tasks.Result.Failed, "TestFailed")]
    [TestCase("Task3", XEditor.Tasks.Result.Unknown, "TestNormal")]
    [TestCase("Task4", XEditor.Tasks.Result.Cancelled, "TestNormal")]
    public void GetTaskStatusIcon(string metaName, XEditor.Tasks.Result resultType, string result)
    {
        var panel = ScriptableObject.CreateInstance<TasksPanel>();

        panel.taskInfoList.Add(new XEditor.Tasks.TaskInfo(metaName, resultType.ToString()));
        var icon = panel.GetTaskStatusIcon(metaName);
        Assert.That(icon, Is.EqualTo(XEditor.Icons.GetIcon(result)?.image), "状态图标应当正确");

        Object.DestroyImmediate(panel);
    }

    /// <summary>
    /// 测试任务信息的更新逻辑。
    /// </summary>
    /// <remarks>
    /// 测试内容：
    /// 1. 新任务信息的添加逻辑
    ///     - 验证新任务信息正确添加到列表中
    /// 2. 任务信息的更新逻辑
    ///     - 验证已存在任务信息的更新
    /// </remarks>
    [Test]
    public void UpdateTaskInfo()
    {
        var panel = ScriptableObject.CreateInstance<TasksPanel>();

        try
        {
            // 场景1：添加新任务
            panel.taskInfoList.Clear();
            panel.UpdateTaskInfo("TaskA", "Succeeded", "LogA");
            Assert.That(panel.taskInfoList.Count, Is.EqualTo(1));
            Assert.That(panel.taskInfoList[0].Name, Is.EqualTo("TaskA"));
            Assert.That(panel.taskInfoList[0].Result, Is.EqualTo("Succeeded"));
            Assert.That(panel.taskInfoList[0].Log, Is.EqualTo("LogA"));

            // 场景2：更新已存在任务
            panel.UpdateTaskInfo("TaskA", "Failed", "LogB");
            Assert.That(panel.taskInfoList.Count, Is.EqualTo(1));
            Assert.That(panel.taskInfoList[0].Name, Is.EqualTo("TaskA"));
            Assert.That(panel.taskInfoList[0].Result, Is.EqualTo("Failed"));
            Assert.That(panel.taskInfoList[0].Log, Is.EqualTo("LogB"));

            // 场景3：添加另一个新任务
            panel.UpdateTaskInfo("TaskB", "Unknown", "LogC");
            Assert.That(panel.taskInfoList.Count, Is.EqualTo(2));
            Assert.That(panel.taskInfoList.Exists(t => t.Name == "TaskB" && t.Result == "Unknown" && t.Log == "LogC"), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(panel);
        }
    }

    #endregion
}
#endif
