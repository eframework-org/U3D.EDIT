// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
#if UNITY_6000_0_OR_NEWER
using UnityEditor.Build.Profile;
#endif
using UnityEditor.Build.Reporting;
using EFramework.Utility;
using System.Runtime.InteropServices;

namespace EFramework.Editor
{
    public partial class XEditor
    {
        /// <summary>
        /// XEditor.Binary 提供了一套完整的构建流程管理系统，简化了多平台项目的构建配置，支持自动化和构建后处理。
        /// </summary>
        /// <remarks>
        /// <code>
        /// 功能特性
        /// - 多平台构建：支持 Windows、Linux、macOS、Android、iOS、WebGL 等平台
        /// - 构建配置管理：通过 BuildProfile 管理构建配置，支持版本号、签名证书等参数设置
        /// - 构建流程管理：包含预处理、构建、后处理三个阶段，支持符号表备份等功能
        /// - 可视化管理：支持构建文件的搜索、重命名、运行和目录管理
        /// 
        /// 使用手册
        /// 1. 构建配置
        /// 1.1 构建名称规则
        ///     {Solution}-{Channel}-{Mode}{LogLevel}-{DateTime}{Index}
        ///     - Solution：解决方案名称前 3 字符(大写)，如：EFU
        ///     - Channel：渠道名称前 3 字符(大写)，如：DEV
        ///     - Mode：应用模式首字母，如：D(Dev)
        ///     - LogLevel：日志等级数字，如：1
        ///     - DateTime：日期(yyyyMMdd)，如：20250325
        ///     - Index：当天构建序号，如：1
        /// 
        /// 1.2 参数配置
        /// 面板参数：
        ///     - BuildProfile：构建配置文件
        ///     - KeystoreName：安卓证书文件
        ///     - KeystorePass：安卓证书密钥
        ///     - KeyaliasName：安卓签名别名
        ///     - KeyaliasPass：安卓签名密钥
        ///     - SigningTeam：iOS 签名证书
        /// 
        /// 继承参数：
        ///     - Output：构建输出目录
        ///     - Name：构建名称
        ///     - Code：构建版本号
        ///     - Options：构建选项
        ///     - Scenes：构建场景列表
        ///     - Defines：构建定义符号列表
        ///     - File：构建输出文件
        /// 
        /// 示例：自定义构建参数
        /// internal class MyBinary : XEditor.Binary
        /// {
        ///     // 自定义输出目录
        ///     public override string Output => XFile.PathJoin(Root, "CustomOutput");
        /// 
        ///     // 自定义构建名称
        ///     public override string Name => "CustomName";
        /// 
        ///     // 自定义版本号
        ///     public override string Code => "202501011";
        /// 
        ///     // 自定义构建选项
        ///     public override BuildOptions Options => BuildOptions.Development | BuildOptions.AllowDebugging;
        /// 
        ///     // 自定义场景列表
        ///     public override string[] Scenes => new string[] { "Assets/Scenes/Test.unity" };
        /// }
        /// 
        /// 2. 构建流程
        /// 2.1 预处理阶段
        ///     - 加载 BuildProfile 配置
        ///     - 设置构建选项(Development/Debug等)
        ///     - 生成构建路径和名称
        ///     - 配置平台参数(版本号/签名等)
        /// 
        /// 2.2 构建阶段
        ///     - 执行平台构建
        ///     - 备份符号表文件
        ///     - 生成符号表压缩包
        /// 
        /// 2.3 后处理阶段
        ///     - 恢复 BuildProfile 配置
        /// 
        /// 3. 可视化面板
        /// 3.1 界面布局
        ///     +-----------------------+
        ///     |         Search        |
        ///     +-----------------------+
        ///     |    Name  Path  Run    |
        ///     |    Name  Path  Run    |
        ///     |          ...          |
        ///     +-----------------------+
        /// 
        /// 3.2 操作说明
        ///     - 搜索：按名称过滤构建文件
        ///     - 重命名：双击名称修改
        ///     - 打开目录：Path 按钮打开构建文件目录
        ///     - 运行程序：Run 按钮运行/安装构建文件
        /// </code>
        /// 更多信息请参考模块文档。
        /// </remarks>
        public partial class Binary : Tasks.Worker, Tasks.Panel.IOnGUI, Event.Internal.OnPreprocessBuild
        {
            /// <summary>
            /// 用于标记构建输出根目录路径属性的特性。
            /// 被此特性标记的静态属性将被用作构建输出根目录路径。
            /// </summary>
            [AttributeUsage(AttributeTargets.Property)]
            public class RootAttribute : Attribute { }

