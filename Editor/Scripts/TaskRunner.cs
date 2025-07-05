// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using EFramework.Utility;

namespace EFramework.Editor
{
    /// <summary>
    /// TaskRunner 是任务系统的可视化管理面板，对任务进行分组管理，提供可视化的参数配置等功能。
    /// </summary>
    [Serializable]
    internal class TaskRunner : EditorWindow
    {
        /// <summary>
        /// Stateful 用来描述分组的折叠/选中状态。
        /// </summary>
        [Serializable]
        internal class Stateful
        {
            /// <summary>
            /// Key 是状态的键名。
            /// </summary>
            [SerializeField]
            internal string Key;

            /// <summary>
            /// Status 表示折叠/选中的状态。
            /// </summary>
            [SerializeField]
            internal bool Status;
        }

        /// <summary>
        /// taskArguments 存储任务参数的字典，key 为任务属性，value 为参数字典。
        /// </summary>
        /// <remarks>
        /// 用于保存每个任务的参数配置，支持运行时修改和持久化。
        /// </remarks>
        internal readonly Dictionary<string, Dictionary<XEditor.Tasks.Param, string>> taskArguments = new();

        /// <summary>
        /// taskGroups 是任务组列表，每个组包含多个相关的任务。
        /// </summary>
        /// <remarks>
        /// 按照任务的 Group 属性进行分组，用于界面展示和批量操作。
        /// </remarks>
        internal readonly List<List<string>> taskGroups = new();

        /// <summary>
        /// groupFoldouts 存储任务组展开状态的列表。
        /// </summary>
        [SerializeField]
        internal List<Stateful> groupFoldouts = new();

        /// <summary>
        /// groupSelects 存储任务组选中状态的列表。
        /// </summary> 
        [SerializeField]
        internal List<Stateful> groupSelects = new();

        /// <summary>
        /// taskFoldouts 存储任务展开状态的列表。
        /// </summary>
        [SerializeField]
        internal List<Stateful> taskFoldouts = new();

        /// <summary>
        /// taskSelects 存储任务选中状态的列表。
        /// </summary>
        [SerializeField]
        internal List<Stateful> taskSelects = new();

        /// <summary>
        /// taskOrders 是任务执行顺序的列表。
        /// </summary>
        [SerializeField]
        internal List<string> taskOrders = new();

        /// <summary>
        /// taskExcutings 是正在执行的任务列表。
        /// </summary>
        internal List<string> taskExcutings = new();

        /// <summary>
        /// foldoutAll 表示是否展开所有任务组和任务。
        /// </summary>
        [SerializeField]
        internal bool foldoutAll = true;

        /// <summary>
        /// selectAll 表示是否选中所有任务组和任务。
        /// </summary>
        [SerializeField]
        internal bool selectAll = false;

        /// <summary>
        /// scroll 是滚动视图的位置。
        /// </summary>
        internal Vector2 scroll = Vector2.zero;

        /// <summary>
        /// reportContent 是任务执行的结果。
        /// </summary>
        [SerializeField]
        internal string reportContent;

        /// <summary>
        /// reportScroll 是任务执行结果显示区域的滚动位置。
        /// </summary>
        internal Vector2 reportScroll = Vector2.zero;

        /// <summary>
        /// reportHeight 是任务执行结果显示区域初始高度。
        /// </summary>
        [SerializeField]
        internal float reportHeight = 150f;

        /// <summary>
        /// reportResizing 表示是否正在调整任务执行结果显示区域高度。
        /// </summary>
        internal bool reportResizing = false;

        /// <summary>
        /// reportRoot 是任务执行结果的文件根目录。
        /// </summary>
        internal static readonly string reportRoot = XFile.PathJoin(XEnv.ProjectPath, "Library/TaskRunner");

