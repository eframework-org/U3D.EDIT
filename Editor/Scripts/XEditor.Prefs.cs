// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.Build;
using UnityEngine.UIElements;
using UnityEditor.TestTools.TestRunner.Api;
using EFramework.Utility;

namespace EFramework.Editor
{
    public partial class XEditor
    {
        /// <summary>
        /// XEditor.Prefs 提供了编辑器首选项的加载和应用功能，支持自动收集和组织首选项面板、配置持久化和构建预处理。
        /// </summary>
        /// <remarks>
        /// <code>
        /// 功能特性
        /// - 面板管理：基于 Unity SettingsProvider 组织首选项面板，提供可视化的配置管理界面
        /// - 构建预处理：在构建时处理和验证首选项，支持变量求值和编辑器配置清理
        /// 
        /// 使用手册
        /// 1. 用户交互
        /// 
        /// 1.1 打开界面
        /// - 通过菜单：EFramework/Preferences
        /// - 快捷键：Ctrl+R
        /// - 代码调用：XEditor.Prefs.Open()
        /// 
        /// 1.2 配置操作
        /// - 保存配置：点击底部工具栏的"Save"按钮
        /// - 应用配置：点击底部工具栏的"Apply"按钮
        /// - 克隆配置：点击顶部工具栏的"Clone"按钮
        /// - 删除配置：点击顶部工具栏的"Delete"按钮
        /// 
        /// 1.3 面板导航
        /// - 区域折叠：点击区域标题前的折叠箭头
        /// - 配置切换：使用顶部下拉列表切换不同配置文件
        /// - 文件定位：点击配置文件右侧的"定位"按钮
        /// 
        /// 2. 自定义面板
        /// 
        /// 2.1 面板定义
        ///     public class MyPrefsPanel : XPrefs.Panel
        ///     {
        ///         // 面板所属区域
        ///         public override string Section => "MySection";
        ///         
        ///         // 面板提示信息
        ///         public override string Tooltip => "My Panel";
        ///         
        ///         // 是否支持折叠
        ///         public override bool Foldable => true;
        ///         
        ///         // 面板优先级（数值越小越靠前）
        ///         public override int Priority => 0;
        ///     }
        /// 
        /// 2.2 生命周期
        ///     public class MyPrefsPanel : XPrefs.Panel
        ///     {
        ///         // 面板激活时调用
        ///         public override void OnActivate(string searchContext, VisualElement root)
        ///         {
        ///             // 初始化面板
        ///         }
        ///         
        ///         // 绘制界面时调用
        ///         public override void OnVisualize(string searchContext)
        ///         {
        ///             // 绘制配置界面
        ///         }
        /// 
        ///         // 面板停用时调用
        ///         public override void OnDeactivate()
        ///         {
        ///             // 清理资源
        ///         }
        /// 
        ///         // 保存配置时调用
        ///         public override bool Validate()
        ///         {
        ///             // 验证配置有效性
        ///             return true;
        ///         }
        /// 
        ///         // 保存配置时调用
        ///         public override void OnSave()
        ///         {
        ///             // 保存配置
        ///         }
        ///         
        ///         // 应用配置时调用
        ///         public override void OnApply()
        ///         {
        ///             // 应用配置
        ///         }
        ///     }
        /// 
        /// 3. 构建预处理
        /// 
        /// 3.1 变量求值
        ///     {
        ///         "build_path": "${Env.ProjectPath}/Build",
        ///         "version": "${Env.Version}",
        ///         "const_value@Const": "${Env.LocalPath}"  // @Const 标记的值不会被求值
        ///     }
        /// 
        /// 3.2 编辑器配置
        ///     {
        ///         "normal_key": "runtime_value",
        ///         "editor_key@Editor": "editor_value"  // @Editor 标记的配置在构建时会被移除
        ///     }
        /// 
        /// 3.3 预处理流程
        /// 构建时会进行以下检查：
        /// 1. 检查首选项文件是否存在
        /// 2. 验证首选项内容是否有效
        /// 3. 对首选项进行变量引用求值
        /// 4. 保存处理后的首选项文件至 StreamingAssets/Preferences.json
        /// </code>
        /// 更多信息请参考模块文档。
        /// </remarks>
        public class Prefs : SettingsProvider, Event.Internal.OnEditorLoad, Event.Internal.OnPreprocessBuild
        {
            #region 静态成员
            /// <summary>
            /// 用于标记首选项根目录路径属性的特性。
            /// 被此特性标记的静态属性将被用作首选项根目录路径。
            /// </summary>
            [AttributeUsage(AttributeTargets.Property)]
            public class RootAttribute : Attribute { }

