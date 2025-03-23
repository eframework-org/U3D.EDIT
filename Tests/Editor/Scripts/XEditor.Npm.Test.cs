// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using EFramework.Editor;
using EFramework.Utility;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// XEditor.Npm 模块的单元测试类。
/// </summary>
/// <remarks>
/// 测试范围：
/// 1. NPM 命令执行
///    - 命令参数传递
///    - 执行结果解析
///    - 工作目录设置
/// 2. 任务管理
///    - 任务创建
///    - 任务执行
///    - 结果验证
/// </remarks>
public class TestXEditorNpm
{
    /// <summary>
    /// 测试用临时目录路径。
    /// </summary>
    /// <remarks>
    /// 用于存放测试所需的临时文件：
    /// - package.json：NPM 配置文件
    /// - my-task.js：测试用 Node.js 脚本
    /// </remarks>
    internal string testDir;

    /// <summary>
    /// 测试环境初始化。
    /// </summary>
    /// <remarks>
    /// 执行以下操作：
    /// 1. 创建测试目录
    /// 2. 创建测试用 Node.js 脚本
    /// 3. 创建 package.json 配置文件
    /// </remarks>
    [OneTimeSetUp]
    public void Setup()
    {
        // 创建测试目录和package.json
        testDir = XFile.PathJoin(XEnv.ProjectPath, "Temp", "TestXEditorNpm");
        if (XFile.HasDirectory(testDir)) XFile.DeleteDirectory(testDir);
        XFile.CreateDirectory(testDir);

        // 创建my-task.js
        var jsContent =
@"const args = process.argv.slice(2);
console.log(JSON.stringify({
    args: args.reduce((acc, arg) => {
        const [key, value] = arg.replace(/^--/, '').split('=');
        acc[key] = value;
        return acc;
    }, {})
}));";
        XFile.SaveText(XFile.PathJoin(testDir, "my-task.js"), jsContent);

        // 创建package.json，定义一个简单的测试命令
        var packageJson = $@"{{
            ""scripts"": {{
                ""my-task"": ""node my-task.js""
            }}
        }}";
        XFile.SaveText(XFile.PathJoin(testDir, "package.json"), packageJson);
    }

    /// <summary>
    /// 测试环境清理。
    /// </summary>
    /// <remarks>
    /// 执行以下操作：
    /// 1. 删除测试目录
    /// 2. 清理所有临时文件
    /// </remarks>
    [OneTimeTearDown]
    public void Reset()
    {
        if (XFile.HasDirectory(testDir))
        {
            XFile.DeleteDirectory(testDir);
        }
    }

    /// <summary>
    /// 测试 NPM 命令执行功能。
    /// </summary>
    /// <remarks>
    /// 验证：
    /// 1. 任务创建和配置
    /// 2. 参数传递机制
    /// 3. 执行结果解析
    /// 4. JSON 输出格式
    /// </remarks>
    [Test]
    public void Execute()
    {
        // 有些 npm 版本会通过 stderr 输出 npm notice... 信息
        // 这里忽略这些信息
        LogAssert.ignoreFailingMessages = true;

        // 创建npm任务
        var npm = new XEditor.Npm(id: "my-task", script: "my-task", runasync: false, cwd: testDir, batchmode: Application.isBatchMode);

        // 执行任务并传递参数
        var args = new Dictionary<string, string>
        {
            { "param1", "value1" },
            { "param2", "value2" }
        };
        var report = XEditor.Tasks.Execute(npm, args);
        report.Task.Wait();

        // 验证执行结果
        Assert.That(report.Result, Is.EqualTo(XEditor.Tasks.Result.Succeeded),
            "NPM 任务应该成功执行完成");

        Assert.That(report.Extras, Is.Not.Null,
            "任务报告的附加信息不应为空");

        var cmdResult = report.Extras as XEditor.Cmd.Result;
        Assert.That(cmdResult, Is.Not.Null,
            "任务报告应包含命令执行结果");

        Assert.That(cmdResult.Data, Contains.Substring("\"args\":{"),
            "输出应包含参数对象");

        Assert.That(cmdResult.Data, Contains.Substring("\"param1\":\"value1\""),
            "输出应包含第一个测试参数");

        Assert.That(cmdResult.Data, Contains.Substring("\"param2\":\"value2\""),
            "输出应包含第二个测试参数");
    }
}
#endif
