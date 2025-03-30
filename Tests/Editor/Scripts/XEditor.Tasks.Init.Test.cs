// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using EFramework.Editor;
using EFramework.Utility;
using System.Linq;

/// <summary>
/// XEditor.Tasks.Init 模块的单元测试类。
/// </summary>
/// <remarks>
/// <code>
/// 测试范围：
/// 1. 配置文件解析
///    - package.json 任务配置解析
///    - 配置文件动态更新
///    - 平台特定任务处理
/// 
/// 2. 类型定义解析
///    - 任务特性解析
///    - 参数特性解析
///    - 平台限制处理
///    - Pre/Post 特性验证
/// 
/// 3. 任务系统功能
///    - 任务优先级排序
///    - 单例任务处理
///    - 异步任务支持
/// </code>
/// </remarks>
public class TestXEditorTasksInit
{
    /// <summary>
    /// 测试用临时目录路径。
    /// </summary>
    private string testDir;

    /// <summary>
    /// 测试任务 1：基本任务配置测试。
    /// </summary>
    /// <remarks>
    /// 用于测试：
    /// - 基本任务特性
    /// - 参数特性
    /// - 单例和同步执行
    /// </remarks>
    [XEditor.Tasks.Worker(test: true, name: "Test Task1", group: "Test", tooltip: "Test task 1", priority: 1)]
    [XEditor.Tasks.Param("param1", "Test param 1", "default1")]
    private class TestTask1 : XEditor.Tasks.Worker
    {
        public override bool Singleton => true;
        public override bool Runasync => false;
        public override void Process(XEditor.Tasks.Report report) { }
    }

    /// <summary>
    /// 测试任务 2：前置任务和字段参数测试。
    /// </summary>
    /// <remarks>
    /// 用于测试：
    /// - Pre 特性
    /// - 字段参数特性
    /// - 异步执行
    /// </remarks>
    [XEditor.Tasks.Worker(test: true, name: "Test Task2", group: "Test", tooltip: "Test task 2", priority: 2, runasync: true)]
    [XEditor.Tasks.Pre(typeof(TestTask1))]
    private class TestTask2 : XEditor.Tasks.Worker
    {
        [XEditor.Tasks.Param("param2", "Test param 2", "default2")]
        internal string testParam;
        public override void Process(XEditor.Tasks.Report report) { }
    }

    /// <summary>
    /// 测试任务 3：后置任务和非单例测试。
    /// </summary>
    /// <remarks>
    /// 用于测试：
    /// - Post 特性
    /// - 非单例任务
    /// </remarks>
    [XEditor.Tasks.Worker(test: true, name: "Test Task3", group: "Test", tooltip: "Test task 3", priority: 3)]
    [XEditor.Tasks.Post(typeof(TestTask2))]
    private class TestTask3 : XEditor.Tasks.Worker
    {
        public override bool Singleton => false;
        public override void Process(XEditor.Tasks.Report report) { }
    }

    /// <summary>
    /// Windows 平台特定任务测试。
    /// </summary>
    [XEditor.Tasks.Worker(test: true, name: "Test Windows", group: "Test", tooltip: "Windows only task", platform: XEnv.PlatformType.Windows)]
    private class TestWindowsTask : XEditor.Tasks.Worker
    {
        [XEditor.Tasks.Param("winParam", "Windows param", "winDefault", platform: XEnv.PlatformType.Windows)]
        internal string winParam;
        public override void Process(XEditor.Tasks.Report report) { }
    }

    /// <summary>
    /// Linux 平台特定任务测试。
    /// </summary>
    [XEditor.Tasks.Worker(test: true, name: "Test Linux", group: "Test", tooltip: "Linux only task", platform: XEnv.PlatformType.Linux)]
    private class TestLinuxTask : XEditor.Tasks.Worker
    {
        [XEditor.Tasks.Param("linuxParam", "Linux param", "linuxDefault", platform: XEnv.PlatformType.Linux)]
        internal string linuxParam;
        public override void Process(XEditor.Tasks.Report report) { }
    }

    /// <summary>
    /// macOS 平台特定任务测试。
    /// </summary>
    [XEditor.Tasks.Worker(test: true, name: "Test macOS", group: "Test", tooltip: "macOS only task", platform: XEnv.PlatformType.macOS)]
    private class TestmacOSTask : XEditor.Tasks.Worker
    {
        [XEditor.Tasks.Param("macosParam", "macOS param", "macosDefault", platform: XEnv.PlatformType.macOS)]
        internal string macosParam;
        public override void Process(XEditor.Tasks.Report report) { }
    }