            /// <summary>
            /// 标记是否已初始化首选项根目录路径。
            /// </summary>
            internal static bool root;

            /// <summary>
            /// 存储首选项根目录路径属性信息。
            /// </summary>
            internal static PropertyInfo rootProp;

            /// <summary>
            /// 获取首选项根目录路径。
            /// 如果未通过 <see cref="RootAttribute"/> 自定义，则返回默认路径：项目目录/Docs/Prefs。
            /// </summary>
            public static string Root { get => Const.GetCoustom<RootAttribute, string>(ref root, ref rootProp, XFile.PathJoin(XEnv.ProjectPath, "ProjectSettings", "Preferences")); }

            /// <summary>
            /// 配置后缀，用于标识首选项文件类型。
            /// </summary>
            public const string Extension = ".json";

            /// <summary>
            /// 菜单路径，定义了在 Unity 主菜单中的位置。
            /// 快捷键为 #r（Ctrl+R）
            /// </summary>
            internal const string MenuPath = "EFramework/Preferences #r";

            /// <summary>
            /// 项目设置菜单路径，定义了在 Project Settings 窗口中的位置。
            /// </summary>
            internal const string ProjMenu = "Project/EFramework/Preferences";

            /// <summary>
            /// 打开首选项设置窗口。
            /// </summary>
            [MenuItem(MenuPath)]
            public static void Open() { SettingsService.OpenProjectSettings(ProjMenu); }

            internal static Prefs Instance = new();

            /// <summary>
            /// 提供首选项提供者组，用于 Unity 编辑器设置系统的注册。
            /// </summary>
            /// <returns>包含本首选项提供者的数组。</returns>
            [SettingsProviderGroup]
            internal static SettingsProvider[] Provider() { return new SettingsProvider[] { Instance }; }

            internal Prefs() : base(ProjMenu, SettingsScope.Project) { }
            #endregion

            #region 类型成员
            /// <summary>
            /// 当前活动的首选项目标对象。
            /// </summary>
            internal XPrefs.IBase activeTarget;

            /// <summary>
            /// 当前选中的首选项索引。
            /// </summary>
            internal int activeIndex = -1;

            /// <summary>
            /// 所有首选项面板列表。
            /// </summary>
            internal List<XPrefs.IPanel> panels;

            /// <summary>
            /// 首选项面板缓存，按类型索引。
            /// </summary>
            internal readonly Dictionary<Type, XPrefs.IPanel> panelCache = new();

            /// <summary>
            /// 按区域分组的首选项面板列表。
            /// </summary>
            internal List<List<XPrefs.IPanel>> sections;

            /// <summary>
            /// 各区域折叠状态字典。
            /// </summary>
            internal readonly Dictionary<string, bool> foldouts = new();

            /// <summary>
            /// 首选项窗口的根视觉元素。
            /// </summary>
            internal VisualElement visualElement;

