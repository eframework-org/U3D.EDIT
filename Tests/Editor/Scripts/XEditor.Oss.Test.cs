// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using EFramework.Editor;
using EFramework.Utility;
using UnityEngine;

/// <summary>
/// XEditor.Oss 模块的单元测试类。
/// </summary>
/// <remarks>
/// 测试范围：
/// 1. MinIO 客户端管理
///    - 客户端下载
///    - 配置设置
/// 2. 文件操作
///    - 文件上传
///    - 内容验证
/// </remarks>
public class TestXEditorOss
{
    /// <summary>
    /// 测试用临时目录路径。
    /// </summary>
    /// <remarks>
    /// 用于存放测试所需的临时文件：
    /// - test.txt：测试用上传文件
    /// - temp.txt：下载文件验证用
    /// </remarks>
    private string testDir;

    /// <summary>
    /// 测试环境初始化。
    /// </summary>
    /// <remarks>
    /// 执行以下操作：
    /// 1. 创建测试目录
    /// 2. 创建测试文件并写入内容
    /// </remarks>
    [OneTimeSetUp]
    public void Setup()
    {
        testDir = XFile.PathJoin(XEnv.ProjectPath, "Temp", "TestXEditorOss");
        if (XFile.HasDirectory(testDir)) XFile.DeleteDirectory(testDir);
        XFile.CreateDirectory(testDir);

        var mcBin = XFile.PathJoin(XEnv.ProjectPath, "Library", Application.platform == RuntimePlatform.WindowsEditor ? "mc.exe" : "mc");
        if (XFile.HasFile(mcBin)) XFile.DeleteFile(mcBin);

        XFile.SaveText(XFile.PathJoin(testDir, "test.txt"), $"test content {XTime.GetMillisecond()}");
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
    public void Cleanup()
    {
        if (XFile.HasDirectory(testDir))
        {
            XFile.DeleteDirectory(testDir);
        }
    }

    /// <summary>
    /// 测试对象存储服务操作。
    /// </summary>
    /// <remarks>
    /// 验证：
    /// 1. OSS 任务创建和配置
    /// 2. 文件上传功能
    /// 3. 文件内容一致性
    /// </remarks>
    [Test]
    public void Execute()
    {
        var oss = new XEditor.Oss
        {
            ID = "TestXEditorOss/my-upload",
            Host = "http://localhost:9000",
            Bucket = "default",
            Access = "admin",
            Secret = "adminadmin",
            Local = testDir,
            Remote = "TestXEditorOss",
            Batchmode = Application.isBatchMode
        };

        var report = XEditor.Tasks.Execute(oss);

        Assert.That(report.Result, Is.EqualTo(XEditor.Tasks.Result.Succeeded), "OSS 上传任务应该成功执行完成");

        // 验证上传的文件内容
        var localContent = XFile.OpenText(XFile.PathJoin(testDir, "test.txt"));
        var tempFile = XFile.PathJoin(testDir, "temp.txt");

        // 下载远程文件
        var task = XEditor.Cmd.Run(
            bin: oss.Bin,
            args: new string[] {
                    "get",
                    $"\"{oss.Alias}/{oss.Bucket}/{oss.Remote}/test.txt\"",
                    tempFile,
                    "--config-dir",
                    oss.Temp
            }
        );
        task.Wait();

        Assert.That(task.Result.Code, Is.EqualTo(0), "MinIO 客户端下载命令应该成功执行");

        // 比较文件内容
        var remoteContent = XFile.OpenText(tempFile);
        Assert.That(remoteContent, Is.EqualTo(localContent), "上传后的远程文件内容应该与本地文件完全一致");

        Assert.That(XFile.HasDirectory(oss.Temp), Is.False, "任务完成后临时目录应该被清理");
    }
}
#endif
