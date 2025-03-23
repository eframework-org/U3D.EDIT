// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using EFramework.Editor;
using EFramework.Utility;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;

/// <summary>
/// XEditor.Cmd 命令行工具类的单元测试。
/// </summary>
/// <remarks>
/// 测试内容：
/// 1. 命令查找功能
/// 2. 命令执行功能
/// 3. 跨平台兼容性
/// </remarks>
public class TestXEditorCmd
{
    private string testDir;

    private string succeedCmdFile;

    private string succeedCmdName;

    private string failedCmdFile;

    private string failedCmdName;

    /// <summary>
    /// 测试环境初始化。
    /// </summary>
    [OneTimeSetUp]
    public void Setup()
    {
        // 创建测试目录
        testDir = XFile.PathJoin(XEnv.ProjectPath, "Temp", "TestXEditorCmd");
        if (!XFile.HasDirectory(testDir)) XFile.CreateDirectory(testDir);

        // 创建测试命令文件
        succeedCmdName = Application.platform == RuntimePlatform.WindowsEditor ? "succeed.cmd" : "succeed";
        succeedCmdFile = XFile.PathJoin(testDir, succeedCmdName);
        XFile.SaveText(succeedCmdFile, Application.platform == RuntimePlatform.WindowsEditor ?
            "@echo Hello World\r\n@exit 0" :  // Windows 命令格式
            "#!/bin/bash\necho Hello World\nexit 0");  // Unix 命令格式

        failedCmdName = Application.platform == RuntimePlatform.WindowsEditor ? "failed.cmd" : "failed";
        failedCmdFile = XFile.PathJoin(testDir, failedCmdName);
        XFile.SaveText(failedCmdFile, Application.platform == RuntimePlatform.WindowsEditor ?
            "@echo Hello World\r\n@exit 1" :  // Windows 命令格式
            "#!/bin/bash\necho Hello World\nexit 1");  // Unix 命令格式

        // 非 Windows 平台设置执行权限
        if (Application.platform != RuntimePlatform.WindowsEditor)
        {
            XEditor.Cmd.Run("/bin/chmod", testDir, false, false, "+x", succeedCmdFile).Wait();
            XEditor.Cmd.Run("/bin/chmod", testDir, false, false, "+x", failedCmdFile).Wait();
        }
    }

    /// <summary>
    /// 测试环境清理。
    /// </summary>
    [OneTimeTearDown]
    public void Cleanup()
    {
        if (XFile.HasDirectory(testDir)) XFile.DeleteDirectory(testDir);
    }

    /// <summary>
    /// 测试命令查找功能。
    /// </summary>
    [Test]
    public void Find()
    {
        Assert.AreEqual(XEditor.Cmd.Find(""), "", "空命令名称应返回空字符串");
        Assert.AreEqual(XEditor.Cmd.Find("nonexistent"), "nonexistent", "不存在的命令应返回原字符串");
        Assert.AreEqual(XEditor.Cmd.Find(succeedCmdName, testDir), succeedCmdFile, "指定路径的命令应返回完整路径");
    }

    /// <summary>
    /// 测试命令执行功能。
    /// </summary>
    /// <param name="print">是否打印输出</param>
    /// <param name="progress">是否显示进度</param>
    /// <param name="cmd">命令名称</param>
    /// <param name="code">期望的返回码</param>
    [TestCase(false, false, "succeed", 0, TestName = "验证成功命令执行，无打印输出，无进度显示")]
    [TestCase(true, false, "succeed", 0, TestName = "验证成功命令执行，启用打印输出，无进度显示")]
    [TestCase(false, true, "failed", 1, TestName = "验证失败命令执行，无打印输出，启用进度显示")]
    [TestCase(true, true, "failed", 1, TestName = "验证失败命令执行，启用打印输出和进度显示")]
    public async Task Run(bool print, bool progress, string cmd, int code)
    {
        if (print)
        {
            if (code != 0) LogAssert.Expect(LogType.Error, new Regex(@"XEditor\.Cmd\.Run: finish .* with code: .*"));
            else LogAssert.Expect(LogType.Log, new Regex(@"XEditor\.Cmd\.Run: finish .* with code: .*"));
        }
        var result = await XEditor.Cmd.Run(bin: XEditor.Cmd.Find(cmd, testDir), print: print, progress: progress);
        Assert.That(result.Code, Is.EqualTo(code), "命令应返回正确的退出码");
    }
}
#endif
