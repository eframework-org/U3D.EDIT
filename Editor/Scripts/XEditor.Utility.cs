// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using EFramework.Utility;

namespace EFramework.Editor
{
    public partial class XEditor
    {
        /// <summary>
        /// XEditor.Utility 提供了一系列编辑器实用工具函数，包括资源收集、依赖分析、文件操作等功能。
        /// </summary>
        /// <remarks>
        /// <code>
        /// 功能特性
        /// - 资源接口：递归扫描目录并收集满足条件的文件，分析资源文件之间的依赖关系
        /// - 文件操作：提供压缩/解压文件，文件浏览等功能
        /// - 消息通知：在 Console 窗口显示提示 Toast 信息
        /// 
        /// 使用手册
        /// 1. 消息通知
        /// 
        /// 1.1 显示Toast消息
        ///     在 Console 窗口中显示的消息通知。
        ///     
        ///     // 显示消息，显示时间4秒
        ///     XEditor.Utility.ShowToast(&quot;操作成功完成&quot;);
        ///     
        ///     // 自定义显示时间并焦点
        ///     XEditor.Utility.ShowToast(&quot;重要消息&quot;, true, 8.0f);
        /// 
        /// 2. 文件操作
        /// 
        /// 2.1 收集文件
        ///     递归扫描目录并收集满足条件的文件。
        ///     
        ///     List&lt;string&gt; files = new List&lt;string&gt;();
        ///     XEditor.Utility.CollectFiles(&quot;Assets/MyFolder&quot;, files, &quot;.meta&quot;, &quot;.tmp&quot;);
        ///     foreach (var file in files)
        ///     {
        ///         Debug.Log($&quot;找到文件: {file}&quot;);
        ///     }
        /// 
        /// 2.2 压缩文件
        ///     将指定目录压缩为zip文件。
        ///     
        ///     bool success = XEditor.Utility.ZipDirectory(&quot;Assets/MyFolder&quot;, &quot;Output/archive.zip&quot;);
        ///     if (success)
        ///     {
        ///         Debug.Log(&quot;压缩成功&quot;);
        ///     }
        /// 
        /// 3. 资源操作
        /// 
        /// 3.1 收集资源
        ///     收集Unity资源文件。
        ///     
        ///     List&lt;string&gt; assets = new List&lt;string&gt;();
        ///     XEditor.Utility.CollectAssets(&quot;Assets/MyFolder&quot;, assets, &quot;.meta&quot;);
        /// 
        /// 3.2 分析资源依赖
        ///     分析资源文件之间的依赖关系。
        ///     
        ///     List&lt;string&gt; sourceAssets = new List&lt;string&gt; { &quot;Assets/MyPrefab.prefab&quot; };
        ///     Dictionary&lt;string, List&lt;string&gt;&gt; dependencies = XEditor.Utility.CollectDependency(sourceAssets);
        ///     foreach (var pair in dependencies)
        ///     {
        ///         Debug.Log($&quot;资源 {pair.Key} 依赖于:&quot;);
        ///         foreach (var dep in pair.Value)
        ///         {
        ///             Debug.Log($&quot;  - {dep}&quot;);
        ///         }
        ///     }
        /// 
        /// 4. 编辑器扩展
        /// 
        /// 4.1 获取选中资源
        ///     获取当前在Project窗口中选中的资源。
        ///     
        ///     List&lt;string&gt; selectedAssets = XEditor.Utility.GetSelectedAssets();
        /// 
        /// 4.2 在文件浏览器中显示
        ///     在系统文件浏览器中显示指定路径。
        ///     
        ///     XEditor.Utility.ShowInExplorer(&quot;Assets/MyFolder&quot;);
        ///     
        /// </code>
        /// 更多信息请参考模块文档。
        /// </remarks>
        public class Utility : Event.Internal.OnEditorUpdate
        {
            /// <summary>
            /// Toast 提示框类，用于在编辑器中显示临时提示信息。
            /// </summary>
            internal class Toast
            {
                /// <summary>
                /// Toast 的可视化元素，用于显示提示内容。
                /// </summary>
                public VisualElement View;

