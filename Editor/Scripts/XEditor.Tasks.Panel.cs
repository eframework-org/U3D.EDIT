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
            /// Panel 是任务面板的管理器，提供面板的创建、显示和生命周期管理。
            /// </summary>
            public class Panel : Event.Internal.OnEditorLoad
            {
                /// <summary>
                /// IOnEnable 是任务面板打开时的回调接口，用于初始化任务状态和资源。
                /// </summary>
                public interface IOnEnable { void OnEnable(); }

                /// <summary>
                /// IOnGUI 是任务面板绘制时的回调接口，用于绘制自定义界面元素。
                /// </summary>
                public interface IOnGUI { void OnGUI(); }

                /// <summary>
                /// IOnDisable 是任务面板关闭时的回调接口，用于保存状态和清理资源。
                /// </summary>
                public interface IOnDisable { void OnDisable(); }

                /// <summary>
                /// IOnDestroy 是任务面板销毁时的回调接口，用于执行最终的清理操作。
                /// </summary>
                public interface IOnDestroy { void OnDestroy(); }

                /// <summary>
                /// MenuPath 是菜单栏的路径，定义了在 Unity 主菜单中的位置。
                /// </summary>
                internal const string MenuPath = "Tools/EFramework/Task Runner";

                /// <summary>
                /// Priority 是事件处理的优先级。
                /// </summary>
                int Event.Callback.Priority => 0;

                /// <summary>
                /// Singleton 表示是否为单例事件处理器。
                /// </summary>
                bool Event.Callback.Singleton => false;

                /// <summary>
                /// Process 初始化任务系统，在 Unity 编辑器加载时调用。
                /// </summary>
                void Event.Internal.OnEditorLoad.Process(params object[] _) { Reset(); }

                /// <summary>
                /// Open 打开任务窗口。
                /// </summary>
                [MenuItem(MenuPath)]
                public static void Open()
                {
                    var name = Path.GetFileName(MenuPath);
                    var window = EditorWindow.GetWindow<TaskRunner>(name);
                    window.titleContent = new GUIContent(name, EditorGUIUtility.IconContent("d_PlayButton@2x").image);
                }

                /// <summary>
                /// Reset 重置任务窗口状态。
                /// </summary>
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
