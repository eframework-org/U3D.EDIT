// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using UnityEditor;
using EFramework.Utility;
using System.Threading.Tasks;
#if UNITY_6000_0_OR_NEWER
using UnityEngine;
#endif

namespace EFramework.Editor
{
    public partial class XEditor
    {
        /// <summary>
        /// XEditor.Title 拓展了编辑器标题的功能，支持在标题中显示首选项信息和 Git 版本控制信息，方便开发者快速识别当前工作环境和项目状态。
        /// </summary>
        /// <remarks>
        /// <code>
        /// 功能特性
        /// - 集成首选项信息显示：作者、渠道、版本、模式和日志级别
        /// - 集成 Git 信息显示：自动更新并显示当前工作的 Git 版本控制信息
        /// 
        /// 使用手册
        /// 1. 基本功能
        /// 
        /// 1.1 标题信息格式
        ///     首选项信息格式：`[Prefs&lt;是否修改&gt;: &lt;作者&gt;/&lt;渠道&gt;/&lt;版本&gt;/&lt;模式&gt;/&lt;日志级别&gt;]，示例：[Prefs*: Admin/Default/1.0/Test/Debug]`
        ///     
        ///     Git 信息格式：`[Git&lt;是否存在未提交的修改&gt;: &lt;分支名&gt; &lt;待推送数量&gt; &lt;待拉取数量&gt;]，示例：[Git*: master ↑1 ↓2]，[Git*: ⟳]`
        /// </code>
        /// 更多信息请参考模块文档。
        /// </remarks>
        public class Title : Event.Internal.OnEditorLoad, Event.Internal.OnPreferencesApply
        {
            /// <summary>
            /// 标识是否正在刷新 Git 信息。
            /// </summary>
            internal static bool isRefreshing = false;

            /// <summary>
            /// 首选项信息标签。
            /// </summary>
            internal static string prefsLabel = "";

            /// <summary>
            /// 当前 Git 分支名称。
            /// </summary>
            internal static string gitBranch = "";

            /// <summary>
            /// 可推送到远程的提交数量。
            /// </summary>
            internal static int gitPushCount = 0;

            /// <summary>
            /// 需要从远程拉取的提交数量。
            /// </summary>
            internal static int gitPullCount = 0;

            /// <summary>
            /// 未提交的更改数量。
            /// </summary>
            internal static int gitDirtyCount = 0;

            /// <summary>
            /// 事件处理优先级。
            /// </summary>
            int Event.Callback.Priority => 0;

            /// <summary>
            /// 是否为单例事件处理器。
            /// </summary>
            bool Event.Callback.Singleton => true;