        /// <summary>
        /// OnEnable 是窗口启用时的回调，初始化面板实例并重置状态，通知所有实现了 IOnEnable 接口的任务。
        /// </summary>
        internal void OnEnable()
        {
            // 重置数据
            taskArguments.Clear();
            taskGroups.Clear();
            taskExcutings.Clear();

            // 对任务列表进行归并
            foreach (var kvp in XEditor.Tasks.Metas)
            {
                var meta = kvp.Value;
                List<string> group = null;
                for (var j = 0; j < taskGroups.Count; j++)
                {
                    var temp = taskGroups[j];
                    if (temp != null && temp.Count > 0 && XEditor.Tasks.Metas[temp[0]].Group == meta.Group)
                    {
                        group = temp;
                        break;
                    }
                }
                if (group == null)
                {
                    group = new List<string>();
                    taskGroups.Add(group);
                }
                group.Add(kvp.Key);
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
        /// OnDisable 是窗口禁用时的回调，通知所有实现了 IOnDisable 接口的任务。
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
        /// OnLostFocus 是失去焦点时的回调，保存变更的首选项。
        /// </summary>
        internal void OnLostFocus()
        {
            if (XPrefs.Asset.Dirty && XFile.HasFile(XPrefs.Asset.File))
            {
                XPrefs.Asset.Save();
            }
        }

        /// <summary>
        /// OnDestroy 是窗口销毁时的回调，通知所有实现了 IOnDestroy 接口的任务，并清理面板实例。
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
        /// OnGUI 绘制窗口 GUI，包括任务组、任务列表、参数设置等界面元素，通知所有实现了 IOnGUI 接口的任务。
        /// </summary>
        internal void OnGUI()
        {
            #region 头部视图
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.MaxWidth(10));
            GUILayout.Space(5);
            var lastSelect = selectAll;
            selectAll = EditorGUILayout.Toggle(selectAll);
            if (lastSelect != selectAll)
            {
                if (!selectAll) taskOrders.Clear();
                foreach (var groupSelect in groupSelects) groupSelect.Status = selectAll;
                foreach (var taskSelect in taskSelects) taskSelect.Status = selectAll;
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Space(6);
            var foldoutButton = foldoutAll ? EditorGUIUtility.IconContent("IN foldout on") : EditorGUIUtility.IconContent("IN foldout");
            foldoutButton.tooltip = "(Un)foldout task(s).";
            if (GUILayout.Button(foldoutButton, EditorStyles.iconButton, GUILayout.Width(8)))
            {
                foldoutAll = !foldoutAll;
                foreach (var tasks in taskGroups)
                {
                    var groupMeta = XEditor.Tasks.Metas[tasks[0]];
                    var groupFoldout = groupFoldouts.Find(ele => ele.Key == groupMeta.Group);
                    if (groupFoldout == null)
                    {
                        groupFoldout = new Stateful { Key = groupMeta.Group };
                        groupFoldouts.Add(groupFoldout);
                    }
                    groupFoldout.Status = foldoutAll;

                    // 体验优化：不控制 task 级别的折叠
                    // foreach (var task in tasks)
                    // {
                    //     var taskFoldout = taskFoldouts.Find(ele => ele.Key == task);
                    //     if (taskFoldout == null)
                    //     {
                    //         taskFoldout = new Stateful { Key = task };
                    //         taskFoldouts.Add(taskFoldout);
                    //     }
                    //     taskFoldout.Status = foldoutAll;
                    // }
                }
            }
            GUILayout.EndVertical();

            var prefsName = string.IsNullOrEmpty(XPrefs.Asset.File) ? "Unknown" : Path.GetFileName(XPrefs.Asset.File);
            var prefsContent = $"[Preferences: {prefsName}/{XEnv.Channel}/{XEnv.Version}/{XEnv.Mode}/{XLog.Level()}]";
            var prefsInvalid = !XFile.HasFile(XPrefs.Asset.File) || XPrefs.Asset.Count == 0;
            var ocolor = GUI.color;
            if (prefsInvalid) GUI.color = Color.gray;
            if (EditorGUILayout.LinkButton(new GUIContent(prefsContent)))
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
                    foreach (var task in taskOrders)
                    {
                        workers.Add(XEditor.Tasks.Workers[task]);
                    }
                    var uworkers = new List<XEditor.Tasks.IWorker>();
                    foreach (var kvp in taskSelects)
                    {
                        if (kvp.Status)
                        {
                            var worker = XEditor.Tasks.Workers[kvp.Key];
                            if (!workers.Contains(worker))
                            {
                                uworkers.Add(worker);
                            }
                        }
                    }
                    if (uworkers.Count > 0)
                    {
                        uworkers.Sort((e1, e2) => e1.Priority.CompareTo(e2.Priority));
                        workers.AddRange(uworkers);
                    }
                    _ = Run(workers);
                });
            }
            EditorGUILayout.EndVertical();
            GUILayout.Space(4);

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4);
            EditorGUILayout.EndVertical();
            #endregion

