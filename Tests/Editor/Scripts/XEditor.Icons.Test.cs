// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;
using EFramework.Editor;

/// <summary>
/// XEditor.Icons 模块的单元测试类。
/// </summary>
/// <remarks>
/// 测试范围：
/// 1. 图标列表管理
///    - 列表初始化
///    - 列表非空验证
/// 2. 图标查找功能
///    - 已存在图标的查找
///    - 不存在图标的处理
///    - 图标资源的有效性
/// </remarks>
public class TestXEditorIcons
{
    /// <summary>
    /// 图标窗口实例。
    /// </summary>
    /// <remarks>
    /// 用于测试的 Icons 窗口实例，每个测试用例运行前创建。
    /// </remarks>
    private XEditor.Icons iconsWindow;

    /// <summary>
    /// 测试环境初始化。
    /// </summary>
    /// <remarks>
    /// 在每个测试方法执行前：
    /// 1. 创建新的图标窗口实例
    /// 2. 确保测试环境的独立性
    /// </remarks>
    [SetUp]
    public void Setup()
    {
        iconsWindow = ScriptableObject.CreateInstance<XEditor.Icons>();
    }

    /// <summary>
    /// 测试环境清理。
    /// </summary>
    /// <remarks>
    /// 在每个测试方法执行后：
    /// 1. 销毁图标窗口实例
    /// 2. 释放相关资源
    /// 3. 避免内存泄漏
    /// </remarks>
    [TearDown]
    public void Cleanup()
    {
        if (iconsWindow != null)
        {
            Object.DestroyImmediate(iconsWindow);
        }
    }

    /// <summary>
    /// 测试图标列表功能。
    /// </summary>
    /// <remarks>
    /// 验证：
    /// 1. 图标列表是否成功初始化
    /// 2. 列表是否包含图标
    /// 3. 列表长度是否符合预期
    /// </remarks>
    [Test]
    public void List()
    {
        Assert.That(XEditor.Icons.List, Is.Not.Empty,
            "图标列表不应为空，应包含系统预设图标");

        Assert.That(XEditor.Icons.List.Length, Is.GreaterThan(0),
            "图标列表长度应大于0，至少包含基础系统图标");
    }

    /// <summary>
    /// 测试图标查找功能。
    /// </summary>
    /// <param name="iconName">要查找的图标名称</param>
    /// <param name="shouldExist">图标是否应该存在</param>
    /// <remarks>
    /// 测试场景：
    /// 1. 查找存在的系统图标（如"Folder Icon"）
    /// 2. 查找不存在的图标（如"NonExistentIcon_12345"）
    /// 
    /// 验证内容：
    /// 1. 对于存在的图标：
    ///    - 返回值不为空
    ///    - 图标纹理有效
    /// 2. 对于不存在的图标：
    ///    - 返回值为空
    /// </remarks>
    [TestCase("Folder Icon", true)]
    [TestCase("NonExistentIcon_12345", false)]
    public void Find(string iconName, bool shouldExist)
    {
        var icon = iconsWindow.GetIcon(iconName);
        if (shouldExist)
        {
            Assert.That(icon, Is.Not.Null,
                $"查找已存在的图标 '{iconName}' 应返回有效的图标对象");

            Assert.That(icon.image, Is.Not.Null,
                $"已存在图标 '{iconName}' 的纹理不应为空");
        }
        else
        {
            Assert.That(icon, Is.Null,
                $"查找不存在的图标 '{iconName}' 应返回 null");
        }
    }
}
#endif