                /// <summary>
                /// Toast 的显示时间，单位为秒。
                /// </summary>
                public double Duration;
            }

            /// <summary>
            /// 当前活动的 Toast 列表。
            /// </summary>
            internal static readonly List<Toast> activeToasts = new();

            /// <summary>
            /// Unity 控制台窗口的引用。
            /// </summary>
            internal static EditorWindow consoleWindow;

            int Event.Callback.Priority => 0;

            bool Event.Callback.Singleton => true;

            void Event.Internal.OnEditorUpdate.Process(params object[] args)
            {
                Event.Decode<double>(out var deltaTime, args);
                if (consoleWindow != null)
                {
                    for (var i = 0; i < activeToasts.Count;)
                    {
                        var toast = activeToasts[i];
                        toast.Duration -= deltaTime;
                        if (toast.Duration <= 0)
                        {
                            try
                            {
                                // 检查 View 是否还在父元素中
                                if (toast.View != null && toast.View.parent == consoleWindow.rootVisualElement)
                                {
                                    consoleWindow.rootVisualElement.Remove(toast.View);
                                }
                            }
                            catch (Exception) { /* 忽略移除失败的错误 */ }

                            activeToasts.RemoveAt(i);
                            if (activeToasts.Count > 0)
                            {
                                var firstToast = activeToasts[0];
                                if (firstToast.View != null)
                                {
                                    firstToast.View.style.marginLeft = 56; // prevent to block clear button's event
                                }
                            }
                        }
                        else i++;
                    }
                }
            }

            /// <summary>
            /// 在控制台窗口显示提示框。
            /// </summary>
            /// <param name="content">提示内容。</param>
            /// <param name="focus">是否聚焦到控制台窗口，默认为 false。</param>
            /// <param name="duration">提示框显示时间（秒），默认为 4.0 秒。</param>
            public static void ShowToast(string content, bool focus = false, float duration = 4.0f)
            {
                if (Application.isBatchMode) return;
                if (string.IsNullOrEmpty(content) == false)
                {
                    if (consoleWindow == null)
                    {
                        var editorAssembly = GetEditorAssembly();
                        var clazzConsoleWindow = editorAssembly.GetType("UnityEditor.ConsoleWindow");

                        consoleWindow = EditorWindow.GetWindow(clazzConsoleWindow);
                        if (focus) consoleWindow.Focus();
                    }

                    if (duration <= 0) duration = 4.0f;

                    var label = new Label(content.Omit(120));
                    label.style.unityTextAlign = TextAnchor.MiddleCenter;
                    label.style.fontSize = 16;

                    var layout = new VisualElement();
                    layout.style.flexDirection = FlexDirection.Column;
                    layout.style.justifyContent = Justify.Center;
                    layout.style.alignItems = Align.Center;
                    layout.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f, 0.7f);
                    layout.Add(label);
                    if (activeToasts.Count == 0) layout.style.marginLeft = 56; // prevent to block clear button's event

                    var toast = new Toast { View = layout, Duration = duration };
                    layout.RegisterCallback<MouseDownEvent>((evt) => toast.Duration = 0);
                    activeToasts.Add(toast);
                    consoleWindow.rootVisualElement.Add(layout);
                }
            }

            /// <summary>
            /// 递归收集指定目录下的所有文件。
            /// </summary>
            /// <param name="directory">要收集的目录路径。</param>
            /// <param name="outfiles">输出的文件列表。</param>
            /// <param name="exclude">要排除的文件扩展名数组。</param>
            public static void CollectFiles(string directory, List<string> outfiles, params string[] exclude)
            {
                if (Directory.Exists(directory))
                {
                    string[] files = Directory.GetFiles(directory);
                    for (int i = 0; i < files.Length; i++)
                    {
                        string file = XFile.NormalizePath(files[i]);
                        bool avilable = true;
                        for (int j = 0; j < exclude.Length; j++)
                        {
                            string ext = exclude[j];
                            if (file.EndsWith(ext))
                            {
                                avilable = false;
                                break;
                            }
                        }
                        if (avilable)
                        {
                            outfiles.Add(file);
                        }
                    }
                    string[] dirs = Directory.GetDirectories(directory);
                    for (int i = 0; i < dirs.Length; i++)
                    {
                        CollectFiles(dirs[i], outfiles, exclude);
                    }
                }
                else if (File.Exists(directory))
                {
                    bool avilable = true;
                    for (int j = 0; j < exclude.Length; j++)
                    {
                        string ext = exclude[j];
                        if (directory.EndsWith(ext))
                        {
                            avilable = false;
                            break;
                        }
                    }
                    if (avilable)
                    {
                        outfiles.Add(directory);
                    }
                }
            }

