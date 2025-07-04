// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS
using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using EFramework.Editor;
using EFramework.Utility;
#if !UNITY_6000_0_OR_NEWER
using System.Reflection;
using System.Linq;
#endif

public class TestXEditorTitle
{
    [SetUp]
    public void Setup()
    {
        XEditor.Title.isRefreshing = false;
        XEditor.Title.prefsLabel = "";
        XEditor.Title.gitBranch = "";
        XEditor.Title.gitPushCount = 0;
        XEditor.Title.gitPullCount = 0;
        XEditor.Title.gitDirtyCount = 0;
    }

    [TearDown]
    public void Cleanup()
    {
        XEditor.Title.isRefreshing = false;
        _ = XEditor.Title.Refresh();
    }

    /// <summary>
    /// 测试标题设置功能。
    /// </summary>
    /// <param name="prefsLabel">首选项标签</param>
    /// <param name="gitBranch">Git 分支名称</param>
    /// <param name="gitDirtyCount">Git 脏文件计数</param>
    /// <param name="gitPushCount">Git 推送计数</param>
    /// <param name="gitPullCount">Git 拉取计数</param>
    /// <param name="isRefreshing">是否正在刷新</param>
    /// <param name="expected">期望的标题内容</param>
    /// <remarks>
    /// 验证以下场景：
    /// 1. 仅首选项标签
    /// 2. 仅 Git 信息
    /// 3. 首选项和 Git 信息组合
    /// 4. 刷新状态下的标题
    /// </remarks>
    [Obsolete]
    [TestCase("", "", 0, 0, 0, false, "Unity", Description = "无首选项和 Git 信息时的默认标题")]
    [TestCase("[Preferences: Test/Channel/1.0.0/Debug/Info]", "", 0, 0, 0, false, "Unity - [Preferences: Test/Channel/1.0.0/Debug/Info]", Description = "仅包含首选项标签的标题")]
    [TestCase("", "main", 1, 2, 3, false, "Unity - [Git*: main ↑2 ↓3]", Description = "仅包含 Git 信息的标题")]
    [TestCase("", "main", 0, 0, 0, true, "Unity - [Git: main ⟳]", Description = "刷新状态下的 Git 标题")]
    [TestCase("[Preferences: Test/Channel/1.0.0/Debug/Info]", "main", 1, 0, 0, false, "Unity - [Preferences: Test/Channel/1.0.0/Debug/Info] - [Git*: main]", Description = "首选项和 Git 信息组合的标题")]
    public void SetTitle(string prefsLabel, string gitBranch, int gitDirtyCount, int gitPushCount, int gitPullCount, bool isRefreshing, string expected)
    {
#if UNITY_6000_0_OR_NEWER
        var descriptor = new ApplicationTitleDescriptor("Unity", "Editor", "6000.0.32f1", "Personal", false) { title = "Unity" };
#else
        // 使用反射创建 ApplicationTitleDescriptor 实例
        var descriptorType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ApplicationTitleDescriptor");
        var constructors = descriptorType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

#if UNITY_2022_1_OR_NEWER
        // 查找匹配的构造函数
        var constructor = constructors.FirstOrDefault(c => c.GetParameters().Length == 5);
        var descriptor = constructor.Invoke(new object[] { "Unity", "Editor", "6000.0.32f1", "Personal", false });
#else
        var constructor = constructors.FirstOrDefault(c => c.GetParameters().Length == 6);
        var descriptor = constructor.Invoke(new object[] { "Unity", "Editor", "6000.0.32f1", "", "Personal", false });
#endif
        // 使用反射设置 title 属性
        var titleProperty = descriptor.GetType().GetField("title", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        titleProperty.SetValue(descriptor, "Unity");
#endif
        XEditor.Title.prefsLabel = prefsLabel;
        XEditor.Title.gitBranch = gitBranch;
        XEditor.Title.gitDirtyCount = gitDirtyCount;
        XEditor.Title.gitPushCount = gitPushCount;
        XEditor.Title.gitPullCount = gitPullCount;
        XEditor.Title.isRefreshing = isRefreshing;

        XEditor.Title.SetTitle(descriptor);
#if UNITY_6000_0_OR_NEWER
        Assert.That(descriptor.title, Is.EqualTo(expected));
#else
        // 使用反射获取 title 属性值
        var actualTitle = titleProperty.GetValue(descriptor) as string;
        Assert.That(actualTitle, Is.EqualTo(expected));
#endif
    }

#if UNITY_6000_0_OR_NEWER
    /// <summary>
    /// 测试标题刷新功能。
    /// </summary>
    /// <remarks>
    /// 验证以下场景：
    /// 1. 首选项标签更新
    /// 2. Git 信息更新
    /// 3. 非 Git 仓库环境
    /// </remarks>
    [Test]
    public async Task Refresh()
    {
        XEditor.Title.isRefreshing = false;
        await XEditor.Title.Refresh();

        var prefsName = string.IsNullOrEmpty(XPrefs.Asset.File) ? "Unknown" : Path.GetFileName(XPrefs.Asset.File);
        var prefsInvalid = !XFile.HasFile(XPrefs.Asset.File) || XPrefs.Asset.Count == 0 ? "*" : "";
        var expectedPrefs = $"[Preferences{prefsInvalid}: {prefsName}/{XEnv.Channel}/{XEnv.Version}/{XEnv.Mode}/{XLog.Level()}]";
        Assert.That(XEditor.Title.prefsLabel, Is.EqualTo(expectedPrefs), "Should update preferences label");

        var task = XEditor.Cmd.Run("git", print: false, args: new string[] { "rev-parse", "--git-dir" });
        if (task.Result.Code == 0) Assert.That(XEditor.Title.gitBranch, Is.Not.Empty); // 在 Git 仓库中
        else Assert.That(XEditor.Title.gitBranch, Is.Empty); // 不在 Git 仓库中
    }
#endif
}
#endif
