// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System.IO;
using UnityEngine;
using UnityEditor;

namespace EFramework.Editor
{
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
                void Event.Internal.OnEditorLoad.Process(params object[] _) { Reset(); }

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
                    var window = EditorWindow.GetWindow<TaskRunner>(name);
                    window.titleContent = new GUIContent(name, EditorGUIUtility.IconContent("d_PlayButton@2x").image);
                }

                /// <summary>
                /// 重置任务窗口状态。
                /// </summary>
                /// <remarks>
                /// 重新初始化任务面板的状态，通常在任务配置发生变化时调用。
                /// </remarks>
                public static void Reset()
                {
                    var windows = Resources.FindObjectsOfTypeAll<TaskRunner>();
                    if (windows != null && windows.Length > 0)
                    {
                        foreach (var window in windows)
                        {
                            window.OnEnable();
                        }
                    }
                }
            }
        }
    }
}