            /// <summary>
            /// 重新加载所有首选项面板。
            /// 收集所有实现了 XPrefs.IPanel 接口的类型，并创建面板实例。
            /// 将面板按区域分组并排序。
            /// </summary>
            /// <param name="searchContext">搜索上下文字符串</param>
            internal void Reload(string searchContext = "")
            {
                panels = new List<XPrefs.IPanel>();
                var types = TypeCache.GetTypesDerivedFrom<XPrefs.IPanel>();

                foreach (var type in types)
                {
                    try
                    {
                        if (!panelCache.TryGetValue(type, out var obj) || (obj is ScriptableObject sobj && sobj == null))
                        {
                            if (type.IsSubclassOf(typeof(ScriptableObject)))
                            {
                                obj = ScriptableObject.CreateInstance(type) as XPrefs.IPanel;
                            }
                            else
                            {
                                obj = Activator.CreateInstance(type) as XPrefs.IPanel;
                            }
                            if (obj != null)
                            {
                                panelCache[type] = obj;
                            }
                        }

                        if (obj != null)
                        {
                            obj.Target = activeTarget;
                            panels.Add(obj);
                        }
                    }
                    catch (Exception e) { XLog.Panic(e); }
                }

                panels.Sort((e1, e2) => e1.Priority.CompareTo(e2.Priority));

                sections = new List<List<XPrefs.IPanel>>();
                for (var i = 0; i < panels.Count; i++)
                {
                    var target = panels[i];
                    List<XPrefs.IPanel> group = null;
                    for (var j = 0; j < sections.Count; j++)
                    {
                        var temp = sections[j];
                        if (temp != null && temp.Count > 0 && temp[0].Section == target.Section)
                        {
                            group = temp;
                            break;
                        }
                    }
                    if (group == null)
                    {
                        group = new List<XPrefs.IPanel>();
                        sections.Add(group);
                    }
                    group.Add(target);
                }

                foreach (var panel in panels)
                {
                    try
                    {
                        panel.Target = activeTarget;
                        panel.OnActivate(searchContext, visualElement);
                    }
                    catch (Exception e) { XLog.Panic(e); }
                }
            }

            /// <summary>
            /// 当首选项面板被激活时调用。
            /// 初始化根视觉元素并重新加载面板。
            /// </summary>
            /// <param name="searchContext">搜索上下文字符串</param>
            /// <param name="rootElement">根视觉元素</param>
            public override void OnActivate(string searchContext, VisualElement rootElement)
            {
                visualElement = rootElement;
                Reload(searchContext);
            }