    /// <summary>
    /// 测试环境设置。
    /// </summary>
    /// <remarks>
    /// 创建临时测试目录用于存放测试文件。
    /// </remarks>
    [OneTimeSetUp]
    public void Setup()
    {
        testDir = XFile.PathJoin(XEnv.ProjectPath, "Temp", "TestXEditorTasksInit");
        if (XFile.HasDirectory(testDir)) XFile.DeleteDirectory(testDir);
        XFile.CreateDirectory(testDir);
    }

    /// <summary>
    /// 测试环境清理。
    /// </summary>
    /// <remarks>
    /// 删除临时测试目录并重置任务系统。
    /// </remarks>
    [OneTimeTearDown]
    public void Cleanup()
    {
        if (XFile.HasDirectory(testDir))
        {
            XFile.DeleteDirectory(testDir);
        }
        XEditor.Tasks.Init.Parse();
    }

    /// <summary>
    /// 测试 package.json 配置解析。
    /// </summary>
    /// <remarks>
    /// 测试内容：
    /// 1. 基本任务配置解析
    /// 2. 任务参数解析
    /// 3. 平台特定任务过滤
    /// 4. 配置文件动态更新
    /// </remarks>
    [Test]
    public void Package()
    {
        var packageFile = XFile.PathJoin(testDir, "package.json");

        // 创建初始配置文件
        var initialJson = @"{
            ""name"": ""test-package"",
            ""version"": ""1.0.0"",
            ""scripts"": {
                ""test"": ""echo test"",
                ""build"": ""echo build"",
                ""windows"": ""echo windows"",
                ""linux"": ""echo linux"",
                ""macos"": ""echo macos""
            },
            ""scriptsMeta"": {
                ""test"": {
                    ""name"": ""Test Script"",
                    ""group"": ""Test"",
                    ""tooltip"": ""Test npm script"",
                    ""priority"": 1,
                    ""singleton"": true,
                    ""runasync"": true,
                    ""params"": [
                        {
                            ""name"": ""env"",
                            ""tooltip"": ""Environment"",
                            ""default"": ""dev"",
                            ""persist"": true,
                            ""platform"": ""Unknown""
                        }
                    ]
                },
                ""build"": {
                    ""name"": ""Build Script"",
                    ""group"": ""Build"",
                    ""priority"": 2
                },
                ""windows"": {
                    ""name"": ""Windows Script"",
                    ""group"": ""Platform"",
                    ""priority"": 3,
                    ""platform"": ""Windows"",
                    ""params"": [
                        {
                            ""name"": ""winParam"",
                            ""tooltip"": ""Windows Parameter"",
                            ""default"": ""win"",
                            ""platform"": ""Windows""
                        }
                    ]
                },
                ""linux"": {
                    ""name"": ""Linux Script"",
                    ""group"": ""Platform"",
                    ""priority"": 3,
                    ""platform"": ""Linux"",
                    ""params"": [
                        {
                            ""name"": ""linuxParam"",
                            ""tooltip"": ""Linux Parameter"",
                            ""default"": ""linux"",
                            ""platform"": ""Linux""
                        }
                    ]
                },
                ""macos"": {
                    ""name"": ""macOS Script"",
                    ""group"": ""Platform"",
                    ""priority"": 3,
                    ""platform"": ""macOS"",
                    ""params"": [
                        {
                            ""name"": ""macosParam"",
                            ""tooltip"": ""macOS Parameter"",
                            ""default"": ""macos"",
                            ""platform"": ""macOS""
                        }
                    ]
                }
            }
        }";
        XFile.SaveText(packageFile, initialJson);

        // 解析初始配置并验证
        XEditor.Tasks.Init.Parse(test: true, packageFile: packageFile);

        Assert.That(XEditor.Tasks.Metas.Count, Is.EqualTo(3),
            "应该解析出三个通用任务");

        var testTask = XEditor.Tasks.Metas.Find(m => m.Name == "Test Script");
        Assert.That(testTask, Is.Not.Null,
            "Test Script 任务应该存在");
        Assert.That(testTask.Group, Is.EqualTo("Test"),
            "Test Script 任务组应该为 Test");
        Assert.That(testTask.Tooltip, Is.EqualTo("Test npm script"),
            "Test Script 任务提示应该正确");
        Assert.That(testTask.Priority, Is.EqualTo(1),
            "Test Script 任务优先级应该为 1");
        Assert.That(testTask.Singleton, Is.True,
            "Test Script 任务应该是单例的");
        Assert.That(testTask.Runasync, Is.True,
            "Test Script 任务应该是异步的");