            /// <summary>
            /// 编辑器加载时的初始化处理，注册各种事件监听并刷新标题信息。
            /// </summary>
            /// <param name="args">事件参数（未使用）。</param>
            void Event.Internal.OnEditorLoad.Process(params object[] args)
            {
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

#if UNITY_6000_0_OR_NEWER
                EditorApplication.focusChanged -= OnFocusChanged;
                EditorApplication.focusChanged += OnFocusChanged;

                EditorApplication.updateMainWindowTitle -= SetTitle;
                EditorApplication.updateMainWindowTitle += SetTitle;
#else
                var focusChangedEvent = typeof(EditorApplication).GetField("focusChanged", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                if (focusChangedEvent != null)
                {
                    var focusChangedDelegate = focusChangedEvent.GetValue(null) as Delegate;
                    var onFocusChangedDelegate = Delegate.CreateDelegate(focusChangedEvent.FieldType, null, GetType().GetMethod("OnFocusChanged", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic));
                    focusChangedEvent.SetValue(null, Delegate.Combine(focusChangedDelegate, onFocusChangedDelegate));
                }

                var updateMainWindowTitleEvent = typeof(EditorApplication).GetField("updateMainWindowTitle", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                if (updateMainWindowTitleEvent != null)
                {
                    var updateMainWindowTitleDelegate = updateMainWindowTitleEvent.GetValue(null) as Delegate;
                    var setTitleDelegate = Delegate.CreateDelegate(updateMainWindowTitleEvent.FieldType, null, GetType().GetMethod("SetTitle", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic));
                    updateMainWindowTitleEvent.SetValue(null, Delegate.Combine(updateMainWindowTitleDelegate, setTitleDelegate));
                }
#endif

                _ = Refresh();
            }

            /// <summary>
            /// 在首选项应用时的刷新逻辑。
            /// </summary>
            /// <param name="args"></param>
            void Event.Internal.OnPreferencesApply.Process(params object[] args) { _ = Refresh(); }

            /// <summary>
            /// 播放模式状态变化时刷新标题信息。
            /// </summary>
            /// <param name="state">播放模式状态。</param>
            internal void OnPlayModeStateChanged(PlayModeStateChange state) { _ = Refresh(); }

            /// <summary>
            /// 编辑器焦点变化时刷新标题信息。
            /// </summary>
            /// <param name="hasFocus">是否获得焦点。</param>
            internal void OnFocusChanged(bool hasFocus) { if (hasFocus) _ = Refresh(); }

            /// <summary>
            /// 设置 Unity 编辑器的标题，添加首选项信息和 Git 版本控制信息。
            /// </summary>
            /// <param name="des">应用程序标题描述符。</param>
            internal static void SetTitle(object des)
            {
#if UNITY_6000_0_OR_NEWER
                var descriptor = des as ApplicationTitleDescriptor;
                if (!string.IsNullOrEmpty(prefsLabel))
                {
                    descriptor.title += $" - {prefsLabel}";
                }
                if (!string.IsNullOrEmpty(gitBranch))
                {
                    var gitDirty = gitDirtyCount > 0 ? "*" : "";
                    var gitInfo = $"[Git{gitDirty}: {gitBranch}";
                    if (isRefreshing) gitInfo += " ⟳";
                    else
                    {
                        if (gitPushCount > 0) gitInfo += $" ↑{gitPushCount}";
                        if (gitPullCount > 0) gitInfo += $" ↓{gitPullCount}";
                    }
                    gitInfo += "]";
                    descriptor.title += $" - {gitInfo}";
                }
#else
                if (des == null) return;

                var titleField = des.GetType().GetField("title", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (titleField == null) return;

                var currentTitle = (string)titleField.GetValue(des);
                if (!string.IsNullOrEmpty(prefsLabel))
                {
                    currentTitle += $" - {prefsLabel}";
                }
                if (!string.IsNullOrEmpty(gitBranch))
                {
                    var gitDirty = gitDirtyCount > 0 ? "*" : "";
                    var gitInfo = $"[Git{gitDirty}: {gitBranch}";
                    if (isRefreshing) gitInfo += " ⟳";
                    else
                    {
                        if (gitPushCount > 0) gitInfo += $" ↑{gitPushCount}";
                        if (gitPullCount > 0) gitInfo += $" ↓{gitPullCount}";
                    }
                    gitInfo += "]";
                    currentTitle += $" - {gitInfo}";
                }
                titleField.SetValue(des, currentTitle);
#endif
            }

            /// <summary>
            /// 刷新标题信息，获取最新的首选项信息和 Git 版本控制状态。
            /// </summary>
            /// <returns>表示异步操作的任务。</returns>
            public static async Task Refresh()
            {
                if (isRefreshing) return;
                try
                {
                    var prefsDirty = !XFile.HasFile(XPrefs.Asset.File) || !XPrefs.Asset.Keys.MoveNext() ? "*" : "";
                    prefsLabel = $"[Prefs{prefsDirty}: {XEnv.Author}/{XEnv.Channel}/{XEnv.Version}/{XEnv.Mode}/{XLog.Level()}]";
                    isRefreshing = true;
                    await XLoom.RunInMain(() =>
                    {
#if UNITY_6000_0_OR_NEWER
                        EditorApplication.UpdateMainWindowTitle();
#else
                        var updateMainWindowTitleMethod = typeof(EditorApplication).GetMethod("UpdateMainWindowTitle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                        if (updateMainWindowTitleMethod != null) updateMainWindowTitleMethod.Invoke(null, null);
#endif
                    });

                    gitBranch = "";
                    gitPushCount = 0;
                    gitPullCount = 0;
                    gitDirtyCount = 0;

                    var branchResult = await Cmd.Run(bin: "git", print: false, args: new string[] { "branch", "--show-current" });
                    if (branchResult.Code == 0 && !string.IsNullOrEmpty(branchResult.Data))
                    {
                        gitBranch = branchResult.Data.Trim();

                        var statusResult = await Cmd.Run(bin: "git", print: false, args: new string[] { "status", "--porcelain" });
                        if (statusResult.Code == 0)
                        {
                            var changes = statusResult.Data.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                            gitDirtyCount = changes.Length;
                        }

                        var pushResult = await Cmd.Run(bin: "git", print: false, args: new string[] { "rev-list", "@{push}..HEAD", "--count" });
                        if (pushResult.Code == 0 && !string.IsNullOrEmpty(pushResult.Data))
                        {
                            int.TryParse(pushResult.Data.Trim(), out gitPushCount);
                        }

                        var pullResult = await Cmd.Run(bin: "git", print: false, args: new string[] { "rev-list", "HEAD..@{upstream}", "--count" });
                        if (pullResult.Code == 0 && !string.IsNullOrEmpty(pullResult.Data))
                        {
                            int.TryParse(pullResult.Data.Trim(), out gitPullCount);
                        }
                    }
                }
                catch (Exception e) { XLog.Panic(e, $"XEditor.Title.Refresh: {e.Message}"); }
                finally
                {
                    isRefreshing = false;
                    await XLoom.RunInMain(() =>
                    {
#if UNITY_6000_0_OR_NEWER
                        EditorApplication.UpdateMainWindowTitle();
#else
                        var updateMainWindowTitleMethod = typeof(EditorApplication).GetMethod("UpdateMainWindowTitle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                        if (updateMainWindowTitleMethod != null) updateMainWindowTitleMethod.Invoke(null, null);
#endif
                    });
                }
            }
        }
    }
}

