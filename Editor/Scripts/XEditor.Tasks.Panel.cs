// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using EFramework.Utility;

namespace EFramework.Editor
{
    /// <summary>
    /// 任务系统的可视化管理面板，对任务进行分组管理，提供可视化的参数配置等功能。
    /// </summary>
    [Serializable]
    internal class TasksPanel : EditorWindow
    {
        /// <summary>
        /// 存储任务参数的字典，key 为任务属性，value 为参数字典。
        /// </summary>
        /// <remarks>
        /// 用于保存每个任务的参数配置，支持运行时修改和持久化。
        /// </remarks>
        internal readonly Dictionary<XEditor.Tasks.WorkerAttribute, Dictionary<XEditor.Tasks.Param, string>> taskArguments = new();

        /// <summary>
        /// 任务组列表，每个组包含多个相关的任务。
        /// </summary>
        /// <remarks>
        /// 按照任务的 Group 属性进行分组，用于界面展示和批量操作。
        /// </remarks>
        internal readonly List<List<XEditor.Tasks.WorkerAttribute>> taskGroups = new();

        /// <summary>
        /// 存储任务组展开状态的字典。
        /// </summary>
        /// <remarks>
        /// key 为组名，value 为展开状态。用于保持界面状态。
        /// </remarks>
        internal readonly Dictionary<string, bool> groupFoldouts = new();

        /// <summary>
        /// 存储任务组选中状态的字典。
        /// </summary>
        /// <remarks>
        /// key 为组名，value 为选中状态。用于批量操作。
        /// </remarks>
        internal readonly Dictionary<string, bool> groupSelects = new();

        /// <summary>
        /// 存储任务展开状态的字典。
        /// </summary>
        /// <remarks>
        /// key 为任务元数据，value 为展开状态。用于保持界面状态。
        /// </remarks>
        internal readonly Dictionary<XEditor.Tasks.WorkerAttribute, bool> taskFoldouts = new();

        /// <summary>
        /// 存储任务选中状态的字典。
        /// </summary>
        /// <remarks>
        /// key 为任务元数据，value 为选中状态。用于执行控制。
        /// </remarks>
        internal readonly Dictionary<XEditor.Tasks.WorkerAttribute, bool> taskSelects = new();

        /// <summary>
        /// 任务执行顺序列表。
        /// </summary>
        /// <remarks>
        /// 存储已选中任务的执行顺序，考虑任务间的依赖关系。
        /// </remarks>
        internal readonly List<XEditor.Tasks.WorkerAttribute> taskOrders = new();

        internal readonly Dictionary<string, int> taskReports = new();

        /// <summary>
        /// 是否展开所有任务组和任务。
        /// </summary>
        internal bool foldoutAll = true;

        /// <summary>
        /// 是否选中所有任务组和任务。
        /// </summary>
        internal bool selectAll = false;

        /// <summary>
        /// 滚动视图位置。
        /// </summary>
        internal Vector2 scroll = Vector2.zero;

        internal const string FAIL = "TestFailed";
        internal const string SUCCESS = "TestPassed";
        internal const string UNKNOW = "TestNormal";

        /// <summary>
        /// 窗口启用时的回调，初始化面板实例并重置状态。
        /// </summary>
        internal void OnEnable()
        {
            taskArguments.Clear();
            taskGroups.Clear();
            groupFoldouts.Clear();
            groupSelects.Clear();
            taskFoldouts.Clear();
            taskSelects.Clear();
            taskOrders.Clear();
            foldoutAll = true;
            selectAll = false;

            foreach (var kvp in XEditor.Tasks.Workers)
            {
                var meta = kvp.Key;
                List<XEditor.Tasks.WorkerAttribute> group = null;
                for (var j = 0; j < taskGroups.Count; j++)
                {
                    var temp = taskGroups[j];
                    if (temp != null && temp.Count > 0 && temp[0].Group == meta.Group)
                    {
                        group = temp;
                        break;
                    }
                }
                if (group == null)
                {
                    group = new List<XEditor.Tasks.WorkerAttribute>();
                    taskGroups.Add(group);
                }
                group.Add(meta);
            }

            // 对每个分组内的任务按优先级排序
            foreach (var group in taskGroups)
            {
                group.Sort((a, b) => XEditor.Tasks.Workers[a].Priority.CompareTo(XEditor.Tasks.Workers[b].Priority));
            }

            // 对分组列表按首个任务的优先级排序
            taskGroups.Sort((a, b) => XEditor.Tasks.Workers[a[0]].Priority.CompareTo(XEditor.Tasks.Workers[b[0]].Priority));

            // 按照taskGroups的优先级顺序执行OnEnable回调
            foreach (var group in taskGroups)
            {
                foreach (var meta in group)
                {
                    var worker = XEditor.Tasks.Workers[meta];
                    if (worker != null && typeof(XEditor.Tasks.Panel.IOnEnable).IsAssignableFrom(worker.GetType()))
                    {
                        try { (worker as XEditor.Tasks.Panel.IOnEnable).OnEnable(); }
                        catch (Exception e) { XLog.Panic(e); }
                    }
                }
            }
        }