        Assert.That(testTask.Params.Count, Is.EqualTo(1),
            "Test Script 任务应该有一个参数");
        var envParam = testTask.Params[0];
        Assert.That(envParam.Name, Is.EqualTo("env"),
            "参数名称应该为 env");
        Assert.That(envParam.Tooltip, Is.EqualTo("Environment"),
            "参数提示应该正确");
        Assert.That(envParam.Default, Is.EqualTo("dev"),
            "参数默认值应该为 dev");
        Assert.That(envParam.Persist, Is.True,
            "参数应该是持久化的");

        // 验证平台特定任务
        var windowsTask = XEditor.Tasks.Metas.Find(m => m.Name == "Windows Script");
        var linuxTask = XEditor.Tasks.Metas.Find(m => m.Name == "Linux Script");
        var macosTask = XEditor.Tasks.Metas.Find(m => m.Name == "macOS Script");

        if (XEnv.Platform == XEnv.PlatformType.Windows)
        {
            Assert.That(windowsTask, Is.Not.Null,
                "Windows 平台应该有 Windows Script 任务");
            Assert.That(linuxTask, Is.Null,
                "Windows 平台不应该有 Linux Script 任务");
            Assert.That(macosTask, Is.Null,
                "Windows 平台不应该有 macOS Script 任务");
            if (windowsTask != null)
            {
                Assert.That(windowsTask.Params[0].Platform, Is.EqualTo(XEnv.PlatformType.Windows),
                    "Windows 任务参数应该是 Windows 平台特定的");
            }
        }
        else if (XEnv.Platform == XEnv.PlatformType.Linux)
        {
            Assert.That(windowsTask, Is.Null,
                "Linux 平台不应该有 Windows Script 任务");
            Assert.That(linuxTask, Is.Not.Null,
                "Linux 平台应该有 Linux Script 任务");
            Assert.That(macosTask, Is.Null,
                "Linux 平台不应该有 macOS Script 任务");
            if (linuxTask != null)
            {
                Assert.That(linuxTask.Params[0].Platform, Is.EqualTo(XEnv.PlatformType.Linux),
                    "Linux 任务参数应该是 Linux 平台特定的");
            }
        }
        else if (XEnv.Platform == XEnv.PlatformType.macOS)
        {
            Assert.That(windowsTask, Is.Null,
                "macOS 平台不应该有 Windows Script 任务");
            Assert.That(linuxTask, Is.Null,
                "macOS 平台不应该有 Linux Script 任务");
            Assert.That(macosTask, Is.Not.Null,
                "macOS 平台应该有 macOS Script 任务");
            if (macosTask != null)
            {
                Assert.That(macosTask.Params[0].Platform, Is.EqualTo(XEnv.PlatformType.macOS),
                    "macOS 任务参数应该是 macOS 平台特定的");
            }
        }

