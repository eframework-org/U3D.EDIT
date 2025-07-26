// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS
using System;
using System.Reflection;
using NUnit.Framework;
using EFramework.Editor;

/// <summary>
/// XEditor.Const 常量工具类的单元测试。
/// </summary>
/// <remarks>
/// 测试内容：
/// 1. 自定义特性的获取
/// 2. 属性值的缓存机制
/// 3. 默认值的处理
/// 4. 异常情况的处理
/// </remarks>
public class TestXEditorConst
{
    /// <summary>
    /// 用于测试的自定义属性特性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    private class MyPropAttribute : Attribute { }

    /// <summary>
    /// 用于测试的常量类。
    /// </summary>
    [XEditor.Const]
    private class MyConst
    {
        /// <summary>
        /// 自定义的测试属性。
        /// </summary>
        [MyProp]
        public static string CustomValue => "my_value";
    }

    /// <summary>
    /// 测试获取自定义特性标记的属性值。
    /// </summary>
    /// <param name="attributeType">特性类型</param>
    /// <param name="defaultValue">默认值</param>
    /// <param name="expectedValue">期望的返回值</param>
    /// <remarks>
    /// 验证以下场景：
    /// 1. 存在的自定义特性 (MyPropAttribute)
    /// 2. 不存在的特性 (ObsoleteAttribute)
    /// 3. 空特性类型 (null)
    /// 4. 无效的特性类型 (SerializableAttribute)
    /// 5. 缓存机制的有效性
    /// 6. 默认值的处理
    /// </remarks>
    [TestCase(typeof(MyPropAttribute), null, "my_value",
        Description = "验证存在的自定义特性 MyPropAttribute")]
    [TestCase(typeof(ObsoleteAttribute), null, null,
        Description = "验证不存在的特性 ObsoleteAttribute")]
    [TestCase(typeof(ObsoleteAttribute), "default", "default",
        Description = "验证不存在的特性 ObsoleteAttribute，使用默认值")]
    [TestCase(null, "default", "default",
        Description = "验证空特性类型 null，使用默认值")]
    [TestCase(typeof(SerializableAttribute), "default", "default",
        Description = "验证无效的特性类型 SerializableAttribute，使用默认值")]
    public void GetCustom(Type attributeType, object defaultValue, object expectedValue)
    {
        bool sig = false;
        PropertyInfo prop = null;

        // 第一次调用
        var result1 = XEditor.Const.GetCustom(attributeType, ref sig, ref prop, defaultValue);
        Assert.That(result1, Is.EqualTo(expectedValue),
            "首次查找特性 {0} 应返回 {1}",
            attributeType?.Name ?? "null",
            expectedValue ?? "null");

        // 重置 sig 但保留 prop，测试缓存
        sig = false;
        var result2 = XEditor.Const.GetCustom(attributeType, ref sig, ref prop, defaultValue);
        Assert.That(result2, Is.EqualTo(expectedValue),
            "使用缓存的属性信息查找特性 {0} 应返回 {1}",
            attributeType?.Name ?? "null",
            expectedValue ?? "null");

        // 完全重置，测试重新查找
        sig = false;
        prop = null;
        var result3 = XEditor.Const.GetCustom(attributeType, ref sig, ref prop, defaultValue);
        Assert.That(result3, Is.EqualTo(expectedValue),
            "重新查找特性 {0} 应返回 {1}",
            attributeType?.Name ?? "null",
            expectedValue ?? "null");

        // 使用不同的默认值测试
        var differentDefault = "different";
        var result4 = XEditor.Const.GetCustom(attributeType, ref sig, ref prop, differentDefault);
        var expectedResult4 = prop != null ? expectedValue : differentDefault;
        Assert.That(result4, Is.EqualTo(expectedResult4),
            "使用不同默认值 {0} 查找特性 {1} 应返回 {2}",
            differentDefault,
            attributeType?.Name ?? "null",
            expectedResult4 ?? "null");
    }
}
#endif