        /// <summary>
        /// 窗口禁用时的回调，通知所有实现了 IOnDisable 接口的任务。
        /// </summary>
        internal void OnDisable()
        {
            // 按照taskGroups的优先级顺序执行OnDisable回调
            foreach (var group in taskGroups)
            {
                foreach (var meta in group)
                {
                    if (XEditor.Tasks.Workers.TryGetValue(meta, out var worker) &&
                        worker != null && typeof(XEditor.Tasks.Panel.IOnDisable).IsAssignableFrom(worker.GetType()))
                    {
                        try { (worker as XEditor.Tasks.Panel.IOnDisable).OnDisable(); }
                        catch (Exception e) { XLog.Panic(e); }
                    }
                }
            }

            if (XPrefs.Asset.Dirty && XFile.HasFile(XPrefs.Asset.File))
            {
                XPrefs.Asset.Save();
            }
        }

        /// <summary>
        /// 失去焦点时保存配置文件。
        /// </summary>
        internal void OnLostFocus()
        {
            if (XPrefs.Asset.Dirty && XFile.HasFile(XPrefs.Asset.File))
            {
                XPrefs.Asset.Save();
            }
        }

        /// <summary>
        /// 窗口销毁时的回调，通知所有实现了 IOnDestroy 接口的任务，并清理面板实例。
        /// </summary>
        internal void OnDestroy()
        {
            // 按照taskGroups的优先级顺序执行OnDestroy回调
            foreach (var group in taskGroups)
            {
                foreach (var meta in group)
                {
                    if (XEditor.Tasks.Workers.TryGetValue(meta, out var worker) &&
                        worker != null && typeof(XEditor.Tasks.Panel.IOnDisable).IsAssignableFrom(worker.GetType()))
                    {
                        try { (worker as XEditor.Tasks.Panel.IOnDestroy).OnDestroy(); }
                        catch (Exception e) { XLog.Panic(e); }
                    }
                }
            }
        }

