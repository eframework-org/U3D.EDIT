// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using System.Linq;
using System.Collections.Generic;
using EFramework.Editor;
using EFramework.Utility;

/// <summary>
/// XEditor.Tasks.Init 模块的单元测试类。
/// </summary>
public class TestXEditorTasksInit
{
    /// <summary>
    /// 测试用临时目录路径。
    /// </summary>
    private string testDir;

    /// <summary>
    /// 测试任务 1：基本任务配置测试。
    /// </summary>
    [XEditor.Tasks.Worker(name: "Test Task1", group: "Test Initialize", tooltip: "Test task 1", priority: 1)]
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
    [XEditor.Tasks.Worker(name: "Test Task2", group: "Test Initialize", tooltip: "Test task 2", priority: 2, runasync: true)]
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
    [XEditor.Tasks.Worker(name: "Test Task3", group: "Test Initialize", tooltip: "Test task 3", priority: 3)]
    [XEditor.Tasks.Post(typeof(TestTask2))]
    private class TestTask3 : XEditor.Tasks.Worker
    {
        public override bool Singleton => false;
        public override void Process(XEditor.Tasks.Report report) { }
    }

    /// <summary>
    /// Windows 平台特定任务测试。
    /// </summary>
    [XEditor.Tasks.Worker(name: "Test Windows", group: "Test Initialize", tooltip: "Windows only task", platform: XEnv.PlatformType.Windows)]
    private class TestWindowsTask : XEditor.Tasks.Worker
    {
        [XEditor.Tasks.Param("winParam", "Windows param", "winDefault", platform: XEnv.PlatformType.Windows)]
        internal string winParam;
        public override void Process(XEditor.Tasks.Report report) { }
    }

    /// <summary>
    /// Linux 平台特定任务测试。
    /// </summary>
    [XEditor.Tasks.Worker(name: "Test Linux", group: "Test Initialize", tooltip: "Linux only task", platform: XEnv.PlatformType.Linux)]
    private class TestLinuxTask : XEditor.Tasks.Worker
    {
        [XEditor.Tasks.Param("linuxParam", "Linux param", "linuxDefault", platform: XEnv.PlatformType.Linux)]
        internal string linuxParam;
        public override void Process(XEditor.Tasks.Report report) { }
    }

    /// <summary>
    /// macOS 平台特定任务测试。
    /// </summary>
    [XEditor.Tasks.Worker(name: "Test macOS", group: "Test Initialize", tooltip: "macOS only task", platform: XEnv.PlatformType.macOS)]
    private class TestmacOSTask : XEditor.Tasks.Worker
    {
        [XEditor.Tasks.Param("macosParam", "macOS param", "macosDefault", platform: XEnv.PlatformType.macOS)]
        internal string macosParam;
        public override void Process(XEditor.Tasks.Report report) { }
    }

    /// <summary>
    /// 测试环境设置。
    /// </summary>
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

        Assert.That(XEditor.Tasks.Metas.Count, Is.EqualTo(3), "应该解析出三个通用任务");

        var testTask = XEditor.Tasks.Metas.FirstOrDefault(m => m.Value.Name == "Test Script").Value;
        Assert.That(testTask, Is.Not.Null, "Test Script 任务应该存在");
        Assert.That(testTask.Group, Is.EqualTo("Test"), "Test Script 任务组应该为 Test");
        Assert.That(testTask.Tooltip, Is.EqualTo("Test npm script"), "Test Script 任务提示应该正确");
        Assert.That(testTask.Priority, Is.EqualTo(1), "Test Script 任务优先级应该为 1");
        Assert.That(testTask.Singleton, Is.True, "Test Script 任务应该是单例的");
        Assert.That(testTask.Runasync, Is.True, "Test Script 任务应该是异步的");

        Assert.That(testTask.Params.Count, Is.EqualTo(1), "Test Script 任务应该有一个参数");
        var envParam = testTask.Params[0];
        Assert.That(envParam.Name, Is.EqualTo("env"), "参数名称应该为 env");
        Assert.That(envParam.Tooltip, Is.EqualTo("Environment"), "参数提示应该正确");
        Assert.That(envParam.Default, Is.EqualTo("dev"), "参数默认值应该为 dev");
        Assert.That(envParam.Persist, Is.True, "参数应该是持久化的");