            /// <summary>
            /// 递归收集指定目录下的所有 Unity 资源文件。
            /// </summary>
            /// <param name="directory">要收集的目录路径。</param>
            /// <param name="outfiles">输出的资源文件列表。</param>
            /// <param name="exclude">要排除的文件扩展名数组。</param>
            public static void CollectAssets(string directory, List<string> outfiles, params string[] exclude)
            {
                if (Directory.Exists(directory))
                {
                    var files = Directory.GetFiles(directory);
                    for (int i = 0; i < files.Length; i++)
                    {
                        var file = XFile.NormalizePath(files[i]);
                        var avilable = true;
                        for (int j = 0; j < exclude.Length; j++)
                        {
                            string ext = exclude[j];
                            if (file.EndsWith(ext))
                            {
                                avilable = false;
                                break;
                            }
                        }
                        if (avilable)
                        {
                            avilable = !file.EndsWith(".meta");
                        }
                        if (avilable)
                        {
                            if (file.StartsWith(Application.dataPath))
                            {
                                file = file[(Application.dataPath.Length + 1)..];
                                file = "Assets/" + file;
                            }
                            outfiles.Add(file);
                        }
                    }
                    var dirs = Directory.GetDirectories(directory);
                    for (int i = 0; i < dirs.Length; i++)
                    {
                        CollectAssets(dirs[i], outfiles, exclude);
                    }
                }
                else if (File.Exists(directory))
                {
                    var avilable = true;
                    for (int j = 0; j < exclude.Length; j++)
                    {
                        var ext = exclude[j];
                        if (directory.EndsWith(ext))
                        {
                            avilable = false;
                            break;
                        }
                    }
                    if (avilable)
                    {
                        if (directory.StartsWith(Application.dataPath))
                        {
                            directory = directory[(Application.dataPath.Length + 1)..];
                            directory = "Assets/" + directory;
                        }
                        outfiles.Add(directory);
                    }
                }
            }

            /// <summary>
            /// 收集指定资源文件的所有依赖项。
            /// </summary>
            /// <param name="sourceAssets">源资源文件列表。</param>
            /// <returns>返回依赖关系字典，key 为源资源路径，value 为依赖项列表。</returns>
            public static Dictionary<string, List<string>> CollectDependency(List<string> sourceAssets)
            {
                var dependencies = new Dictionary<string, List<string>>();
                for (int i = 0; i < sourceAssets.Count; i++)
                {
                    var sourceAsset = sourceAssets[i];
                    var deps = AssetDatabase.GetDependencies(sourceAsset);
                    var depList = new List<string>();
                    for (int j = 0; j < deps.Length; j++)
                    {
                        var dep = deps[j];
                        if (dep.EndsWith(".cs")) continue;
                        depList.Add(dep);
                    }
                    dependencies.Add(sourceAsset, depList);
                }
                return dependencies;
            }