        /// <summary>
        /// 绘制窗口 GUI，包括任务组、任务列表、参数设置等界面元素。
        /// </summary>
        internal void OnGUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.MaxWidth(10));
            GUILayout.Space(5);
            var lastSelect = selectAll;
            selectAll = EditorGUILayout.Toggle(selectAll);
            if (lastSelect != selectAll)
            {
                if (selectAll == false)
                {
                    groupSelects.Clear();
                    taskSelects.Clear();
                    taskOrders.Clear();
                }
                else
                {
                    foreach (var group in taskGroups)
                    {
                        groupSelects[group[0].Group] = true;
                    }
                    foreach (var meta in XEditor.Tasks.Metas)
                    {
                        taskSelects[meta] = true;
                    }
                }
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Space(6);
            var foldoutButton = foldoutAll ? EditorGUIUtility.IconContent("IN foldout on") : EditorGUIUtility.IconContent("IN foldout");
            foldoutButton.tooltip = "(Un)foldout task(s).";
            if (GUILayout.Button(foldoutButton, EditorStyles.iconButton, GUILayout.Width(8)))
            {
                foldoutAll = !foldoutAll;
                foreach (var group in taskGroups)
                {
                    groupFoldouts[group[0].Group] = foldoutAll;
                }
                foreach (var meta in XEditor.Tasks.Metas)
                {
                    taskFoldouts[meta] = foldoutAll;
                }
            }
            GUILayout.EndVertical();

            var prefsName = string.IsNullOrEmpty(XPrefs.Asset.File) ? "Unknown" : Path.GetFileName(XPrefs.Asset.File);
            var prefsContent = XPrefs.Asset.Json();
            var prefsInvalid = !XFile.HasFile(XPrefs.Asset.File) || !XPrefs.Asset.Keys.MoveNext();
            var ocolor = GUI.color;
            if (prefsInvalid) GUI.color = Color.gray;
            if (EditorGUILayout.LinkButton(new GUIContent($"[Preferences: {prefsName}]", prefsContent)))
            {
                XEditor.Prefs.Open();
            }
            GUI.color = ocolor;

            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical();
            GUILayout.Space(7);
            if (GUILayout.Button(new GUIContent("", EditorGUIUtility.FindTexture("d_PlayButton@2x"), "Execute task(s)."), EditorStyles.iconButton))
            {
                XLoom.RunInNext(() =>
                {
                    var workers = new List<XEditor.Tasks.IWorker>();
                    foreach (var meta in taskOrders)
                    {
                        workers.Add(XEditor.Tasks.Workers[meta]);
                    }
                    var uworkers = new List<XEditor.Tasks.IWorker>();
                    foreach (var kvp in taskSelects)
                    {
                        if (kvp.Value)
                        {
                            var worker = XEditor.Tasks.Workers[kvp.Key];
                            if (workers.Contains(worker) == false)
                            {
                                uworkers.Add(worker);
                            }
                        }
                    }
                    uworkers.Sort((e1, e2) => e1.Priority.CompareTo(e2.Priority));
                    workers.AddRange(uworkers);
                    Run(workers);
                });
            }
            EditorGUILayout.EndVertical();
            GUILayout.Space(4);

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4);
            EditorGUILayout.EndVertical();

