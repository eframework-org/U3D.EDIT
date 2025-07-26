// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using EFramework.Editor;
using EFramework.Utility;

/// <summary>
/// XEditor.Binary 模块的单元测试类。
/// </summary>
public class TestXEditorBinary
{
    /// <summary>
    /// 测试用构建类（自定义参数）。
    /// </summary>
    public class MyBinary : XEditor.Binary
    {
        public override string Output => XFile.PathJoin(Root, "CustomOutput");
        public override string Name => "CustomName";
        public override string Code => "202501011";
        public override BuildOptions Options => BuildOptions.Development | BuildOptions.AllowDebugging;
        public override string[] Scenes => new string[] { "Assets/Scenes/Test.unity" };
    }

    /// <summary>
    /// 测试用构建类（自定义场景）。
    /// </summary>
    [XEditor.Tasks.Worker(name: "Build Binary", group: "Test Binary")]
    public class MyBinary2 : XEditor.Binary
    {
        public const string Scene = "Assets/Temp/TestXEditorBinary/Test.unity";
        public override string[] Scenes => new string[] { Scene };

        public override void Preprocess(XEditor.Tasks.Report report)
        {
            base.Preprocess(report);

            // 创建场景
            var sceneDir = Path.GetDirectoryName(Scene);
            if (!XFile.HasDirectory(sceneDir)) XFile.DeleteDirectory(sceneDir);
            XFile.CreateDirectory(sceneDir);
            AssetDatabase.Refresh();
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects, UnityEditor.SceneManagement.NewSceneMode.Single);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, Scene);
            AssetDatabase.Refresh();
        }

        public override void Postprocess(XEditor.Tasks.Report report)
        {
            base.Postprocess(report);

            // 删除场景
            var sceneDir = Path.GetDirectoryName(Scene);
            if (XFile.HasDirectory(sceneDir)) XFile.DeleteDirectory(sceneDir);
            AssetDatabase.Refresh();
        }
    }

    /// <summary>
    /// 测试预处理参数。
    /// </summary>
    /// <param name="type">要测试的构建类型</param>
    [TestCase(typeof(XEditor.Binary), Description = "验证默认构建类的参数生成")]
    [TestCase(typeof(MyBinary), Description = "验证自定义构建类的参数覆盖")]
    public void Prepare(Type type)
    {
        XLog.Debug("XEditor.Binary.Test.Prepare: testing build handler type: {0}.", type.Name);
        var handler = (XEditor.Binary)Activator.CreateInstance(type);
        try
        {
            handler.Preprocess(new XEditor.Tasks.Report());
            if (type == typeof(XEditor.Binary))
            {
                // 验证默认参数
                var solution = Regex.Replace(XEnv.Solution, @"[^\w\s-]", "X");
                var channel = Regex.Replace(XEnv.Channel, @"[^\w\s-]", "X");
                var mode = XEnv.Mode.ToString();

                // 构建名称格式：{Solution前3字符}-{Channel前3字符}-{Mode首字母}{LogLevel}-{日期}{序号}
                var pattern = string.Format("^{0}-{1}-{2}{3}-\\d{{8}}\\d+$",
                    solution.Length > 2 ? solution[..3].ToUpper() : solution.ToUpper(),
                    channel.Length > 2 ? channel[..3].ToUpper() : channel.ToUpper(),
                    mode.Length > 0 ? mode[0] : "D",
                    (int)XLog.Level());

                var expectedOutput = XFile.PathJoin(XEditor.Binary.Root, XEnv.Channel, XEnv.Platform.ToString());

                // 验证构建名称格式是否符合规范
                Assert.That(handler.Name, Does.Match(pattern), "构建名称应符合规范格式");

                // 验证版本号格式是否为日期加序号
                Assert.That(handler.Code, Does.Match(@"^\d{8}\d+$"), "版本号应为日期加序号格式");

                // 验证输出路径是否遵循默认规则
                Assert.That(handler.Output, Is.EqualTo(expectedOutput), "输出路径应遵循默认规则");

                Assert.That(handler.File, Does.StartWith(XFile.PathJoin(handler.Output, handler.Name)), "构建文件应位于输出目录下");
            }
            else
            {
                // 验证自定义参数
                Assert.That(handler.Output, Is.EqualTo(XFile.PathJoin(XEditor.Binary.Root, "CustomOutput")), "应使用自定义的输出路径");
                Assert.That(handler.Name, Is.EqualTo("CustomName"), "应使用自定义的构建名称");
                Assert.That(handler.Code, Is.EqualTo("202501011"), "应使用自定义的版本号");
                Assert.That(handler.Options, Is.EqualTo(BuildOptions.Development | BuildOptions.AllowDebugging), "应使用自定义的构建选项");
                Assert.That(handler.Scenes, Is.EqualTo(new string[] { "Assets/Scenes/Test.unity" }), "应使用自定义的场景列表");
            }

            // 验证目录创建
            Assert.That(XFile.HasDirectory(handler.Output), Is.True, "应成功创建输出目录");

            // 验证通用设置
            Assert.That(PlayerSettings.bundleVersion, Is.EqualTo(XEnv.Version), "应正确设置应用版本号");
            Assert.That(EditorUserBuildSettings.buildScriptsOnly, Is.False, "不应仅构建脚本");
            Assert.That(EditorUserBuildSettings.exportAsGoogleAndroidProject, Is.False, "不应导出为 Android 工程");

            // 验证平台特定设置
            if (XEnv.Platform == XEnv.PlatformType.Android)
            {
                if (int.TryParse(handler.Code, out var code))
                    Assert.That(PlayerSettings.Android.bundleVersionCode, Is.EqualTo(code), "应正确设置 Android 版本号");
            }
            else if (XEnv.Platform == XEnv.PlatformType.iOS)
            {
                Assert.That(PlayerSettings.iOS.buildNumber, Is.EqualTo(handler.Code), "应正确设置 iOS 构建号");
            }
            else if (XEnv.Platform == XEnv.PlatformType.macOS)
            {
                Assert.That(PlayerSettings.macOS.buildNumber, Is.EqualTo(handler.Code), "应正确设置 macOS 构建号");
            }

#if UNITY_ANDROID
#if UNITY_6000_0_OR_NEWER
            Assert.That(UnityEditor.Android.UserBuildSettings.DebugSymbols.level, Is.EqualTo(Unity.Android.Types.DebugSymbolLevel.Full), "应设置完整的调试符号");
            Assert.That(UnityEditor.Android.UserBuildSettings.DebugSymbols.format, Is.EqualTo(Unity.Android.Types.DebugSymbolFormat.Zip), "应使用 ZIP 格式的调试符号");
#else
            Assert.That(EditorUserBuildSettings.androidCreateSymbols, Is.EqualTo(AndroidCreateSymbols.Debugging), "应生成调试符号");
#endif
#endif
        }
        catch (Exception e)
        {
            Assert.Fail(e.Message);
        }
        finally
        {
            if (type == typeof(MyBinary))
            {
                XLog.Debug("XEditor.Binary.Test.Prepare: cleaning up custom output directory.");
                XFile.DeleteDirectory(handler.Output);
            }
        }
    }

    /// <summary>
    /// 测试完整构建流程。
    /// </summary>
    [Test]
    [Timeout(600000)]
    public void Execute()
    {
        var testDir = XFile.PathJoin(XEnv.ProjectPath, "Temp", "TestXEditorBinary");

        try
        {
            // 准备首选项数据
            var tempPrefs = new XPrefs.IBase { File = XFile.PathJoin(testDir, "Default.json") };
            tempPrefs.Set("Version", "1.0.0");
            tempPrefs.Save();

            // 设置当前首选项
            XPrefs.Asset.Read(tempPrefs.File);

            // 构建阶段
            var worker = XEditor.Tasks.Workers["Test Binary/Build Binary"] as MyBinary2;
            var report = XEditor.Tasks.Execute(worker);

            // 验证构建结果
            Assert.That(report.Result, Is.EqualTo(XEditor.Tasks.Result.Succeeded), "构建过程应成功完成");
            Assert.That(XFile.HasFile(worker.File) || XFile.HasDirectory(worker.File), Is.True, "应生成构建文件或目录");

            // 验证符号表
            // Linux平台未生成符号表，这里不作验证
            // var symbolZip = XFile.PathJoin(XEditor.Binary.Root, "Symbol", XEnv.Channel, XEnv.Platform.ToString(), handler.Name + ".zip");
            // Assert.That(XFile.HasFile(symbolZip), Is.True, "应生成符号表压缩包");

            // 运行阶段
            Assert.That(worker.Run(), Is.True, "应能成功运行构建产物");
        }
        finally
        {
            // 清理测试环境
            if (XFile.HasDirectory(testDir)) XFile.DeleteDirectory(testDir);
        }
    }
}
#endif