            /// <summary>
            /// 标记是否已初始化构建输出根目录路径。
            /// </summary>
            internal static bool root;

            /// <summary>
            /// 存储构建输出根目录路径属性信息。
            /// </summary>
            internal static PropertyInfo rootProp;

            /// <summary>
            /// 获取构建输出根目录路径。
            /// </summary>
            /// <returns>如果未通过 <see cref="RootAttribute"/> 自定义,则返回默认路径：项目目录/Builds/Binary</returns>
            public static string Root { get => Const.GetCoustom<RootAttribute, string>(ref root, ref rootProp, XFile.PathJoin(XEnv.ProjectPath, "Builds", "Binary")); }

            #region 构建参数
#if UNITY_6000_0_OR_NEWER
            /// <summary>
            /// 构建配置文件。
            /// </summary>
            [Tasks.Param(name: "Profile", tooltip: "Build Profile.", persist: true)]
            protected string ProfileFile;
#endif

            /// <summary>
            /// 安卓证书文件。
            /// </summary>
            [Tasks.Param(name: "KeyName", tooltip: "Android Keystore Name.", persist: true, platform: XEnv.PlatformType.Android)]
            protected string KeystoreName;

            /// <summary>
            /// 安卓证书密钥。
            /// </summary>
            [Tasks.Param(name: "KeyPass", tooltip: "Android Keystore Pass.", persist: true, platform: XEnv.PlatformType.Android)]
            protected string KeystorePass;

            /// <summary>
            /// 安卓签名别名。
            /// </summary>
            [Tasks.Param(name: "AliasName", tooltip: "Android Keyalias Name.", persist: true, platform: XEnv.PlatformType.Android)]
            protected string KeyaliasName;

            /// <summary>
            /// 安卓签名密钥。
            /// </summary>
            [Tasks.Param(name: "AliasPass", tooltip: "Android Keyalias Pass.", persist: true, platform: XEnv.PlatformType.Android)]
            protected string KeyaliasPass;

            /// <summary>
            /// iOS 签名证书。
            /// </summary>
            [Tasks.Param(name: "Signing", tooltip: "iOS Signing Team.", persist: true, platform: XEnv.PlatformType.iOS)]
            protected string SigningTeam;
            #endregion

#if UNITY_6000_0_OR_NEWER
            /// <summary>
            /// 上一次使用的构建配置。
            /// </summary>
            protected BuildProfile lastProfile;

            /// <summary>
            /// 当前使用的构建配置。
            /// </summary>
            protected BuildProfile profile;

            /// <summary>
            /// 获取当前构建配置。
            /// </summary>
            /// <returns>当前正在使用的 BuildProfile 配置</returns>
            protected virtual BuildProfile Profile { get => profile; }
#endif

            /// <summary>
            /// 构建输出目录。
            /// </summary>
            private string output;

            /// <summary>
            /// 获取构建输出目录。
            /// </summary>
            /// <returns>构建文件的输出目录路径</returns>
            public virtual string Output { get => output; }

            /// <summary>
            /// 构建名称。
            /// </summary>
            protected string name;

            /// <summary>
            /// 获取构建名称。
            /// </summary>
            /// <returns>当前构建的名称</returns>
            public virtual string Name { get => name; }

            /// <summary>
            /// 构建版本号。
            /// </summary>
            private string code;