            scroll = GUILayout.BeginScrollView(scroll);
            if (taskGroups != null && taskGroups.Count > 0)
            {
                foreach (var group in taskGroups)
                {
                    var groupName = group[0].Group;
                    if (string.IsNullOrEmpty(groupName)) continue;
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    EditorGUILayout.BeginHorizontal();
                    groupSelects.TryGetValue(groupName, out lastSelect);
                    var currentSelect = EditorGUILayout.Toggle(lastSelect, GUILayout.MaxWidth(15));
                    if (lastSelect != currentSelect)
                    {
                        groupSelects[groupName] = currentSelect;
                        foreach (var meta in group)
                        {
                            taskSelects[meta] = currentSelect;
                            if (currentSelect == false) taskOrders.Remove(meta);
                        }
                    }

                    var gfoldout = true;
                    var gfoldoutRect = EditorGUILayout.GetControlRect();
                    if (groupFoldouts.ContainsKey(groupName)) gfoldout = groupFoldouts[groupName];
                    gfoldout = EditorGUI.Foldout(gfoldoutRect, gfoldout, new GUIContent(groupName, GetGroupStatusIcon(group), group[0].Tooltip));
                    groupFoldouts[groupName] = gfoldout;

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.BeginVertical();
                    GUILayout.Space(3);
                    if (GUILayout.Button(new GUIContent("", EditorGUIUtility.FindTexture("d_PlayButton@2x"), "Execute task(s)."), EditorStyles.iconButton) ||
                    (gfoldoutRect.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown && Event.current.button == 0 && Event.current.clickCount == 2))
                    {
                        Event.current.Use();
                        XLoom.RunInNext(() =>
                        {
                            var workers = new List<XEditor.Tasks.IWorker>();
                            foreach (var meta in taskOrders)
                            {
                                if (group.Contains(meta)) workers.Add(XEditor.Tasks.Workers[meta]);
                            }
                            var uworkers = new List<XEditor.Tasks.IWorker>();
                            foreach (var kvp in taskSelects)
                            {
                                if (kvp.Value && group.Contains(kvp.Key))
                                {
                                    var worker = XEditor.Tasks.Workers[kvp.Key];
                                    if (workers.Contains(worker) == false)
                                    {
                                        uworkers.Add(worker);
                                    }
                                }
                            }
                            if (uworkers.Count == 0) // 未选中，则默认执行该分组的所有任务
                            {
                                foreach (var meta in group)
                                {
                                    uworkers.Add(XEditor.Tasks.Workers[meta]);
                                }
                            }
                            uworkers.Sort((e1, e2) => e1.Priority.CompareTo(e2.Priority));
                            workers.AddRange(uworkers);
                            Run(workers);
                        });
                    }
                    EditorGUILayout.EndVertical();
                    GUILayout.Space(4);

                    EditorGUILayout.EndHorizontal();

                    if (gfoldout)
                    {
                        foreach (var meta in group)
                        {
                            try
                            {
                                EditorGUILayout.BeginHorizontal();

                                EditorGUILayout.BeginVertical(GUILayout.MaxWidth(15));
                                GUILayout.Space(4);
                                taskSelects.TryGetValue(meta, out lastSelect);
                                currentSelect = EditorGUILayout.Toggle(lastSelect);
                                if (lastSelect != currentSelect)
                                {
                                    taskSelects[meta] = currentSelect;
                                    if (currentSelect) taskOrders.Add(meta);
                                    else taskOrders.Remove(meta);
                                }
                                EditorGUILayout.EndVertical();

                                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                                GUILayout.BeginHorizontal();
                                var idx = taskOrders.IndexOf(meta);
                                var sidx = idx >= 0 ? $" #{idx + 1}" : "";
                                var tfoldout = false;
                                var tfoldoutRect = EditorGUILayout.GetControlRect();
                                var icon = GetTaskStatusIcon(meta.Name);
                                if (meta.Params != null && meta.Params.Count > 0 || meta.Worker != null && typeof(XEditor.Tasks.Panel.IOnGUI).IsAssignableFrom(meta.Worker))
                                {
                                    if (taskFoldouts.ContainsKey(meta)) tfoldout = taskFoldouts[meta];
                                    tfoldout = EditorGUI.Foldout(tfoldoutRect, tfoldout, new GUIContent(meta.Name + sidx, icon, meta.Tooltip));
                                    taskFoldouts[meta] = tfoldout;
                                }
                                else EditorGUI.Foldout(tfoldoutRect, tfoldout, new GUIContent(meta.Name + sidx, icon, meta.Tooltip));
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button(new GUIContent("", EditorGUIUtility.FindTexture("d_PlayButton@2x"), "Execute task."), EditorStyles.iconButton) ||
                                (tfoldoutRect.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown && Event.current.button == 0 && Event.current.clickCount == 2))
                                {
                                    Event.current.Use();
                                    XLoom.RunInNext(() =>
                                    {
                                        var worker = XEditor.Tasks.Workers[meta];
                                        Run(new List<XEditor.Tasks.IWorker> { worker });
                                    });
                                }
                                GUILayout.EndHorizontal();

                                if (tfoldout)
                                {
                                    taskArguments.TryGetValue(meta, out var marguments);

                                    if (meta.Params != null && meta.Params.Count > 0)
                                    {
                                        if (marguments == null)
                                        {
                                            marguments = new Dictionary<XEditor.Tasks.Param, string>();
                                            taskArguments[meta] = marguments;
                                        }
                                        foreach (var param in meta.Params)
                                        {
                                            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                                            EditorGUILayout.LabelField(new GUIContent(param.Name.Omit(10), !string.IsNullOrEmpty(param.Tooltip) ? param.Tooltip : param.Name), GUILayout.Width(70));
                                            var opvalue = marguments.ContainsKey(param) ? marguments[param] :
                                                param.Persist ? XPrefs.GetString(param.ID, param.Default) : param.Default;
                                            var npvalue = EditorGUILayout.TextField(opvalue);
                                            if (opvalue != npvalue)
                                            {
                                                if (param.Persist) XPrefs.Asset.Set(param.ID, npvalue);
                                                marguments[param] = npvalue;
                                            }
                                            else if (!marguments.ContainsKey(param)) marguments[param] = npvalue;
                                            EditorGUILayout.EndHorizontal();
                                        }
                                    }

                                    if (meta.Worker != null && typeof(XEditor.Tasks.Panel.IOnGUI).IsAssignableFrom(meta.Worker))
                                    {
                                        try
                                        {
                                            var worker = XEditor.Tasks.Workers[meta];
                                            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                                            try { (worker as XEditor.Tasks.Panel.IOnGUI).OnGUI(); }
                                            catch (Exception e) { XLog.Panic(e); }
                                            EditorGUILayout.EndVertical();
                                        }
                                        catch (Exception e) { XLog.Panic(e); }
                                    }
                                }
                                EditorGUILayout.EndVertical();

                                EditorGUILayout.EndHorizontal();
                            }
                            catch (Exception e) { XLog.Panic(e); }

                            GUILayout.Space(3);
                        }
                    }
                    EditorGUILayout.EndVertical();

                    GUILayout.Space(2);
                }
            }
            GUILayout.EndScrollView();
        }

        /// <summary>
        /// 执行任务列表
        /// </summary>
        /// <param name="workers">要执行的任务列表</param>
        internal void Run(List<XEditor.Tasks.IWorker> workers)
        {
            if (workers == null || workers.Count == 0) return;

            // 检查是否存在同步任务
            var hasSync = false;
            foreach (var worker in workers)
            {
                var meta = XEditor.Tasks.Workers.First(kvp => kvp.Value == worker).Key;
                if (!meta.Runasync)
                {
                    hasSync = true;
                    break;
                }
            }

            // 执行任务
            foreach (var worker in workers)
            {
                var meta = XEditor.Tasks.Workers.First(kvp => kvp.Value == worker).Key;
                var arguments = new Dictionary<string, string>();
                taskArguments.TryGetValue(meta, out var marguments);
                foreach (var param in meta.Params)
                {
                    if (marguments != null && marguments.TryGetValue(param, out var pvalue)) arguments[param.Name] = pvalue;
                    else if (param.Persist) arguments[param.Name] = XPrefs.GetString(param.ID, param.Default);
                    else arguments[param.Name] = param.Default;
                }
                worker.Runasync = meta.Runasync;
                // 因任务间的依赖关系未知，多任务并发时，且有主线程任务的情况下，使用串行模式执行
                if (workers.Count > 1 && hasSync && worker.Runasync) worker.Runasync = false;
                var report = XEditor.Tasks.Execute(worker: worker, arguments: arguments);
                taskReports[meta.Name] = (int)report.Result;
            }
            SaveTaskReportsCache();
        }

        /// <summary>
        /// 获取任务组状态图标
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        internal Texture GetGroupStatusIcon(List<XEditor.Tasks.WorkerAttribute> group)
        {
            var hasUnknown = false;
            var hasFail = false;
            foreach (var meta in group)
            {
                if (!taskReports.TryGetValue(meta.Name, out var result) || result == 0) // 0为未知
                {
                    hasUnknown = true;
                    break;
                }
                if (result == 2) // 2为失败
                {
                    hasFail = true;
                }
            }
            if (hasUnknown) return XEditor.Icons.GetIcon(UNKNOW)?.image;
            if (hasFail) return XEditor.Icons.GetIcon(FAIL)?.image;
            return XEditor.Icons.GetIcon(SUCCESS)?.image;
        }

        /// <summary>
        /// 获取任务状态图标
        /// </summary>
        /// <param name="metaName"></param>
        /// <returns></returns>
        internal Texture GetTaskStatusIcon(string metaName)
        {
            // 没有数据时，加载缓存
            if (!taskReports.ContainsKey(metaName)) LoadTaskReportsCache();
            taskReports.TryGetValue(metaName, out var result);
            Texture icon = XEditor.Icons.GetIcon(UNKNOW)?.image;
            if (result == (int)XEditor.Tasks.Result.Succeeded)
                icon = XEditor.Icons.GetIcon(SUCCESS)?.image;
            else if (result == (int)XEditor.Tasks.Result.Failed)
                icon = XEditor.Icons.GetIcon(FAIL)?.image;
            return icon;
        }

        /// <summary>
        /// 保存任务状态缓存
        /// </summary>
        internal void SaveTaskReportsCache()
        {
            var cache = new XEditor.Tasks.TaskStatusCache(taskReports);
            var json = JsonUtility.ToJson(cache);
            XFile.SaveText(XFile.PathJoin(XEnv.ProjectPath, "Library/TaskReportsCache.json"), json);
        }

        /// <summary>
        /// 加载任务状态缓存
        /// </summary>
        internal void LoadTaskReportsCache()
        {
            var path = "Library/TaskReportsCache.json";
            if (!File.Exists(path)) return;
            var json = File.ReadAllText(path);
            var cache = JsonUtility.FromJson<XEditor.Tasks.TaskStatusCache>(json);
            if (cache != null)
            {
                var dict = cache.ToDictionary();
                foreach (var kv in dict)
                {
                    taskReports[kv.Key] = kv.Value;
                }
            }
        }
    }

    public partial class XEditor
    {
        public partial class Tasks
        {
            /// <summary>
            /// 任务面板管理器，提供面板的创建、显示和生命周期管理。
            /// </summary>
            public class Panel : Event.Internal.OnEditorLoad
            {
                /// <summary>
                /// 任务面板打开时的回调接口。
                /// </summary>
                /// <remarks>
                /// 用于初始化任务状态和资源。
                /// </remarks>
                public interface IOnEnable { void OnEnable(); }

                /// <summary>
                /// 任务面板绘制时的回调接口。
                /// </summary>
                /// <remarks>
                /// 用于绘制自定义界面元素。
                /// </remarks>
                public interface IOnGUI { void OnGUI(); }

                /// <summary>
                /// 任务面板关闭时的回调接口。
                /// </summary>
                /// <remarks>
                /// 用于保存状态和清理资源。
                /// </remarks>
                public interface IOnDisable { void OnDisable(); }

                /// <summary>
                /// 任务面板销毁时的回调接口。
                /// </summary>
                /// <remarks>
                /// 用于执行最终的清理操作。
                /// </remarks>
                public interface IOnDestroy { void OnDestroy(); }

                /// <summary>
                /// 菜单路径，定义了在 Unity 主菜单中的位置。
                /// </summary>
                internal const string MenuPath = "Tools/EFramework/Task Runner";

                /// <summary>
                /// 任务窗口实例。
                /// </summary>
                internal static TasksPanel Instance;

                /// <summary>
                /// 事件处理优先级。
                /// </summary>
                int Event.Callback.Priority => 0;

                /// <summary>
                /// 是否为单例事件处理器。
                /// </summary>
                bool Event.Callback.Singleton => false;

                /// <summary>
                /// 初始化任务系统，在 Unity 编辑器加载时调用。
                /// </summary>
                /// <remarks>
                /// 查找并初始化已存在的任务面板实例。
                /// </remarks>
                void Event.Internal.OnEditorLoad.Process(params object[] _)
                {
                    var windows = Resources.FindObjectsOfTypeAll<TasksPanel>();
                    if (windows != null && windows.Length > 0)
                    {
                        Instance = windows[0];
                        Instance.OnEnable();
                    }
                }

                /// <summary>
                /// 打开任务窗口。
                /// </summary>
                /// <remarks>
                /// 创建或获取任务面板实例，并设置窗口标题和图标。
                /// </remarks>
                [MenuItem(MenuPath)]
                public static void Open()
                {
                    var name = Path.GetFileName(MenuPath).Split("#")[0].Trim();
                    Instance = EditorWindow.GetWindow<TasksPanel>(name);
                    Instance.titleContent = new GUIContent(name, EditorGUIUtility.IconContent("d_PlayButton@2x").image);
                }

                /// <summary>
                /// 重置任务窗口状态。
                /// </summary>
                /// <remarks>
                /// 重新初始化任务面板的状态，通常在任务配置发生变化时调用。
                /// </remarks>
                public static void Reset() { if (Instance) Instance.OnEnable(); }
            }
        }
    }
}