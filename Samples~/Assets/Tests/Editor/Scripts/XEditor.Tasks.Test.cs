// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using EFramework.Editor;

/// <summary>
/// XEditor.Tasks 模块的基础功能单元测试类。
/// </summary>
public class TestXEditorTasks
{
    /// <summary>
    /// 测试 Report 的各项功能。
    /// </summary>
    [Test]
    public void Report()
    {
        var report = new XEditor.Tasks.Report();

        // 测试阶段1：验证基本阶段添加
        report.Current = new XEditor.Tasks.Phase { Name = "Phase1", Elapsed = 5 };
        Assert.That(report.Current.Name, Is.EqualTo("Phase1"), "阶段1名称应正确设置");
        Assert.That(report.Phases.Count, Is.EqualTo(1), "阶段列表应包含一个阶段");

        // 测试阶段2：验证多阶段管理
        report.Current = new XEditor.Tasks.Phase { Name = "Phase2", Elapsed = 3 };
        Assert.That(report.Current.Name, Is.EqualTo("Phase2"), "阶段2名称应正确设置");
        Assert.That(report.Phases.Count, Is.EqualTo(2), "阶段列表应包含两个阶段");

        // 测试阶段3：验证时间累计
        report.Current = new XEditor.Tasks.Phase { Name = "Phase3", Elapsed = 2 };
        Assert.That(report.Current.Name, Is.EqualTo("Phase3"), "阶段3名称应正确设置");
        Assert.That(report.Phases.Count, Is.EqualTo(3), "阶段列表应包含三个阶段");
        Assert.That(report.Elapsed, Is.EqualTo(10), "总执行时间应为所有阶段时间之和");
        Assert.That(report.Result, Is.EqualTo(XEditor.Tasks.Result.Succeeded), "无错误时应为成功状态");

        // 测试阶段4：验证错误处理
        report.Current = new XEditor.Tasks.Phase { Name = "Phase4", Elapsed = 2 };
        report.Error = "Error4";
        Assert.That(report.Current.Name, Is.EqualTo("Phase4"), "阶段4名称应正确设置");
        Assert.That(report.Phases.Count, Is.EqualTo(4), "阶段列表应包含四个阶段");
        Assert.That(report.Result, Is.EqualTo(XEditor.Tasks.Result.Failed), "有错误时应为失败状态");
        Assert.That(report.Error, Contains.Substring("Phase4: Error4"), "错误信息应包含阶段名称和错误描述");
    }
}
#endif