        // 测试配置更新
        var updatedJson = @"{
            ""name"": ""test-package"",
            ""version"": ""1.0.0"",
            ""scripts"": {
                ""deploy"": ""echo deploy""
            },
            ""scriptsMeta"": {
                ""deploy"": {
                    ""name"": ""Deploy Script"",
                    ""group"": ""Deploy"",
                    ""priority"": 3
                }
            }
        }";
        XFile.SaveText(packageFile, updatedJson);

        XEditor.Tasks.Init.Parse(test: true, packageFile: packageFile);
        var deployTask = XEditor.Tasks.Metas.Find(m => m.Name == "Deploy Script");
        Assert.That(deployTask, Is.Not.Null,
            "更新配置后应该有 Deploy Script 任务");
        Assert.That(deployTask.Group, Is.EqualTo("Deploy"),
            "Deploy Script 任务组应该为 Deploy");
        Assert.That(deployTask.Priority, Is.EqualTo(3),
            "Deploy Script 任务优先级应该为 3");
    }

    /// <summary>
    /// 测试类型定义解析。
    /// </summary>
    /// <remarks>
    /// 测试内容：
    /// 1. 任务特性解析
    /// 2. 参数特性解析
    /// 3. 平台限制处理
    /// 4. Pre/Post 特性验证
    /// 5. 优先级排序
    /// </remarks>
    [Test]
    public void Class()
    {
        XEditor.Tasks.Init.Parse(test: true, parseClass: true);

        var testTasks = XEditor.Tasks.Metas.FindAll(m => m.Group == "Test");
        Assert.That(testTasks.Where(t => !t.Name.Contains("Windows") && !t.Name.Contains("Linux") && !t.Name.Contains("macOS")).Count(), Is.EqualTo(3),
            "应该解析出三个通用测试任务");

        Assert.That(testTasks[0].Priority, Is.LessThan(testTasks[1].Priority),
            "任务应该按优先级排序");
        Assert.That(testTasks[1].Priority, Is.LessThan(testTasks[2].Priority),
            "任务应该按优先级排序");

        var task1 = XEditor.Tasks.Metas.Find(m => m.Name == "Test Task1");
        Assert.That(task1, Is.Not.Null,
            "Task1 应该存在");
        Assert.That(task1.Params.Count, Is.EqualTo(1),
            "Task1 应该有一个参数");
        Assert.That(task1.Params[0].Name, Is.EqualTo("param1"),
            "Task1 参数名称应该为 param1");

        var task2 = XEditor.Tasks.Metas.Find(m => m.Name == "Test Task2");
        Assert.That(task2, Is.Not.Null,
            "Task2 应该存在");
        Assert.That(task2.Runasync, Is.True,
            "Task2 应该是异步的");
        Assert.That(task2.Params.Count, Is.EqualTo(1),
            "Task2 应该有一个参数");
        Assert.That(task2.Params[0].Name, Is.EqualTo("param2"),
            "Task2 参数名称应该为 param2");

        var task3 = XEditor.Tasks.Metas.Find(m => m.Name == "Test Task3");
        Assert.That(task3, Is.Not.Null,
            "Task3 应该存在");
        Assert.That(task3.Singleton, Is.True,
            "Task3 应该是单例的");

        // 验证平台特定任务
        var windowsTask = XEditor.Tasks.Metas.Find(m => m.Name == "Test Windows");
        var linuxTask = XEditor.Tasks.Metas.Find(m => m.Name == "Test Linux");
        var macosTask = XEditor.Tasks.Metas.Find(m => m.Name == "Test macOS");

        if (XEnv.Platform == XEnv.PlatformType.Windows)
        {
            Assert.That(windowsTask, Is.Not.Null,
                "Windows 平台应该有 Windows 任务");
            Assert.That(linuxTask, Is.Null,
                "Windows 平台不应该有 Linux 任务");
            Assert.That(macosTask, Is.Null,
                "Windows 平台不应该有 macOS 任务");
            if (windowsTask != null)
            {
                var winParam = windowsTask.Params.Find(p => p.Name == "winParam");
                Assert.That(winParam, Is.Not.Null,
                    "Windows 任务应该有 winParam 参数");
                Assert.That(winParam.Platform, Is.EqualTo(XEnv.PlatformType.Windows),
                    "winParam 应该是 Windows 平台特定的");
            }
        }
        else if (XEnv.Platform == XEnv.PlatformType.Linux)
        {
            Assert.That(windowsTask, Is.Null,
                "Linux 平台不应该有 Windows 任务");
            Assert.That(linuxTask, Is.Not.Null,
                "Linux 平台应该有 Linux 任务");
            Assert.That(macosTask, Is.Null,
                "Linux 平台不应该有 macOS 任务");
            if (linuxTask != null)
            {
                var linuxParam = linuxTask.Params.Find(p => p.Name == "linuxParam");
                Assert.That(linuxParam, Is.Not.Null,
                    "Linux 任务应该有 linuxParam 参数");
                Assert.That(linuxParam.Platform, Is.EqualTo(XEnv.PlatformType.Linux),
                    "linuxParam 应该是 Linux 平台特定的");
            }
        }
        else if (XEnv.Platform == XEnv.PlatformType.macOS)
        {
            Assert.That(windowsTask, Is.Null,
                "macOS 平台不应该有 Windows 任务");
            Assert.That(linuxTask, Is.Null,
                "macOS 平台不应该有 Linux 任务");
            Assert.That(macosTask, Is.Not.Null,
                "macOS 平台应该有 macOS 任务");
            if (macosTask != null)
            {
                var macosParam = macosTask.Params.Find(p => p.Name == "macosParam");
                Assert.That(macosParam, Is.Not.Null,
                    "macOS 任务应该有 macosParam 参数");
                Assert.That(macosParam.Platform, Is.EqualTo(XEnv.PlatformType.macOS),
                    "macosParam 应该是 macOS 平台特定的");
            }
        }

        // 验证 Pre/Post 特性
        var task2Type = typeof(TestTask2);
        var preAttr = task2Type.GetCustomAttributes(typeof(XEditor.Tasks.Pre), false)[0] as XEditor.Tasks.Pre;
        Assert.That(preAttr.Handler, Is.EqualTo(typeof(TestTask1)),
            "Task2 的前置任务应该是 Task1");

        var task3Type = typeof(TestTask3);
        var postAttr = task3Type.GetCustomAttributes(typeof(XEditor.Tasks.Post), false)[0] as XEditor.Tasks.Post;
        Assert.That(postAttr.Handler, Is.EqualTo(typeof(TestTask2)),
            "Task3 的后置任务应该是 Task2");
    }
}
#endif
