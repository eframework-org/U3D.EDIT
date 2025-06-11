// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using EFramework.Utility;
using EFramework.Editor;

public class TestXEditorUtility
{
    private string testDirectory;
    private string testAsset;

    [OneTimeSetUp]
    public void Setup()
    {
        // 创建测试目录和文件
        testDirectory = "Assets/Temp/XEditorUtilityTest";
        if (!XFile.HasDirectory(testDirectory))
        {
            XFile.CreateDirectory(testDirectory);
        }
        testAsset = XFile.PathJoin(testDirectory, "test.txt");
        XFile.SaveText(testAsset, "test content");
        AssetDatabase.Refresh();
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        // 清理测试文件和目录
        if (XFile.HasDirectory(testDirectory))
        {
            XFile.DeleteDirectory(testDirectory);
            AssetDatabase.Refresh();
        }
    }

    [Test]
    public void CollectFiles()
    {
        // 验证 CollectFiles 方法是否正确收集指定目录下的文件
        var files = new List<string>();
        XEditor.Utility.CollectFiles(testDirectory, files, ".meta");

        // 验证收集的文件列表是否包含测试文件
        Assert.That(files, Contains.Item(XFile.NormalizePath(testAsset)), "收集的文件列表应包含测试文件");

        // 验证收集的文件数量是否正确
        Assert.That(files.Count, Is.EqualTo(1), "收集的文件数量应为 1");
    }

    [Test]
    public void CollectAssets()
    {
        // 验证 CollectAssets 方法是否正确收集指定目录下的资源
        var assets = new List<string>();
        XEditor.Utility.CollectAssets(testDirectory, assets, ".meta");

        // 验证收集的资源列表是否包含测试资源
        Assert.That(assets, Contains.Item(testAsset), "收集的资源列表应包含测试资源");

        // 验证收集的资源数量是否正确
        Assert.That(assets.Count, Is.EqualTo(1), "收集的资源数量应为 1");
    }

    [Test]
    public void CollectDependency()
    {
        // 验证 CollectDependency 方法是否正确收集资源依赖项
        var sourceAssets = new List<string> { testAsset };
        var dependencies = XEditor.Utility.CollectDependency(sourceAssets);

        // 验证依赖项字典是否包含测试资源
        Assert.That(dependencies, Contains.Key(testAsset), "依赖项字典应包含测试资源");

        // 验证测试资源的依赖项是否不为空
        Assert.That(dependencies[testAsset], Is.Not.Null, "测试资源的依赖项不应为空");
    }

    [Test]
    public void ZipDirectory()
    {
        // 验证 ZipDirectory 方法是否正确压缩目录
        var zipPath = XFile.PathJoin(testDirectory, "test.zip");
        var result = XEditor.Utility.ZipDirectory(XFile.NormalizePath(testDirectory), XFile.NormalizePath(zipPath));

        // 验证压缩操作是否成功
        Assert.That(result, Is.True, "目录压缩操作应成功");

        // 验证压缩文件是否存在
        Assert.That(XFile.HasFile(zipPath), Is.True, "压缩文件应成功生成");
    }

    [Test]
    public void GetEditorAssembly()
    {
        // 验证 GetEditorAssembly 方法是否正确获取编辑器程序集
        var assembly = XEditor.Utility.GetEditorAssembly();

        // 验证返回的程序集是否不为空
        Assert.That(assembly, Is.Not.Null, "编辑器程序集应不为空");

        // 验证程序集是否包含 UnityEditor.EditorWindow 类型
        Assert.That(assembly.GetType("UnityEditor.EditorWindow"), Is.Not.Null, "编辑器程序集应包含 UnityEditor.EditorWindow 类型");
    }

    [Test]
    public void GetEditorClass()
    {
        // 验证 GetEditorClass 方法是否正确获取编辑器类
        var clazz = XEditor.Utility.GetEditorClass("UnityEditor.EditorWindow");

        // 验证返回的类是否不为空
        Assert.That(clazz, Is.Not.Null, "编辑器类应不为空");

        // 验证类的名称是否为 "EditorWindow"
        Assert.That(clazz.Name, Is.EqualTo("EditorWindow"), "编辑器类的名称应为 'EditorWindow'");
    }

    [Test]
    public void FindPackage()
    {
        // 验证 FindPackage 方法是否正确找到包信息
        var package = XEditor.Utility.FindPackage();

        // 验证返回的包信息是否不为空
        Assert.That(package, Is.Not.Null, "包信息应不为空");
    }

    [Test]
    public void ShowToast()
    {
        // 验证 ShowToast 方法是否能正确显示消息
        var content = "Test Toast Message";

        // 验证调用 ShowToast 方法时不应抛出异常
        Assert.DoesNotThrow(() => XEditor.Utility.ShowToast(content), "显示 Toast 消息时不应抛出异常");
    }
}
#endif