            /// <summary>
            /// 获取构建版本号。
            /// </summary>
            /// <returns>当前构建的版本号</returns>
            public virtual string Code { get => code; }

            /// <summary>
            /// 构建输出文件。
            /// </summary>
            private string file;

            /// <summary>
            /// 获取构建输出文件。
            /// </summary>
            /// <returns>构建输出的目标文件路径</returns>
            public virtual string File { get => file; }

            /// <summary>
            /// 构建选项。
            /// </summary>
            private BuildOptions options;

            /// <summary>
            /// 获取构建选项。
            /// </summary>
            /// <returns>当前构建使用的 BuildOptions 选项</returns>
            public virtual BuildOptions Options { get => options; }

            /// <summary>
            /// 获取构建场景列表。
            /// </summary>
            /// <returns>优先使用 BuildProfile 中配置的场景,如果没有配置则使用 EditorBuildSettings 中的场景</returns>
            public virtual string[] Scenes
            {
                get
                {
                    string[] scenes = null;
#if UNITY_6000_0_OR_NEWER
                    var profile = BuildProfile.GetActiveBuildProfile();
                    if (profile && profile.scenes != null && profile.scenes.Length > 0)
                    {
                        scenes = profile.scenes.Select(s => s.path).ToArray();
                    }
#endif
                    scenes ??= EditorBuildSettings.scenes.Select(s => s.path).ToArray();
                    return scenes;
                }
            }

            /// <summary>
            /// 获取构建定义符号列表。
            /// </summary>
            /// <returns>构建时使用的预处理器定义符号列表</returns>
            public virtual string[] Defines { get; }

            /// <summary>
            /// 构建预处理回调。
            /// </summary>
            /// <param name="args">构建报告参数</param>
            /// <remarks>用于在构建开始前设置定义符号</remarks>
            void Event.Internal.OnPreprocessBuild.Process(params object[] args)
            {
                if (Defines == null || Defines.Length == 0) return;
                Event.Decode<BuildReport>(out var report, args);
                var buildTarget = NamedBuildTarget.FromBuildTargetGroup(report.summary.platformGroup);
                PlayerSettings.GetScriptingDefineSymbols(buildTarget, out var temps);
                var defines = new List<string>(temps);
                foreach (var define in Defines)
                {
                    if (!defines.Contains(define)) defines.Add(define);
                }
                PlayerSettings.SetScriptingDefineSymbols(buildTarget, defines.ToArray());
                XLog.Debug("XEditor.Binary.OnPreprocessBuild: set define symbols: {0}.", string.Join(",", defines));
            }

