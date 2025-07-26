// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using EFramework.Editor;
using UnityEngine;
using EFramework.Utility;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;

/// <summary>
/// XEditor.Prefs 模块的单元测试类。
/// </summary>
/// <remarks>
/// 测试范围：
/// 1. 面板生命周期管理
///    - 面板激活和停用
///    - 数据验证和保存
/// 2. 构建处理流程
///    - 首选项验证
///    - 变量求值
///    - 配置清理
/// </remarks>
public class TestXEditorPrefs
{
    /// <summary>
    /// 测试首选项构建处理。
    /// </summary>
    /// <remarks>
    /// 验证：
    /// 1. 构建前的首选项验证
    /// 2. 变量求值处理
    /// 3. 编辑器配置移除
    /// </remarks>
    [Test]
    public void OnBuild()
    {
        LogAssert.ignoreFailingMessages = true;

        var testDir = XFile.PathJoin(XEnv.ProjectPath, "Temp", "TestXEditorPrefs");
        var lastUri = XPrefs.IAsset.Uri;
        var handler = new XEditor.Prefs() as XEditor.Event.Internal.OnPreprocessBuild;

        try
        {
            XPrefs.IAsset.Uri = XFile.PathJoin(testDir, "Streaming.json"); // 重定向构建时拷贝的首选项文件

            // 准备测试数据
            var tempPrefs = new XPrefs.IBase { File = XFile.PathJoin(testDir, "Default.json") };
            tempPrefs.Set("test_ref_key", "${Env.ProjectPath}");
            tempPrefs.Set("test_const_key@Const", "${Env.LocalPath}");
            tempPrefs.Set("test_editor_key@Editor", "editor_value");
            tempPrefs.Save();

            // 设置当前首选项
            XPrefs.Asset.Read(tempPrefs.File);

            // 模拟构建处理
            handler.Process();

            // 验证变量求值
            var processedPrefs = new XPrefs.IBase(encrypt: true); // 读取加密首选项
            processedPrefs.Read(XPrefs.IAsset.Uri);
            Assert.That(processedPrefs.GetString("test_ref_key"), Is.EqualTo(XEnv.ProjectPath), "环境变量引用应该被正确求值");
            Assert.That(processedPrefs.GetString("test_const_key@Const"), Is.EqualTo("${Env.LocalPath}"), "常量值不应被求值处理");
            Assert.That(processedPrefs.Has("test_editor_key@Editor"), Is.False, "编辑器专用配置应该在构建时被移除");

            // 测试不存在的首选项文件
            XPrefs.Asset.File = XFile.PathJoin(testDir, "test_nonexist.json");
            XPrefs.Asset.Read(XPrefs.Asset.File);
            Assert.Throws<UnityEditor.Build.BuildFailedException>(() => handler.Process(), "使用不存在的首选项文件时应抛出构建失败异常");

            // 测试空首选项文件
            XPrefs.Asset.File = XFile.PathJoin(testDir, "test_empty.json");
            XFile.SaveText(XPrefs.Asset.File, "{}");
            XPrefs.Asset.Read(XPrefs.Asset.File);
            Assert.Throws<UnityEditor.Build.BuildFailedException>(() => handler.Process(), "使用空的首选项文件时应抛出构建失败异常");

            // 测试首选项文件读取失败
            XPrefs.Asset.File = XFile.PathJoin(testDir, "test_invalid.json");
            XFile.SaveText(XPrefs.Asset.File, "invalid_content");
            XPrefs.Asset.Read(XPrefs.Asset.File);
            Assert.Throws<UnityEditor.Build.BuildFailedException>(() => handler.Process(), "使用无效的首选项文件时应抛出构建失败异常");
        }
        finally
        {
            if (XFile.HasDirectory(testDir)) XFile.DeleteDirectory(testDir); // 删除测试目录

            XPrefs.IAsset.Uri = lastUri; // 恢复首选项文件

            LogAssert.ignoreFailingMessages = false;
        }
    }
}
#endif