        // 验证平台特定任务
        var windowsTask = XEditor.Tasks.Metas.FirstOrDefault(m => m.Value.Name == "Windows Script").Value;
        var linuxTask = XEditor.Tasks.Metas.FirstOrDefault(m => m.Value.Name == "Linux Script").Value;
        var macosTask = XEditor.Tasks.Metas.FirstOrDefault(m => m.Value.Name == "macOS Script").Value;

        if (XEnv.Platform == XEnv.PlatformType.Windows)
        {
            Assert.That(windowsTask, Is.Not.Null, "Windows 平台应该有 Windows Script 任务");
            Assert.That(linuxTask, Is.Null, "Windows 平台不应该有 Linux Script 任务");
            Assert.That(macosTask, Is.Null, "Windows 平台不应该有 macOS Script 任务");
            if (windowsTask != null)
            {
                Assert.That(windowsTask.Params[0].Platform, Is.EqualTo(XEnv.PlatformType.Windows), "Windows 任务参数应该是 Windows 平台特定的");
            }
        }
        else if (XEnv.Platform == XEnv.PlatformType.Linux)
        {
            Assert.That(windowsTask, Is.Null, "Linux 平台不应该有 Windows Script 任务");
            Assert.That(linuxTask, Is.Not.Null, "Linux 平台应该有 Linux Script 任务");
            Assert.That(macosTask, Is.Null, "Linux 平台不应该有 macOS Script 任务");
            if (linuxTask != null)
            {
                Assert.That(linuxTask.Params[0].Platform, Is.EqualTo(XEnv.PlatformType.Linux), "Linux 任务参数应该是 Linux 平台特定的");
            }
        }
        else if (XEnv.Platform == XEnv.PlatformType.macOS)
        {
            Assert.That(windowsTask, Is.Null, "macOS 平台不应该有 Windows Script 任务");
            Assert.That(linuxTask, Is.Null, "macOS 平台不应该有 Linux Script 任务");
            Assert.That(macosTask, Is.Not.Null, "macOS 平台应该有 macOS Script 任务");
            if (macosTask != null)
            {
                Assert.That(macosTask.Params[0].Platform, Is.EqualTo(XEnv.PlatformType.macOS), "macOS 任务参数应该是 macOS 平台特定的");
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
        var deployTask = XEditor.Tasks.Metas.FirstOrDefault(m => m.Value.Name == "Deploy Script").Value;
        Assert.That(deployTask, Is.Not.Null, "更新配置后应该有 Deploy Script 任务");
        Assert.That(deployTask.Group, Is.EqualTo("Deploy"), "Deploy Script 任务组应该为 Deploy");
        Assert.That(deployTask.Priority, Is.EqualTo(3), "Deploy Script 任务优先级应该为 3");
    }

    /// <summary>
    /// 测试特性定义解析。
    /// </summary>
    [Test]
    public void Attribute()
    {
        XEditor.Tasks.Init.Parse(test: true, attribute: true);

        var testTasks = new List<XEditor.Tasks.WorkerAttribute>();
        foreach (var kvp in XEditor.Tasks.Metas)
        {
            if (kvp.Value.Group == "Test Initialize") testTasks.Add(kvp.Value);
        }
        Assert.That(testTasks.Where(t => !t.Name.Contains("Windows") && !t.Name.Contains("Linux") && !t.Name.Contains("macOS")).Count(), Is.EqualTo(3), "应该解析出三个通用测试任务");

        var task1 = XEditor.Tasks.Metas.FirstOrDefault(m => m.Value.Name == "Test Task1").Value;
        Assert.That(task1, Is.Not.Null, "Task1 应该存在");
        Assert.That(task1.Params.Count, Is.EqualTo(1), "Task1 应该有一个参数");
        Assert.That(task1.Params[0].Name, Is.EqualTo("param1"), "Task1 参数名称应该为 param1");

        var task2 = XEditor.Tasks.Metas.FirstOrDefault(m => m.Value.Name == "Test Task2").Value;
        Assert.That(task2, Is.Not.Null, "Task2 应该存在");
        Assert.That(task2.Runasync, Is.True, "Task2 应该是异步的");
        Assert.That(task2.Params.Count, Is.EqualTo(1), "Task2 应该有一个参数");
        Assert.That(task2.Params[0].Name, Is.EqualTo("param2"), "Task2 参数名称应该为 param2");

        var task3 = XEditor.Tasks.Metas.FirstOrDefault(m => m.Value.Name == "Test Task3").Value;
        Assert.That(task3, Is.Not.Null, "Task3 应该存在");
        Assert.That(task3.Singleton, Is.True, "Task3 应该是单例的");

        // 验证平台特定任务
        var windowsTask = XEditor.Tasks.Metas.FirstOrDefault(m => m.Value.Name == "Test Windows").Value;
        var linuxTask = XEditor.Tasks.Metas.FirstOrDefault(m => m.Value.Name == "Test Linux").Value;
        var macosTask = XEditor.Tasks.Metas.FirstOrDefault(m => m.Value.Name == "Test macOS").Value;

        if (XEnv.Platform == XEnv.PlatformType.Windows)
        {
            Assert.That(windowsTask, Is.Not.Null, "Windows 平台应该有 Windows 任务");
            Assert.That(linuxTask, Is.Null, "Windows 平台不应该有 Linux 任务");
            Assert.That(macosTask, Is.Null, "Windows 平台不应该有 macOS 任务");
            if (windowsTask != null)
            {
                var winParam = windowsTask.Params.Find(p => p.Name == "winParam");
                Assert.That(winParam, Is.Not.Null, "Windows 任务应该有 winParam 参数");
                Assert.That(winParam.Platform, Is.EqualTo(XEnv.PlatformType.Windows), "winParam 应该是 Windows 平台特定的");
            }
        }
        else if (XEnv.Platform == XEnv.PlatformType.Linux)
        {
            Assert.That(windowsTask, Is.Null, "Linux 平台不应该有 Windows 任务");
            Assert.That(linuxTask, Is.Not.Null, "Linux 平台应该有 Linux 任务");
            Assert.That(macosTask, Is.Null, "Linux 平台不应该有 macOS 任务");
            if (linuxTask != null)
            {
                var linuxParam = linuxTask.Params.Find(p => p.Name == "linuxParam");
                Assert.That(linuxParam, Is.Not.Null, "Linux 任务应该有 linuxParam 参数");
                Assert.That(linuxParam.Platform, Is.EqualTo(XEnv.PlatformType.Linux), "linuxParam 应该是 Linux 平台特定的");
            }
        }
        else if (XEnv.Platform == XEnv.PlatformType.macOS)
        {
            Assert.That(windowsTask, Is.Null, "macOS 平台不应该有 Windows 任务");
            Assert.That(linuxTask, Is.Null, "macOS 平台不应该有 Linux 任务");
            Assert.That(macosTask, Is.Not.Null, "macOS 平台应该有 macOS 任务");
            if (macosTask != null)
            {
                var macosParam = macosTask.Params.Find(p => p.Name == "macosParam");
                Assert.That(macosParam, Is.Not.Null, "macOS 任务应该有 macosParam 参数");
                Assert.That(macosParam.Platform, Is.EqualTo(XEnv.PlatformType.macOS), "macosParam 应该是 macOS 平台特定的");
            }
        }

        // 验证 Pre/Post 特性
        var task2Type = typeof(TestTask2);
        var preAttr = task2Type.GetCustomAttributes(typeof(XEditor.Tasks.Pre), false)[0] as XEditor.Tasks.Pre;
        Assert.That(preAttr.Handler, Is.EqualTo(typeof(TestTask1)), "Task2 的前置任务应该是 Task1");

        var task3Type = typeof(TestTask3);
        var postAttr = task3Type.GetCustomAttributes(typeof(XEditor.Tasks.Post), false)[0] as XEditor.Tasks.Post;
        Assert.That(postAttr.Handler, Is.EqualTo(typeof(TestTask2)), "Task3 的后置任务应该是 Task2");
    }
}
#endif