            /// <summary>
            /// 获取当前在 Unity 编辑器中选中的资源文件。
            /// </summary>
            /// <param name="filter">资源过滤器，用于筛选特定资源。</param>
            /// <returns>返回选中的资源路径列表。</returns>
            public static List<string> GetSelectedAssets(Func<string, bool> filter = null)
            {
                var assets = new List<string>();
                if (Selection.assetGUIDs.Length == 0)
                {
                    CollectAssets(Application.dataPath, assets, ".cs", ".js", ".meta", ".DS_Store");
                    for (int i = 0; i < assets.Count;)
                    {
                        var path = assets[i];
                        if (filter == null || filter(path)) i++;
                        else assets.RemoveAt(i);
                    }
                }
                else
                {
                    for (int i = 0; i < Selection.assetGUIDs.Length; i++)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[i]);
                        if (filter == null || filter(path)) assets.Add(path);
                        else if (XFile.HasDirectory(path))
                        {
                            var temps = new List<string>();
                            CollectAssets(Path.GetFullPath(path), temps, ".cs", ".js", ".meta", ".DS_Store");
                            for (int j = 0; j < temps.Count; j++)
                            {
                                string temp = temps[j];
                                if (filter == null || filter(temp)) assets.Add(temp);
                            }
                        }
                    }
                }
                return assets;
            }

            /// <summary>
            /// 将指定目录压缩为 ZIP 文件。
            /// </summary>
            /// <param name="dir">要压缩的目录路径。</param>
            /// <param name="zip">输出的 ZIP 文件路径。</param>
            /// <param name="split">分卷大小（字节），-1 表示不分卷。</param>
            /// <returns>返回压缩是否成功。</returns>
            public static bool ZipDirectory(string dir, string zip, int split = -1)
            {
                try
                {
                    if (!XFile.HasDirectory(dir)) throw new Exception($"Directory of {dir} was not found.");
                    if (dir.EndsWith("/") || dir.EndsWith(@"\")) dir = dir[..^1];
                    if (Application.platform == RuntimePlatform.WindowsEditor)
                    {
                        var rar = Cmd.Find("WinRAR.exe", "C:/Program Files/WinRAR");
                        if (string.IsNullOrEmpty(rar))
                        {
                            XLog.Warn("WinRAR.exe was not found, configure it to boost zip procedure.");
                            return XFile.Zip(dir, zip);
                        }
                        zip = XFile.NormalizePath(Path.GetFullPath(zip)); // 使用全路径，否则会压缩失败
                        Cmd.Run(bin: rar, cwd: dir, args: new string[] { split != -1 ? $"a -r -v{split} \"{zip}\"" : $"a -r \"{zip}\"" }).Wait();
                        return true;
                    }
                    else return XFile.Zip(dir, zip);
                }
                catch (Exception e) { XLog.Panic(e); return false; }
            }

            /// <summary>
            /// 获取 Unity 编辑器程序集。
            /// </summary>
            /// <returns>返回 UnityEditor 程序集。</returns>
            public static Assembly GetEditorAssembly() { return Assembly.GetAssembly(typeof(EditorWindow)); }

            /// <summary>
            /// 通过反射获取 Unity 编辑器中的类型。
            /// </summary>
            /// <param name="name">类型的完整名称。</param>
            /// <returns>返回对应的类型。</returns>
            public static Type GetEditorClass(string name) { return GetEditorAssembly().GetType(name); }

            /// <summary>
            /// 在系统文件浏览器中显示指定的文件或文件夹。
            /// </summary>
            /// <param name="path">要显示的文件或文件夹路径。</param>
            public static void ShowInExplorer(string path)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    path = Path.GetFullPath(path);
                    if (File.Exists(path))
                    {
                        if (Application.platform == RuntimePlatform.WindowsEditor)
                        {
                            try
                            {
                                var proc = new ProcessStartInfo("Explorer.exe");
                                proc.Arguments = "/e,/select," + path;
                                Process.Start(proc);
                                return;
                            }
                            catch (Exception e) { XLog.Panic(e); }
                        }
                        EditorUtility.OpenWithDefaultApp(Path.GetDirectoryName(path));
                        return;
                    }
                    EditorUtility.OpenWithDefaultApp(path);
                }
            }

            /// <summary>
            /// 查找指定程序集所属的 Unity 包信息。
            /// </summary>
            /// <param name="assembly">要查找的程序集，默认为调用者所在的程序集。</param>
            /// <returns>返回包信息。</returns>
            public static UnityEditor.PackageManager.PackageInfo FindPackage(Assembly assembly = null)
            {
                if (assembly == null) assembly = Assembly.GetCallingAssembly();
                return UnityEditor.PackageManager.PackageInfo.FindForAssembly(assembly);
            }
        }
    }
}