            /// <summary>
            /// 构建预处理阶段。
            /// </summary>
            /// <param name="report">处理报告</param>
            /// <remarks>
            /// 执行以下操作：
            /// - 设置构建参数
            /// - 配置构建选项
            /// - 准备构建环境
            /// </remarks>
            public override void Preprocess(Tasks.Report report)
            {
                #region 构建参数
#if UNITY_6000_0_OR_NEWER
                if (!string.IsNullOrEmpty(ProfileFile))
                {
                    if (!XFile.HasFile(ProfileFile))
                    {
                        XLog.Warn("XEditor.Binary.Preprocess: cannot found build profile file: {0}.", ProfileFile);
                    }
                    else
                    {
                        profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(ProfileFile);
                        if (profile == null) throw new Exception($"XEditor.Binary.Preprocess: cannot load build profile file: {ProfileFile}");
                        profile = UnityEngine.Object.Instantiate(profile);
                        lastProfile = BuildProfile.GetActiveBuildProfile();
                        BuildProfile.SetActiveBuildProfile(profile);
                        XLog.Debug("XEditor.Binary.Preprocess: set build profile: {0}.", profile.name);
                    }
                }
#endif

                // 构建选项
                if (Options != BuildOptions.None) options = Options;
                else
                {
                    options = BuildOptions.None;
                    if (Debug.isDebugBuild || XEnv.Mode == XEnv.ModeType.Dev)
                    {
                        var developmentBuild = EditorUserBuildSettings.development;
                        if (developmentBuild)
                            options |= BuildOptions.Development;
                        if (EditorUserBuildSettings.allowDebugging && developmentBuild)
                            options |= BuildOptions.AllowDebugging;
                        if (EditorUserBuildSettings.symlinkSources)
                            options |= BuildOptions.SymlinkSources;
                        if (EditorUserBuildSettings.connectProfiler && developmentBuild)
                            options |= BuildOptions.ConnectWithProfiler;
                        if (EditorUserBuildSettings.buildWithDeepProfilingSupport && developmentBuild)
                            options |= BuildOptions.EnableDeepProfilingSupport;
                    }
                }

                // 构建路径
                if (!string.IsNullOrEmpty(Output)) output = Output;
                else output = XFile.PathJoin(Root, XEnv.Channel, XEnv.Platform.ToString());
                if (!XFile.HasDirectory(output)) XFile.CreateDirectory(output);

                // 构建名称
                var max = 1;
                var datetime = DateTime.Now.ToString("yyyyMMdd");
                var di = new DirectoryInfo(output);
                var isf = true;
                FileSystemInfo[] fis = di.GetFiles().Where(f => f.Name != ".DS_Store").ToArray();
                if (fis.Length == 0)
                {
                    isf = false;
                    fis = di.GetDirectories();
                }
                if (fis != null && fis.Length > 0)
                {
                    for (var i = 0; i < fis.Length; i++)
                    {
                        var file = fis[i];
                        if (file == null) continue;
                        var name = isf ? Path.GetFileNameWithoutExtension(file.Name) : file.Name;
                        if (string.IsNullOrEmpty(name)) continue;
                        var buildIndex = name.LastIndexOf("-");
                        if (buildIndex == -1) continue;
                        name = name[(buildIndex + 1)..];
                        if (name.StartsWith(datetime))
                        {
                            name = name.Replace(datetime, "");
                            int.TryParse(name, out var index);
                            if (index >= max) max = index + 1;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(Name)) name = Name;
                else
                {
                    var pattern = @"[^\w\s-]";
                    var solution = Regex.Replace(XEnv.Solution, pattern, "X");
                    var channel = Regex.Replace(XEnv.Channel, pattern, "X");
                    var mode = XEnv.Mode.ToString();
                    name = XString.Format("{0}-{1}-{2}{3}-{4}{5}",
                       solution.Length > 2 ? solution[..3].ToUpper() : solution.ToUpper(),
                       channel.Length > 2 ? channel[..3].ToUpper() : channel.ToUpper(),
                       mode.Length > 0 ? mode[0] : "D",
                       (int)XLog.Level(),
                       datetime,
                        max);
                }

                if (!string.IsNullOrEmpty(Code)) code = Code;
                else code = datetime + max;

                // 构建文件
                if (!string.IsNullOrEmpty(File)) file = File;
                else
                {
                    if (XEnv.Platform == XEnv.PlatformType.Android) file = XFile.PathJoin(output, Name + ".apk");
                    else if (XEnv.Platform == XEnv.PlatformType.Windows) file = XFile.PathJoin(output, Name, Name + ".exe");
                    else if (XEnv.Platform == XEnv.PlatformType.Linux) file = XFile.PathJoin(output, Name, Name + ".bin");
                    else if (XEnv.Platform == XEnv.PlatformType.macOS) file = XFile.PathJoin(output, Name, Name + ".app");
                    else file = XFile.PathJoin(output, Name);
                }
                #endregion

                #region 平台设置 
                if (XEnv.Platform == XEnv.PlatformType.Android)
                {
                    if (int.TryParse(Code, out var icode)) PlayerSettings.Android.bundleVersionCode = icode;
                    if (!string.IsNullOrEmpty(KeystoreName)) PlayerSettings.Android.keystoreName = KeystoreName;
                    if (!string.IsNullOrEmpty(KeystorePass)) PlayerSettings.Android.keystorePass = KeystorePass;
                    if (!string.IsNullOrEmpty(KeyaliasName)) PlayerSettings.Android.keyaliasName = KeyaliasName;
                    if (!string.IsNullOrEmpty(KeyaliasPass)) PlayerSettings.Android.keyaliasPass = KeyaliasPass;
                }
                else if (XEnv.Platform == XEnv.PlatformType.iOS)
                {
                    PlayerSettings.iOS.buildNumber = Code;
                    if (!string.IsNullOrEmpty(SigningTeam)) PlayerSettings.iOS.appleDeveloperTeamID = SigningTeam;
                }
                else if (XEnv.Platform == XEnv.PlatformType.macOS)
                {
                    PlayerSettings.macOS.buildNumber = Code;
                }

                PlayerSettings.bundleVersion = XEnv.Version;

                EditorUserBuildSettings.buildScriptsOnly = false;
#if UNITY_EDITOR_WIN
                UnityEditor.WindowsStandalone.UserBuildSettings.createSolution = false;
#endif
#if UNITY_EDITOR_OSX
                UnityEditor.OSXStandalone.UserBuildSettings.createXcodeProject = false;
#endif
#if TUANJIE_2022
                EditorUserBuildSettings.exportAsOpenHarmonyProject = false;
#endif
#if UNITY_ANDROID
#if UNITY_6000_0_OR_NEWER
                UnityEditor.Android.UserBuildSettings.DebugSymbols.level = Unity.Android.Types.DebugSymbolLevel.Full;
                UnityEditor.Android.UserBuildSettings.DebugSymbols.format = Unity.Android.Types.DebugSymbolFormat.Zip;
#else
                EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Debugging;
#endif
                EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
#endif
                #endregion
            }

            /// <summary>
            /// 构建处理阶段。
            /// 执行构建操作、处理构建结果、备份符号表。
            /// </summary>
            /// <param name="report">处理报告</param>
            public override void Process(Tasks.Report report)
            {
                #region 构建安装包 
                BuildReport rret = null;

                if (XEnv.Platform == XEnv.PlatformType.Windows) rret = BuildPipeline.BuildPlayer(Scenes, File, BuildTarget.StandaloneWindows64, Options);
                else if (XEnv.Platform == XEnv.PlatformType.Linux) rret = BuildPipeline.BuildPlayer(Scenes, File, BuildTarget.StandaloneLinux64, Options);
                else if (XEnv.Platform == XEnv.PlatformType.macOS) rret = BuildPipeline.BuildPlayer(Scenes, File, BuildTarget.StandaloneOSX, Options);
                else if (XEnv.Platform == XEnv.PlatformType.Android) rret = BuildPipeline.BuildPlayer(Scenes, File, BuildTarget.Android, Options);
                else if (XEnv.Platform == XEnv.PlatformType.iOS) rret = BuildPipeline.BuildPlayer(Scenes, File, BuildTarget.iOS, Options);
                else if (XEnv.Platform == XEnv.PlatformType.Browser) rret = BuildPipeline.BuildPlayer(Scenes, File, BuildTarget.WebGL, Options);
                else report.Error = $"Unsupported platform: {XEnv.Platform}";
                if (rret == null) report.Result = Tasks.Result.Failed;
                else
                {
                    report.Result = (Tasks.Result)rret.summary.result;
                    if (rret.summary.result != BuildResult.Succeeded) report.Error = $"BuildPipeline with {rret.summary.totalErrors} error(s).";
                }
                if (report.Current.Result == Tasks.Result.Succeeded) XLog.Debug("XEditor.Binary.Process: build <a href=\"file:///{0}\">{1}</a> succeed.", Path.GetFullPath(File), Name);
                else XLog.Error("XEditor.Binary.Process: build <a href=\"file:///{0}\">{1}</a> failed.", Path.GetFullPath(File), Name);
                #endregion

                #region 备份符号表
                var symbolRoot = XFile.PathJoin(Root, "Symbol", XEnv.Channel, XEnv.Platform.ToString());
                var symbolZip = XFile.PathJoin(symbolRoot, Name + ".zip");
                var symbolPath = XFile.PathJoin(symbolRoot, Name);
                var brustSrc = "";
                var brustDst = XFile.PathJoin(symbolPath, "BurstDebugInformation_DoNotShip");
                var symbolSrc = "";
                var symbolDst = XFile.PathJoin(symbolPath, "BackUpThisFolder_ButDontShipItWithYourGame");
                if (XEnv.Platform == XEnv.PlatformType.Windows || XEnv.Platform == XEnv.PlatformType.Linux || XEnv.Platform == XEnv.PlatformType.macOS)
                {
                    var temp = XFile.PathJoin(Output, Name);
                    if (XFile.HasDirectory(temp))
                    {
                        var dirs = Directory.GetDirectories(temp);
                        var bin = "";
                        foreach (var dir in dirs)
                        {
                            if (dir.EndsWith("_Data"))
                            {
                                bin = Path.GetFileName(dir).Replace("_Data", "");
                                break;
                            }
                        }
                        brustSrc = XFile.PathJoin(Output, Name, bin + "_BurstDebugInformation_DoNotShip");
                        if (!XFile.HasDirectory(brustSrc))
                        {
                            // Unity6首次编译brust的目录为 product_xxx
                            brustSrc = XFile.PathJoin(Output, Name, Application.productName + "_BurstDebugInformation_DoNotShip");
                        }
                        symbolSrc = XFile.PathJoin(Output, Name, bin + "_BackUpThisFolder_ButDontShipItWithYourGame");
                    }
                }
                else if (XEnv.Platform == XEnv.PlatformType.Android)
                {
                    var bin = Path.GetFileNameWithoutExtension(File);
                    brustSrc = XFile.PathJoin(Output, bin + "_BurstDebugInformation_DoNotShip");
                    if (!XFile.HasDirectory(brustSrc))
                    {
                        // Unity6首次编译brust的目录为product_xxx
                        brustSrc = XFile.PathJoin(Output, Application.productName + "_BurstDebugInformation_DoNotShip");
                    }
                    symbolSrc = XFile.PathJoin(Output, bin + "_BackUpThisFolder_ButDontShipItWithYourGame");
                    var files = Directory.GetFiles(Output);
                    for (int i = 0; i < files.Length; i++)
                    {
                        var f = files[i];
                        if (f.Contains(Name) && f.EndsWith("symbols.zip"))
                        {
                            if (XFile.HasDirectory(symbolPath) == false) XFile.CreateDirectory(symbolPath);
                            XFile.CopyFile(f, XFile.PathJoin(symbolPath, Path.GetFileName(f)));
                            XFile.DeleteFile(f);
                            break;
                        }
                    }
                }
                if (XFile.HasDirectory(brustSrc) || XFile.HasDirectory(symbolSrc))
                {
                    if (XFile.HasDirectory(symbolPath) == false) XFile.CreateDirectory(symbolPath);

                    if (XFile.HasFile(symbolZip)) XFile.DeleteFile(symbolZip);
                    if (XFile.HasDirectory(brustSrc))
                    {
                        XFile.CopyDirectory(brustSrc, brustDst);
                        XFile.DeleteDirectory(brustSrc);
                    }
                    if (XFile.HasDirectory(symbolSrc))
                    {
                        XFile.CopyDirectory(symbolSrc, symbolDst);
                        XFile.DeleteDirectory(symbolSrc);
                    }
                    Utility.ZipDirectory(symbolPath, symbolZip);
                    XFile.DeleteDirectory(symbolPath);
                    XLog.Debug("XEditor.Binary.Process: backup symbols of <a href=\"file:///{0}\">{1}</a> succeed.", Path.GetFullPath(symbolZip), Name);
                }
                #endregion
            }

            /// <summary>
            /// 构建后处理阶段。
            /// 恢复构建配置。
            /// </summary>
            /// <param name="report">处理报告</param>
            public override void Postprocess(Tasks.Report report)
            {
#if UNITY_6000_0_OR_NEWER
                try
                {
                    // 引擎：Unity 6.0.32f1

                    // 问题1：编译后立即恢复报错：AssertionException: Build profile is null
                    // 原因：ScriptableObject 在构建时被销毁，AssetDatabase.LoadAssetAtPath 重新加载的对象也不行
                    // 解决：使用try catch 捕获异常，可以正常恢复

                    // 问题2：batchMode 模式下，构建时虽然使用Profile，但是设置版本号等参数时，会修改ProjectSettings.asset，GUI模式下正常
                    if (lastProfile) BuildProfile.SetActiveBuildProfile(lastProfile);
                }
                catch { }
#endif
            }

            #region 显示面板
            /// <summary>
            /// 构建文件搜索关键字。
            /// </summary>
            protected string searchStr = "";

            /// <summary>
            /// 是否正在安装。
            /// </summary>
            protected bool installing;

            /// <summary>
            /// 运行指定的构建文件。
            /// </summary>
            /// <param name="path">构建文件路径,默认为当前构建文件路径</param>
            /// <param name="name">构建文件名称,默认为当前构建名称</param>
            /// <returns>是否成功启动</returns>
            public virtual bool Run(string path = "", string name = "")
            {
                if (string.IsNullOrEmpty(path)) path = XFile.HasFile(File) ? Path.GetDirectoryName(File) : File;
                if (string.IsNullOrEmpty(name)) name = Name;
                if (XEnv.Platform == XEnv.PlatformType.Windows ||
                    XEnv.Platform == XEnv.PlatformType.Linux ||
                    XEnv.Platform == XEnv.PlatformType.macOS)
                {
                    var dirs = Directory.GetDirectories(path);
                    var bin = "";
                    foreach (var dir in dirs)
                    {
                        if (dir.EndsWith("_Data"))
                        {
                            bin = Path.GetFileName(dir).Replace("_Data", "");
                        }
                    }
                    var files = Directory.GetFiles(path);
                    foreach (var file in files)
                    {
                        if (Path.GetFileNameWithoutExtension(file) == bin)
                        {
                            bin = file;
                            break;
                        }
                    }
                    if (XFile.HasFile(bin) == false)
                    {
                        XLog.Error("XEditor.Binary.Run: cannot found executable file: {0}.", path);
                        return false;
                    }
                    else
                    {
                        var proc = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = bin,
                            WorkingDirectory = path
                        };
                        System.Diagnostics.Process.Start(proc);
                        XLog.Debug("XEditor.Binary.Run: running <a href=\"file:///{0}\">{1}</a>.", Path.GetFullPath(bin), name);
                        return true;
                    }
                }
                else if (XEnv.Platform == XEnv.PlatformType.Android)
                {
                    if (installing)
                    {
                        XLog.Error("XEditor.Binary.Run: previous is still installing.");
                        return false;
                    }

                    var sdkRoot = XFile.PathJoin(BuildPipeline.GetPlaybackEngineDirectory(BuildTarget.Android, BuildOptions.None), "SDK", "platform-tools");
                    installing = true;
                    XLog.Debug("XEditor.Binary.Run: installing <a href=\"file:///{0}\">{1}</a>.", Path.GetFullPath(path), name);

                    XLoom.RunAsync(() =>
                    {
                        var task = Cmd.Run(bin: Cmd.Find("adb", sdkRoot), args: new string[] { "install -r", path });
                        task.Wait();
                        installing = false;
                        if (task.Result.Data.Contains("Success"))
                        {
                            var activity = "";
                            XLoom.RunInMain(() => activity = "\"" + Application.identifier + "/com.unity3d.player.UnityPlayerActivity\"").Wait();
                            task = Cmd.Run(bin: Cmd.Find("adb", sdkRoot), args: new string[] { "-d shell", "am start -a android.intent.action.MAIN -c android.intent.category.LAUNCHER -S -f 0x10200000 -n", activity });
                            task.Wait();
                        }
                    });
                    return true;
                }
                else if (XEnv.Platform == XEnv.PlatformType.iOS)
                {
                    XLoom.RunAsync(() => Cmd.Run(bin: "open", args: new string[] { XFile.PathJoin(path, "Unity-iPhone.xcodeproj") }).Wait());
                    return true;
                }
                else
                {
                    XLog.Warn("XEditor.Binary.Run: non supported platform: {0}.", XEnv.Platform);
                    return false;
                }
            }

            /// <summary>
            /// 绘制构建面板。
            /// </summary>
            /// <remarks>
            /// 提供以下功能：
            /// - 构建文件搜索
            /// - 文件重命名
            /// - 运行构建文件
            /// - 打开目录功能
            /// </remarks>
            public virtual void OnGUI()
            {
                searchStr = EditorGUILayout.TextField(searchStr, EditorStyles.toolbarSearchField);

                var root = XFile.PathJoin(Root, XEnv.Channel, XEnv.Platform.ToString());
                if (XFile.HasDirectory(root))
                {
                    var di = new DirectoryInfo(root);
                    var archs = new List<string>();
                    var isf = true;
                    FileSystemInfo[] fis = di.GetFiles().Where(f => f.Name != ".DS_Store").ToArray();
                    if (fis.Length == 0)
                    {
                        isf = false;
                        fis = di.GetDirectories();
                    }

                    if (isf) archs.AddRange(Directory.GetFiles(root));
                    else archs.AddRange(Directory.GetDirectories(root));
                    archs.Sort((p1, p2) =>
                    {
                        FileSystemInfo f1 = isf ? new FileInfo(p1) : new DirectoryInfo(p1);
                        FileSystemInfo f2 = isf ? new FileInfo(p2) : new DirectoryInfo(p2);
                        return f2.LastWriteTime.CompareTo(f1.LastWriteTime);
                    });
                    for (int i = 0; i < archs.Count; i++)
                    {
                        var path = archs[i];
                        var name = Path.GetFileName(path);
                        if (name.IndexOf(searchStr, StringComparison.OrdinalIgnoreCase) < 0) continue;
                        GUILayout.BeginHorizontal();
                        var str = EditorGUILayout.DelayedTextField(name);
                        if (str != name)
                        {
                            if (isf)
                            {
                                var dst = XFile.PathJoin(root, str);
                                var ext = Path.GetExtension(path);
                                if (dst.EndsWith(ext) == false) dst += ext;
                                if (XFile.HasFile(dst)) XFile.HasFile(dst);
                                XFile.CopyFile(path, dst);
                                XFile.DeleteFile(path);
                            }
                            else
                            {
                                var dst = XFile.PathJoin(root, str);
                                if (XFile.HasDirectory(dst)) XFile.DeleteDirectory(dst);
                                XFile.CopyDirectory(path, dst);
                                XFile.DeleteDirectory(path);
                                XFile.CopyDirectory(XFile.PathJoin(dst, name + "_Data/"), XFile.PathJoin(dst, str + "_Data/"));
                                XFile.DeleteDirectory(XFile.PathJoin(dst, name + "_Data/"));
                                XFile.CopyFile(XFile.PathJoin(dst, name + ".exe"), XFile.PathJoin(dst, str + ".exe"));
                                XFile.DeleteFile(XFile.PathJoin(dst, name + ".exe"));
                            }
                        }
                        if (GUILayout.Button(new GUIContent("Path", $"Show {name} in explorer"))) Utility.ShowInExplorer(path);
                        if (GUILayout.Button(new GUIContent("Run", $"Run {name}")))
                        {
                            Run(path, name);
                        }
                        GUILayout.EndHorizontal();
                    }
                }
            }
            #endregion
        }
    }
}