            #region 任务列表
            scroll = GUILayout.BeginScrollView(scroll);
            if (taskGroups != null && taskGroups.Count > 0)
            {
                foreach (var tasks in taskGroups)
                {
                    var groupMeta = XEditor.Tasks.Metas[tasks[0]];
                    if (string.IsNullOrEmpty(groupMeta.Group)) continue;
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    EditorGUILayout.BeginHorizontal();
                    var groupSelect = groupSelects.Find(ele => ele.Key == groupMeta.Group);
                    if (groupSelect == null)
                    {
                        groupSelect = new Stateful { Key = groupMeta.Group };
                        groupSelects.Add(groupSelect);
                    }
                    lastSelect = groupSelect.Status;
                    var currentSelect = EditorGUILayout.Toggle(lastSelect, GUILayout.MaxWidth(15));
                    if (lastSelect != currentSelect)
                    {
                        groupSelect.Status = currentSelect;
                        foreach (var task in tasks)
                        {
                            var taskSelect = taskSelects.Find(ele => ele.Key == task);
                            if (taskSelect != null) taskSelect.Status = currentSelect;
                            if (!currentSelect) taskOrders.Remove(task);
                        }
                    }

                    var groupFoldoutRect = EditorGUILayout.GetControlRect();
                    var groupFoldout = groupFoldouts.Find(ele => ele.Key == groupMeta.Group);
                    if (groupFoldout == null)
                    {
                        groupFoldout = new Stateful { Key = groupMeta.Group };
                        groupFoldouts.Add(groupFoldout);
                    }
                    var lastFoldout = groupFoldout.Status;
                    var currentFoldout = EditorGUI.Foldout(groupFoldoutRect, groupFoldout.Status, new GUIContent(groupMeta.Group, string.Join(", ", tasks)));
                    if (lastFoldout != currentFoldout)
                    {
                        groupFoldout.Status = currentFoldout;
                        // 体验优化：不控制 task 级别的折叠
                        // foreach (var task in tasks)
                        // {
                        //     var taskFoldout = taskFoldouts.Find(ele => ele.Key == task);
                        //     if (taskFoldout == null)
                        //     {
                        //         taskFoldout = new Stateful { Key = task };
                        //         taskFoldouts.Add(taskFoldout);
                        //     }
                        //     taskFoldout.Status = currentFoldout;
                        // }
                    }

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.BeginVertical();
                    GUILayout.Space(3);
                    if (GUILayout.Button(new GUIContent("", EditorGUIUtility.FindTexture("d_PlayButton@2x"), "Execute task(s)."), EditorStyles.iconButton) ||
                    (groupFoldoutRect.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown && Event.current.button == 0 && Event.current.clickCount == 2))
                    {
                        Event.current.Use();
                        XLoom.RunInNext(() =>
                        {
                            var workers = new List<XEditor.Tasks.IWorker>();
                            foreach (var task in taskOrders)
                            {
                                if (tasks.Contains(task)) workers.Add(XEditor.Tasks.Workers[task]);
                            }
                            var uworkers = new List<XEditor.Tasks.IWorker>();
                            foreach (var kvp in taskSelects)
                            {
                                if (tasks.Contains(kvp.Key))
                                {
                                    var worker = XEditor.Tasks.Workers[kvp.Key];
                                    if (!workers.Contains(worker))
                                    {
                                        uworkers.Add(worker);
                                    }
                                }
                            }
                            if (uworkers.Count > 0)
                            {
                                uworkers.Sort((e1, e2) => e1.Priority.CompareTo(e2.Priority));
                                workers.AddRange(uworkers);
                            }
                            if (workers.Count == 0) // 未选中，则默认执行该分组的所有任务
                            {
                                foreach (var task in tasks)
                                {
                                    workers.Add(XEditor.Tasks.Workers[task]);
                                }
                                workers.Sort((e1, e2) => e1.Priority.CompareTo(e2.Priority));
                            }
                            _ = Run(workers);
                        });
                    }
                    EditorGUILayout.EndVertical();
                    GUILayout.Space(4);

                    EditorGUILayout.EndHorizontal();

                    if (groupFoldout.Status)
                    {
                        foreach (var task in tasks)
                        {
                            try
                            {
                                var taskMeta = XEditor.Tasks.Metas[task];
                                var taskWorker = XEditor.Tasks.Workers[task];

                                EditorGUILayout.BeginHorizontal();

                                EditorGUILayout.BeginVertical(GUILayout.MaxWidth(15));
                                GUILayout.Space(6);
                                var taskSelect = taskSelects.Find(ele => ele.Key == task);
                                if (taskSelect == null)
                                {
                                    taskSelect = new Stateful { Key = task };
                                    taskSelects.Add(taskSelect);
                                }
                                lastSelect = taskSelect.Status;
                                currentSelect = EditorGUILayout.Toggle(lastSelect);
                                if (lastSelect != currentSelect)
                                {
                                    taskSelect.Status = currentSelect;
                                    if (currentSelect) taskOrders.Add(task);
                                    else taskOrders.Remove(task);
                                }
                                EditorGUILayout.EndVertical();

                                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                                GUILayout.BeginHorizontal();
                                var idx = taskOrders.IndexOf(task);
                                var sidx = idx >= 0 ? $" #{idx + 1}" : "";
                                var taskFoldoutRect = EditorGUILayout.GetControlRect();
                                var taskFoldout = taskFoldouts.Find(ele => ele.Key == task);
                                if (taskFoldout == null)
                                {
                                    taskFoldout = new Stateful { Key = task };
                                    taskFoldouts.Add(taskFoldout);
                                }
                                taskFoldout.Status = EditorGUI.Foldout(taskFoldoutRect, taskFoldout.Status, new GUIContent(taskMeta.Name + sidx, taskMeta.Tooltip));
                                GUILayout.FlexibleSpace();

                                EditorGUILayout.BeginVertical();
                                GUILayout.Space(2);
                                EditorGUILayout.BeginHorizontal();
                                if (taskExcutings.Contains(task)) GUILayout.Button(EditorGUIUtility.IconContent("Loading@2x"), EditorStyles.iconButton);
                                else
                                {
                                    var reportFile = XFile.PathJoin(reportRoot, taskWorker.ID.MD5());
                                    if (XFile.HasFile(reportFile))
                                    {
                                        var reportJson = XFile.OpenText(reportFile);
                                        var report = XObject.FromJson<XEditor.Tasks.Report>(reportJson);
                                        if (report != null)
                                        {
                                            var reportIcon = report.Result == XEditor.Tasks.Result.Failed ? "d_console.erroricon.sml@2x" :
                                                report.Result == XEditor.Tasks.Result.Succeeded ? "d_console.infoicon.sml@2x" : "d_console.warnicon.sml@2x";
                                            if (GUILayout.Button(new GUIContent("", EditorGUIUtility.FindTexture(reportIcon), "Show Report."), EditorStyles.iconButton))
                                            {
                                                reportContent = new StringBuilder()
                                                .Append($"\"{taskWorker.ID}\": ")
                                                .Append(reportJson
                                                    .Replace("\"Result\": 2", "<color=red><b>\"Result\": 2</b></color>")
                                                    .Replace("\"Result\": 1", "<color=green><b>\"Result\": 1</b></color>")
                                                    .Replace("\"Result\": 0", "<color=yellow><b>\"Result\": 0</b></color>")
                                                    .Replace("\"Result\": 3", "<color=yellow><b>\"Result\": 3</b></color>"))
                                                .ToString();
                                                reportScroll = Vector2.zero;
                                            }
                                        }
                                    }
                                    if (GUILayout.Button(new GUIContent("", EditorGUIUtility.FindTexture("d_PlayButton@2x"), "Execute task."), EditorStyles.iconButton) ||
                                      (taskFoldoutRect.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown && Event.current.button == 0 && Event.current.clickCount == 2))
                                    {
                                        Event.current.Use();
                                        XLoom.RunInNext(() =>
                                        {
                                            _ = Run(new List<XEditor.Tasks.IWorker> { taskWorker });
                                        });
                                    }
                                }
                                EditorGUILayout.EndHorizontal();
                                EditorGUILayout.EndVertical();

                                GUILayout.EndHorizontal();

                                if (taskFoldout.Status)
                                {
                                    taskArguments.TryGetValue(task, out var marguments);

                                    var draw = false;
                                    if (taskMeta.Params != null && taskMeta.Params.Count > 0)
                                    {
                                        draw = true;
                                        if (marguments == null)
                                        {
                                            marguments = new Dictionary<XEditor.Tasks.Param, string>();
                                            taskArguments[task] = marguments;
                                        }
                                        foreach (var param in taskMeta.Params)
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

                                    if (taskMeta.Worker != null && typeof(XEditor.Tasks.Panel.IOnGUI).IsAssignableFrom(taskMeta.Worker))
                                    {
                                        draw = true;
                                        try
                                        {
                                            try { (taskWorker as XEditor.Tasks.Panel.IOnGUI).OnGUI(); }
                                            catch (Exception e) { XLog.Panic(e); }
                                        }
                                        catch (Exception e) { XLog.Panic(e); }
                                    }

                                    if (!draw)
                                    {
                                        // 绘制一个空的提示，避免无 GUI 变化，影响用户体验
                                        EditorGUILayout.HelpBox("No Params or GUI Panel.", MessageType.None);
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
            #endregion

            #region 日志视图
            // 日志分割
            {
                var splitter = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(8f), GUILayout.ExpandWidth(true));
                var line = new Rect(splitter.x, splitter.center.y - 0.65f, splitter.width, 1f);  // 只画中间一条细线
                EditorGUI.DrawRect(line, Color.black);
                EditorGUIUtility.AddCursorRect(splitter, MouseCursor.ResizeVertical);

                if (Event.current.type == EventType.MouseDown && splitter.Contains(Event.current.mousePosition))
                {
                    reportResizing = true;
                }
                if (reportResizing)
                {
                    if (Event.current.type == EventType.MouseDrag)
                    {
                        reportHeight -= Event.current.delta.y;
                        reportHeight = Mathf.Clamp(reportHeight, 50, position.height - 100);
                        Event.current.Use();
                    }
                    if (Event.current.type == EventType.MouseUp)
                    {
                        reportResizing = false;
                    }
                }
            }

            // 日志显示
            {
                reportScroll = EditorGUILayout.BeginScrollView(reportScroll, GUILayout.Height(reportHeight));
                var richTextStyle = new GUIStyle(EditorStyles.label) { richText = true, wordWrap = true };
                EditorGUILayout.SelectableLabel(
                    reportContent,
                    richTextStyle,
                    GUILayout.ExpandWidth(true),
                    GUILayout.Height(richTextStyle.CalcHeight(new GUIContent(reportContent), position.width - 20))
                );
                EditorGUILayout.EndScrollView();
            }
            #endregion
        }

        /// <summary>
        /// Run 执行任务列表。
        /// </summary>
        /// <param name="workers">要执行的任务列表</param>
        internal async Task Run(List<XEditor.Tasks.IWorker> workers)
        {
            if (workers == null || workers.Count == 0) return;

            // 检查是否存在同步任务
            var hasSync = false;
            foreach (var worker in workers)
            {
                var meta = XEditor.Tasks.Metas[worker.ID];
                if (!meta.Runasync) hasSync = true;
                if (!taskExcutings.Contains(worker.ID))
                {
                    // 监控正在执行的任务
                    taskExcutings.Add(worker.ID);
                }
            }
            Repaint(); // 主动刷新 GUI 面板的 Loading 状态
            await Task.Yield(); // 等待 GUI 面板刷新完成

            // 执行任务
            foreach (var worker in workers)
            {
                var meta = XEditor.Tasks.Metas[worker.ID];
                var arguments = new Dictionary<string, string>();
                taskArguments.TryGetValue(worker.ID, out var marguments);
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
                if (worker.Runasync) await report.Task;

                // 移除正在执行的任务
                taskExcutings.Remove(worker.ID);
                Repaint(); // 主动刷新 GUI 面板的 Loading 状态
                await Task.Yield(); // 等待 GUI 面板刷新完成

                // 保存任务执行结果
                var reportJson = XObject.ToJson(report, true);
                var reportFile = XFile.PathJoin(reportRoot, worker.ID.MD5());
                XFile.SaveText(reportFile, reportJson);

                reportContent = new StringBuilder()
                .Append($"\"{worker.ID}\": ")
                .Append(reportJson
                    .Replace("\"Result\": 2", "<color=red><b>\"Result\": 2</b></color>")
                    .Replace("\"Result\": 1", "<color=green><b>\"Result\": 1</b></color>")
                    .Replace("\"Result\": 0", "<color=yellow><b>\"Result\": 0</b></color>")
                    .Replace("\"Result\": 3", "<color=yellow><b>\"Result\": 3</b></color>"))
                .ToString();
                reportScroll = Vector2.zero;
            }
        }
    }
}