            /// <summary>
            /// 绘制首选项面板的标题栏界面。
            /// 显示可用的首选项文件列表，并提供克隆、删除等操作。
            /// </summary>
            public override void OnTitleBarGUI()
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                var names = new List<string>();
                var files = new List<string>();
                Utility.CollectFiles(Root, files);
                for (var i = 0; i < files.Count;)
                {
                    var file = files[i];
                    if (file.EndsWith(Extension)) i++;
                    else files.RemoveAt(i);
                }
                files.Sort((e1, e2) =>
                {
                    var s1 = e1.Contains(".template", StringComparison.OrdinalIgnoreCase);
                    var s2 = e2.Contains(".template", StringComparison.OrdinalIgnoreCase);
                    if (!s1 && s2) return -1;
                    if (s1 && !s2) return 1;
                    return StringComparer.OrdinalIgnoreCase.Compare(e1, e2);
                });
                names = files.Select(ele => Path.GetFileName(ele)).ToList();
                if (activeTarget != null && activeTarget.File == XPrefs.Asset.File && !activeTarget.Dirty && !activeTarget.Equals(XPrefs.Asset))
                {
                    activeIndex = -1; // 当前应用的首选项已经被修改，这里重置后重新读取它
                }
                if (activeIndex > files.Count) activeIndex = -1; // 数组索引超过本地首选项文件数
                if (activeIndex == -1) // 尝试加载当前应用的首选项
                {
                    if (XFile.HasFile(XPrefs.IAsset.Uri))
                    {
                        activeTarget ??= new XPrefs.IBase();
                        if (activeTarget.Read(XPrefs.IAsset.Uri))
                        {
                            for (int i = 0; i < files.Count; i++)
                            {
                                var f = files[i];
                                var p = new XPrefs.IBase();
                                if (p.Read(f) && activeTarget.Equals(p))
                                {
                                    activeIndex = i;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (activeTarget == null || !string.IsNullOrEmpty(activeTarget.Error)) // 新建一个首选项实例
                {
                    activeTarget = new XPrefs.IBase();
                    if (files.Count > 0) activeIndex = 0; // 使用第一个首选项
                }

                GUILayout.BeginHorizontal();
                var invalidTarget = activeIndex == -1;
                var ocolor = GUI.color;
                if (invalidTarget) GUI.color = Color.gray;

                var lastIndex = activeIndex;
                activeIndex = EditorGUILayout.Popup(activeIndex, names.ToArray());
                if (lastIndex != activeIndex)
                {
                    activeTarget = new XPrefs.IBase();
                    activeTarget.Read(files[activeIndex]);
                }
                if (GUILayout.Button(new GUIContent("", EditorGUIUtility.FindTexture("UnityEditor.ConsoleWindow"))))
                {
                    if (invalidTarget) return;
                    EditorApplication.delayCall += () => Utility.ShowInExplorer(files[activeIndex]);
                }
                if (GUILayout.Button(new GUIContent("Delete", EditorGUIUtility.FindTexture("TreeEditor.Trash"))))
                {
                    if (invalidTarget) return;
                    EditorApplication.delayCall += () =>
                    {
                        if (files[activeIndex].Contains(".template") && EditorUtility.DisplayDialog("Warning", $"Delete the template preferences of {files[activeIndex]} is not allowed. Please continue with explorer.", "Explorer", "Dismiss"))
                        {
                            Utility.ShowInExplorer(files[activeIndex]);
                        }
                        else if (EditorUtility.DisplayDialog("Warning", $"You are deleting the preferences of {files[activeIndex]}. Do you want to proceed?", "Delete", "Cancel"))
                        {
                            if (activeTarget.Equals(XPrefs.Asset)) XPrefs.IAsset.Uri = "";
                            XFile.DeleteFile(files[activeIndex]);
                            activeIndex = -1;
                        }
                    };
                }
                if (GUILayout.Button(new GUIContent("Clone", EditorGUIUtility.FindTexture("TreeEditor.Duplicate"))))
                {
                    if (invalidTarget) return;
                    EditorApplication.delayCall += () =>
                    {
                        if (!XFile.HasDirectory(Root)) XFile.CreateDirectory(Root);
                        var path = EditorUtility.SaveFilePanel("Clone Preferences", Root, Environment.UserName, Extension.TrimStart('.'));
                        if (!string.IsNullOrEmpty(path))
                        {
                            XPrefs.Asset.Save(); // Save previous.

                            var raw = XFile.OpenText(files[activeIndex]);
                            if (XFile.HasFile(path)) XFile.DeleteFile(path);
                            XFile.SaveText(path, raw);
                            activeTarget = new XPrefs.IBase();
                            activeTarget.Read(path);
                            activeIndex = -1;
                            XPrefs.Asset.Read(activeTarget.File);
                        }
                    };
                }
                GUI.color = ocolor;
                GUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }

            /// <summary>
            /// 绘制首选项面板的主体界面。
            /// 按区域分组显示各个首选项面板。
            /// </summary>
            /// <param name="searchContext">搜索上下文字符串</param>
            public override void OnGUI(string searchContext)
            {
                if (sections != null && sections.Count > 0)
                {
                    GUILayout.Space(5);
                    foreach (var section in sections)
                    {
                        var sectionName = section[0].Section;
                        if (string.IsNullOrEmpty(sectionName)) continue;
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        var foldout = true;
                        if (foldouts.ContainsKey(sectionName)) foldout = foldouts[sectionName];
                        foldout = EditorGUILayout.Foldout(foldout, new GUIContent(sectionName, section[0].Tooltip));
                        if (section[0].Foldable) foldouts[sectionName] = foldout;

                        if (foldout)
                        {
                            foreach (var panel in section)
                            {
                                if (panel is ScriptableObject sobj && sobj == null) { Reload(); return; } // 构建后ScriptableObject为空，故重载之
                                try
                                {
                                    panel.Target = activeTarget;
                                    panel.OnVisualize(searchContext);
                                }
                                catch (Exception e) { XLog.Panic(e); }
                            }
                        }
                        EditorGUILayout.EndVertical();
                    }
                }
            }

            /// <summary>
            /// 绘制首选项面板的底部界面。
            /// 提供保存和应用首选项的按钮。
            /// </summary>
            public override void OnFooterBarGUI()
            {
                GUILayout.BeginHorizontal(EditorStyles.helpBox);
                if (GUILayout.Button(new GUIContent("Save", EditorGUIUtility.FindTexture("SaveActive")))) EditorApplication.delayCall += () => Save();
                var ocolor = GUI.color;
                if (!XFile.HasFile(XPrefs.IAsset.Uri)) GUI.color = Color.cyan;
                else if (activeTarget.Dirty) GUI.color = Color.yellow;
                if (GUILayout.Button(new GUIContent("Apply", EditorGUIUtility.FindTexture("SaveFromPlay")))) EditorApplication.delayCall += () => Save(true);
                GUI.color = ocolor;
                GUILayout.EndHorizontal();
                GUILayout.Space(2f);
            }

            /// <summary>
            /// 当首选项面板被关闭时调用。
            /// 对所有面板执行停用操作。
            /// </summary>
            public override void OnDeactivate()
            {
                if (panels != null && panels.Count > 0)
                {
                    foreach (var panel in panels)
                    {
                        if (panel == null) continue;
                        try
                        {
                            panel.Target = activeTarget;
                            panel.OnDeactivate();
                        }
                        catch (Exception e) { XLog.Panic(e); }
                    }
                }
            }

            /// <summary>
            /// 验证所有首选项面板的设置是否有效。
            /// </summary>
            /// <returns>如果所有面板验证通过则返回 true，否则返回 false</returns>
            internal bool Validate()
            {
                foreach (var panel in panels)
                {
                    try
                    {
                        panel.Target = activeTarget;
                        if (!panel.Validate())
                        {
                            return false;
                        }
                    }
                    catch (Exception e)
                    {
                        XLog.Panic(e); return false;
                    }
                }
                return true;
            }

            /// <summary>
            /// 保存首选项设置。
            /// 如果 apply 为 true，则同时应用设置到当前编辑器会话。
            /// </summary>
            /// <param name="apply">是否应用设置到当前编辑器会话</param>
            internal void Save(bool apply = false)
            {
                if (Validate())
                {
                    var func = new Action(() =>
                    {
                        foreach (var panel in panels)
                        {
                            if (panel == null) continue;
                            try
                            {
                                panel.Target = activeTarget;
                                panel.OnSave();
                            }
                            catch (Exception e) { XLog.Panic(e); }
                        }
                        activeTarget.Save();
                        if (apply)
                        {
                            XPrefs.IAsset.Uri = activeTarget.File;
                            XPrefs.Asset.Read(activeTarget.File);
                            foreach (var panel in panels)
                            {
                                if (panel == null) continue;
                                try
                                {
                                    panel.Target = activeTarget;
                                    panel.OnApply();
                                }
                                catch (Exception e) { XLog.Panic(e); }
                            }
                        }
                        XLog.Debug("Save {0}preferences of <a href=\"file:///{1}\">{2}</a> succeed.", apply ? "and apply " : "", Path.GetFullPath(activeTarget.File), Path.GetFileName(activeTarget.File));
                        EditorWindow.focusedWindow.ShowNotification(new GUIContent("Save {0}preferences succeed.".Format(apply ? "and apply " : "")), 1f);
                        if (apply) Event.Notify<Event.Internal.OnPreferencesApply>();
                    });
                    if (string.IsNullOrEmpty(activeTarget.File))
                    {
                        if (!XFile.HasDirectory(Root)) XFile.CreateDirectory(Root);
                        activeTarget.File = EditorUtility.SaveFilePanel("Save Preferences", Root, Environment.UserName, Extension.TrimStart('.'));
                        if (string.IsNullOrEmpty(activeTarget.File))
                        {
                            XLog.Error("Save preferences error caused by nil file path.");
                            return;
                        }
                    }
                    if (activeTarget.File.Contains(".template"))
                    {
                        if (EditorUtility.DisplayDialog("Warning", $"You are saving the template preferences. Do you want to proceed?", "Save", "Cancel"))
                        {
                            func.Invoke();
                        }
                    }
                    else
                    {
                        func.Invoke();
                    }
                }
            }
            #endregion

            #region 事件监听
            class TestListener : ICallbacks
            {
                public void RunStarted(ITestAdaptor testsToRun) { }

                public void RunFinished(ITestResultAdaptor result)
                {
                    // 重新读取本地文件，丢弃测试过程中的修改项
                    XPrefs.Asset.Read(XPrefs.IAsset.Uri);
                }

                public void TestStarted(ITestAdaptor test) { }

                public void TestFinished(ITestResultAdaptor result) { }
            }

            int Event.Callback.Priority => 0;

            bool Event.Callback.Singleton => true;

            void Event.Internal.OnEditorLoad.Process(params object[] args)
            {
                var testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
                testRunnerApi.RegisterCallbacks(new TestListener());
            }

            void Event.Internal.OnPreprocessBuild.Process(params object[] args)
            {
                static void doEval(XPrefs.IBase prefs)
                {
                    var visited = new HashSet<string>();  // 防止循环引用
                    void evalNode(JSONNode node, string path = "")
                    {
                        if (node == null) return;
                        switch (node.Tag)
                        {
                            case JSONNodeType.String:
                                if (!string.IsNullOrEmpty(node.Value) && node.Value.Contains("${"))
                                {
                                    if (!visited.Add(path))
                                    {
                                        XLog.Warn($"XEditor.Prefs.OnPreprocessBuild: detected recursive reference in {path}");
                                        return;
                                    }
                                    var value = node.Value.Eval(XEnv.Vars, prefs);
                                    node.Value = value;
                                    visited.Remove(path);
                                }
                                break;
                            case JSONNodeType.Object:
                                foreach (var kvp in node.AsObject)
                                {
                                    if (kvp.Key.Contains("@Const")) continue; // 常量不处理
                                    var childPath = string.IsNullOrEmpty(path) ? kvp.Key : $"{path}.{kvp.Key}";
                                    evalNode(kvp.Value, childPath);
                                }
                                break;
                            case JSONNodeType.Array:
                                for (int i = 0; i < node.Count; i++)
                                {
                                    evalNode(node[i], $"{path}[{i}]");
                                }
                                break;
                        }
                    }

                    var editorKeys = new List<string>(); // 移除编辑器配置
                    foreach (var kvp in prefs)
                    {
                        if (kvp.Key.Contains("@Editor")) editorKeys.Add(kvp.Key);
                    }
                    foreach (var key in editorKeys)
                    {
                        prefs.Unset(key);
                        XLog.Debug("XEditor.Prefs.OnPreprocessBuild: remove editor prefs: {0}.", key);
                    }

                    evalNode(prefs); // 递归处理所有节点
                }

                var assetFile = XPrefs.Asset.File;
                if (!XFile.HasFile(assetFile)) // 当前首选项不存在
                {
                    if (!Application.isBatchMode && BuildPipeline.isBuildingPlayer) Open();
                    throw new BuildFailedException("XEditor.Prefs.OnPreprocessBuild: no preferences was found, please apply preferences before build.");
                }

                if (!XPrefs.Asset.Keys.MoveNext()) // 当前首选项为空
                {
                    if (!Application.isBatchMode && BuildPipeline.isBuildingPlayer) Open();
                    throw new BuildFailedException("XEditor.Prefs.OnPreprocessBuild: streaming preferences of <a href=\"file:///{0}\">{1}</a> was empty.".Format(Path.GetFullPath(assetFile), Path.GetFileName(assetFile)));
                }

                var prefs = new XPrefs.IBase();
                if (!prefs.Read(assetFile) || !prefs.Keys.MoveNext()) // 首选项读取异常或为空
                {
                    if (!Application.isBatchMode && BuildPipeline.isBuildingPlayer) Open();
                    throw new BuildFailedException("XEditor.Prefs.OnPreprocessBuild: streaming preferences of <a href=\"file:///{0}\">{1}</a> failed.".Format(Path.GetFullPath(assetFile), Path.GetFileName(assetFile)));
                }

                doEval(prefs); // 执行递归求值
                XFile.SaveText(XPrefs.IAsset.Uri, prefs.Json(false).Encrypt());
                AssetDatabase.Refresh();
                XLog.Debug("XEditor.Prefs.OnPreprocessBuild: streaming preferences of <a href=\"file:///{0}\">{1}</a> succeed.", Path.GetFullPath(assetFile), Path.GetFileName(assetFile));
            }
            #endregion
        }
    }
}
