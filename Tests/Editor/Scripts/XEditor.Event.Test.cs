// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS
using System;
using NUnit.Framework;
using EFramework.Editor;
using System.Collections.Generic;

/// <summary>
/// XEditor.Event 模块的单元测试类。
/// </summary>
/// <remarks>
/// 测试范围：
/// 1. 事件注册机制
///    - 接口注册的正确性
///    - 单例处理器的管理
///    - 处理器优先级排序
/// 2. 事件通知机制
///    - 事件触发的正确性
///    - 多次触发的行为
///    - 优先级顺序验证
/// 3. 参数解析机制
///    - 多类型参数解析
///    - 参数顺序保持
///    - 类型转换正确性
/// </remarks>
public class TestXEditorEvent
{
    /// <summary>
    /// 测试用事件接口。
    /// </summary>
    /// <remarks>
    /// 用于验证：
    /// 1. 事件注册机制
    /// 2. 回调触发顺序
    /// 3. 参数传递
    /// </remarks>
    private interface ITestEvent : XEditor.Event.Callback
    {
        void Process(params object[] args);
    }

    /// <summary>
    /// 普通事件处理器。
    /// </summary>
    /// <remarks>
    /// 特点：
    /// - 优先级为0（较高优先级）
    /// - 非单例模式，每次可创建新实例
    /// - 用于测试多实例处理器的行为
    /// </remarks>
    private class TestCallback : ITestEvent
    {
        public int Priority => 0;

        public bool Singleton => false;

        void ITestEvent.Process(params object[] args) { receivedEvents.Add(this); }
    }

    /// <summary>
    /// 单例事件处理器。
    /// </summary>
    /// <remarks>
    /// 特点：
    /// - 优先级为1（较低优先级）
    /// - 单例模式，通过静态Instance属性访问
    /// - 用于测试单例处理器的注册和触发
    /// </remarks>
    private class TestSingletonCallback : ITestEvent
    {
        private static readonly TestSingletonCallback instance = new();
        public static TestSingletonCallback Instance => instance;

        public int Priority => 1;

        public bool Singleton => true;

        void ITestEvent.Process(params object[] args) { receivedEvents.Add(this); }
    }

    /// <summary>
    /// 记录已接收的事件回调。
    /// </summary>
    /// <remarks>
    /// 用于验证：
    /// 1. 事件触发的顺序
    /// 2. 触发次数的正确性
    /// 3. 处理器实例的唯一性
    /// </remarks>
    private static readonly List<ITestEvent> receivedEvents = new();

    /// <summary>
    /// 测试环境清理。
    /// </summary>
    /// <remarks>
    /// 确保每次测试运行前：
    /// 1. 事件记录列表为空
    /// 2. 不影响其他测试用例
    /// </remarks>
    [OneTimeTearDown]
    public void Cleanup()
    {
        receivedEvents.Clear();
    }

    /// <summary>
    /// 测试事件注册机制。
    /// </summary>
    /// <remarks>
    /// 验证：
    /// 1. 事件接口是否正确注册到回调表
    /// 2. 单例处理器是否正确存储
    /// 3. 单例实例引用是否正确
    /// </remarks>
    [Test]
    public void Register()
    {
        Assert.That(XEditor.Event.Callbacks.ContainsKey(typeof(ITestEvent)),
            "事件接口 ITestEvent 应该已注册到回调表中");

        Assert.That(XEditor.Event.Singletons.ContainsKey(typeof(TestSingletonCallback)),
            "单例处理器 TestSingletonCallback 应该已注册到单例表中");

        Assert.That(XEditor.Event.Singletons[typeof(TestSingletonCallback)], Is.EqualTo(TestSingletonCallback.Instance),
            "单例表中的实例应该与 TestSingletonCallback.Instance 相同");
    }

    /// <summary>
    /// 测试参数解析机制。
    /// </summary>
    /// <remarks>
    /// 验证：
    /// 1. 不同类型参数的解析正确性
    /// 2. 参数顺序的保持
    /// 3. 类型转换的准确性
    /// </remarks>
    [Test]
    public void Decode()
    {
        string testStr = "test";
        int testInt = 42;
        bool testBool = true;
        object[] args = new object[] { testStr, testInt, testBool };

        XEditor.Event.Decode(out string str, out int num, out bool flag, args);

        Assert.That(str, Is.EqualTo(testStr),
            $"字符串参数解析应得到 {testStr}");

        Assert.That(num, Is.EqualTo(testInt),
            $"整数参数解析应得到 {testInt}");

        Assert.That(flag, Is.EqualTo(testBool),
            $"布尔参数解析应得到 {testBool}");
    }

    /// <summary>
    /// 测试事件通知机制。
    /// </summary>
    /// <remarks>
    /// 验证：
    /// 1. 事件触发的正确性
    /// 2. 多次触发的行为
    /// 3. 优先级排序（Priority值小的先执行）
    /// 4. 空事件类型的异常处理
    /// </remarks>
    [Test]
    public void Notify()
    {
        XEditor.Event.Notify<ITestEvent>();
        XEditor.Event.Notify<ITestEvent>();

        Assert.That(receivedEvents.Count, Is.EqualTo(4),
            "两次通知应产生4个事件回调（每次2个处理器）");

        Assert.That(receivedEvents[^1], Is.EqualTo(TestSingletonCallback.Instance),
            "最后一个触发的应该是优先级较低的单例处理器");

        Assert.Throws<ArgumentNullException>(() => XEditor.Event.Notify(null),
            "传入空事件类型应抛出 ArgumentNullException 异常");
    }
}
#endif
