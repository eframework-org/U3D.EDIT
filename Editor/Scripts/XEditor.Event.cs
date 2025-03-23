// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using EFramework.Utility;

namespace EFramework.Editor
{
    public partial class XEditor
    {
        /// <summary>
        /// XEditor.Event 提供了基于接口的编辑器事件系统，支持自定义事件回调和通知，内置了一些常用的事件，如编辑器初始化、更新和退出等。
        /// </summary>
        /// <remarks>
        /// <code>
        /// 功能特性
        /// - 支持自定义事件回调：通过接口方式定义事件，支持优先级排序和单例模式
        /// - 提供参数解析工具：简化事件参数的获取和类型转换
        /// - 内置常用事件：提供初始化、加载、更新和退出等常用的事件
        /// 
        /// 使用手册
        /// 1. 事件回调定义
        /// 
        /// 1.1 创建事件回调类
        /// 
        /// 	实现 XEditor.Event.Callback 接口和对应的事件接口，创建自定义事件处理器。
        /// 
        /// 	public class MyEditorInitHandler : XEditor.Event.Internal.OnEditorInit
        /// 	{
        /// 	    public int Priority => 0;
        /// 	    public bool Singleton => true;
        /// 	    
        ///         // 必须使用显式接口实现
        /// 	    void XEditor.Event.Internal.OnEditorInit.Process(params object[] args)
        /// 	    {
        /// 	        Debug.Log("编辑器初始化完成");
        /// 	    }
        /// 	}
        /// 
        /// 2. 事件通知
        /// 
        /// 2.1 内置事件清单
        /// 
        /// 	XEditor.Event 提供了以下内置事件：
        /// 	- OnEditorInit: 编辑器初始化事件，在编辑器首次启动时触发
        /// 	- OnEditorLoad: 编辑器加载事件，在每次编辑器启动时触发
        /// 	- OnEditorUpdate: 编辑器更新事件，在编辑器每帧更新时触发
        /// 	- OnEditorQuit: 编辑器退出事件，在编辑器关闭时触发
        /// 	- OnPreferencesApply: 首选项应用事件，在首选项数据应用时触发
        /// 	- OnPreprocessBuild: 构建前处理事件，在项目构建开始前触发，参数：BuildReport report
        /// 	- OnPostprocessBuild: 构建后处理事件，在项目构建完成后触发，参数：BuildReport report
        /// 
        /// 2.2 触发事件通知
        /// 
        /// 	使用 XEditor.Event.Notify&lt;T&gt; 方法触发事件。
        /// 
        /// 	XEditor.Event.Notify&lt;OnMyCustomEvent&gt;("自定义消息", 200);
        /// 
        /// 3. 参数解析
        /// 
        /// 3.1 单参数解析
        /// 
        /// 	使用 XEditor.Event.Decode&lt;T&gt; 方法解析单个参数。
        /// 
        /// 	void IMyEventHandler.Process(params object[] args)
        /// 	{
        /// 	    XEditor.Event.Decode&lt;string&gt;(out var message, args);
        /// 	    Debug.Log($"消息: {message}");
        /// 	}
        /// 
        /// 3.2 多参数解析
        /// 
        /// 	使用 XEditor.Event.Decode&lt;T1, T2&gt; 或 XEditor.Event.Decode&lt;T1, T2, T3&gt; 方法解析多个参数。
        /// 
        /// 	void IMyEventHandler.Process(params object[] args)
        /// 	{
        /// 	    XEditor.Event.Decode&lt;string, int&gt;(out var message, out var value, args);
        /// 	    Debug.Log($"消息: {message}, 值: {value}");
        /// 	}
        /// 
        /// 注意事项
        /// 1. 事件回调必须使用显式接口实现（void InterfaceName.Process）
        /// 2. 单例模式需要提供公共的 Instance 静态属性/字段
        /// 3. 回调优先级通过 Priority 属性控制，数值越小优先级越高
        /// </code>
        /// 更多信息请参考模块文档。
        /// </remarks>
        public partial class Event
        {
            /// <summary>
            /// 事件回调接口，所有事件处理器都需要实现此接口。
            /// </summary>
            public interface Callback
            {
                /// <summary>
                /// 获取回调执行优先级，数值越小优先级越高。
                /// </summary>
                int Priority { get; }

                /// <summary>
                /// 获取回调是否为单例模式，若为 true 则只创建一个实例。
                /// </summary>
                bool Singleton { get; }
            }

            /// <summary>
            /// 内置事件标识，通过接口方式声明各类事件。
            /// </summary>
            public static class Internal
            {
                /// <summary>
                /// 编辑器初始化事件，在编辑器首次启动时触发。
                /// </summary>
                public interface OnEditorInit : Callback { void Process(params object[] args); }

                /// <summary>
                /// 编辑器加载事件，在每次编辑器启动时触发。
                /// </summary>
                public interface OnEditorLoad : Callback { void Process(params object[] args); }

                /// <summary>
                /// 编辑器更新事件，在编辑器每帧更新时触发。
                /// </summary>
                public interface OnEditorUpdate : Callback { void Process(params object[] args); }

                /// <summary>
                /// 编辑器退出事件，在编辑器关闭时触发。
                /// </summary>
                public interface OnEditorQuit : Callback { void Process(params object[] args); }

                /// <summary>
                /// 首选项应用事件，在首选项应用时触发。
                /// </summary>
                public interface OnPreferencesApply : Callback { void Process(params object[] args); }

                /// <summary>
                /// 构建前处理事件，在项目构建开始前触发。
                /// </summary>
                public interface OnPreprocessBuild : Callback { void Process(params object[] args); }

                /// <summary>
                /// 构建后处理事件，在项目构建完成后触发。
                /// </summary>
                public interface OnPostprocessBuild : Callback { void Process(params object[] args); }
            }

            /// <summary>
            /// 存储事件标识与回调类型的映射关系。
            /// </summary>
            internal static Dictionary<Type, List<Type>> Callbacks = new();

            /// <summary>
            /// 存储单例模式的回调实例。
            /// </summary>
            internal static Dictionary<Type, Callback> Singletons = new();

            /// <summary>
            /// 临时存储当前批次要执行的回调实例。
            /// </summary>
            internal static List<Callback> Batches = new();

            /// <summary>
            /// 编辑器初始化方法，自动注册所有实现了 Callback 接口的类型。
            /// </summary>
            [InitializeOnLoadMethod]
            internal static void OnInit()
            {
                var ctype = typeof(Callback);
                var evts = TypeCache.GetTypesDerivedFrom(ctype);
                foreach (var type in evts)
                {
                    if (type.IsInterface) continue;
                    Callback target = null;

                    // 首先尝试获取单例实例
                    try
                    {
                        var prop = type.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                        if (prop != null)
                        {
                            target = prop.GetValue(null) as Callback;
                        }
                        else
                        {
                            var field = type.GetField("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                            if (field != null)
                            {
                                target = field.GetValue(null) as Callback;
                            }
                        }
                    }
                    catch (Exception) { }

                    // 如果不是单例或获取单例实例失败，则尝试创建新实例
                    if (target == null || !target.Singleton)
                    {
                        try
                        {
                            target = Activator.CreateInstance(type) as Callback;
                        }
                        catch (Exception e)
                        {
                            XLog.Error("XEditor.Event.OnInit: create instance of {0} failed: {1}", type.FullName, e.Message);
                        }
                    }

                    if (target != null)
                    {
                        if (target.Singleton) Singletons[type] = target;
                        var eids = type.GetInterfaces();
                        foreach (var eid in eids)
                        {
                            if (ctype != eid && ctype.IsAssignableFrom(eid))
                            {
                                if (Callbacks.TryGetValue(eid, out List<Type> callbacks) == false)
                                {
                                    callbacks = new List<Type>();
                                    Callbacks.Add(eid, callbacks);
                                }
                                callbacks.Add(type);
                            }
                        }
                    }
                }
                XLog.Debug("XEditor.Event.OnInit: parsed {0} event(s) with {1} singleton(s).", Callbacks.Count, Singletons.Count);

                var onEditorInitKey = XFile.PathJoin(Path.GetFullPath("./"), typeof(Internal.OnEditorInit).FullName);

                var persistTime = EditorPrefs.GetInt(onEditorInitKey);
                var nowTime = EditorApplication.timeSinceStartup;
                EditorPrefs.SetInt(onEditorInitKey, (int)nowTime);
                var lastTime = nowTime;
                var binit = false;
                if (nowTime <= persistTime || persistTime == 0) binit = true;
                var bload = true;
                EditorApplication.update += () =>
                {
                    nowTime = EditorApplication.timeSinceStartup;
                    var deltaTime = nowTime - lastTime;
                    lastTime = nowTime;
                    if (binit)
                    {
                        binit = false;
                        Notify<Internal.OnEditorInit>();
                    }
                    if (bload)
                    {
                        bload = false;
                        Notify<Internal.OnEditorLoad>();
                    }
                    Notify<Internal.OnEditorUpdate>(deltaTime);
                };
                EditorApplication.quitting += () =>
                {
                    EditorPrefs.SetInt(onEditorInitKey, 0);
                    Notify<Internal.OnEditorQuit>();
                };
            }

            /// <summary>
            /// 通知指定类型的事件，触发对应的回调处理。
            /// </summary>
            /// <typeparam name="T">事件标识类型。</typeparam>
            /// <param name="args">传递给回调的参数列表。</param>
            public static void Notify<T>(params object[] args) { Notify(typeof(T), args); }

            /// <summary>
            /// 通知指定类型的事件，触发对应的回调处理。
            /// </summary>
            /// <param name="eid">事件标识类型。</param>
            /// <param name="args">传递给回调的参数列表。</param>
            /// <exception cref="ArgumentNullException">当事件标识为空时抛出。</exception>
            public static void Notify(Type eid, params object[] args)
            {
                if (eid == null) throw new ArgumentNullException("eid");

                var ename = eid.FullName.Replace("+", ".");
                if (Callbacks.TryGetValue(eid, out var callbacks))
                {
                    if (callbacks != null && callbacks.Count > 0)
                    {
                        Batches.Clear();
                        for (int i = 0; i < callbacks.Count;)
                        {
                            var callback = callbacks[i];
                            if (callback == null) callbacks.RemoveAt(i);
                            else
                            {
                                if (Singletons.TryGetValue(callback, out var target) == false)
                                {
                                    try { target = Activator.CreateInstance(callback) as Callback; }
                                    catch (Exception e) { XLog.Panic(e); }
                                }
                                if (target == null) callbacks.RemoveAt(i);
                                else
                                {
                                    i++;
                                    Batches.Add(target);
                                }
                            }
                        }

                        Batches.Sort((e1, e2) => e1.Priority.CompareTo(e2.Priority));
                        for (int i = 0; i < Batches.Count; i++)
                        {
                            var callback = Batches[i];
                            //try
                            //{
                            var methods = callback.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
                            foreach (var method in methods)
                            {
                                if (method.Name.StartsWith(ename))
                                {
                                    method.Invoke(callback, new object[] { args });
                                    break;
                                }
                            }
                            //}
                            //catch (Exception e)
                            //{
                            //    var error = XString.Format("XEditor.Event.Notify: invoke eid={0} of callback={1} error: {2}", eid, callback != null ? callback.GetType().FullName : "Null", e.Message);
                            //    XLog.Panic(e, error);
                            //    XLog.Error(error);
                            //}
                            // handle exception in logic level
                        }
                        Batches.Clear(); // release references
                    }
                }
            }

            /// <summary>
            /// 从参数列表中解析单个参数。
            /// </summary>
            /// <typeparam name="T1">第一个参数的类型。</typeparam>
            /// <param name="arg1">输出的第一个参数。</param>
            /// <param name="args">原始参数列表。</param>
            public static void Decode<T1>(out T1 arg1, params object[] args)
            {
                arg1 = args != null && args.Length > 0 ? (T1)args[0] : default;
            }

            /// <summary>
            /// 从参数列表中解析两个参数。
            /// </summary>
            /// <typeparam name="T1">第一个参数的类型。</typeparam>
            /// <typeparam name="T2">第二个参数的类型。</typeparam>
            /// <param name="arg1">输出的第一个参数。</param>
            /// <param name="arg2">输出的第二个参数。</param>
            /// <param name="args">原始参数列表。</param>
            public static void Decode<T1, T2>(out T1 arg1, out T2 arg2, params object[] args)
            {
                arg1 = args != null && args.Length > 0 ? (T1)args[0] : default;
                arg2 = args != null && args.Length > 1 ? (T2)args[1] : default;
            }

            /// <summary>
            /// 从参数列表中解析三个参数。
            /// </summary>
            /// <typeparam name="T1">第一个参数的类型。</typeparam>
            /// <typeparam name="T2">第二个参数的类型。</typeparam>
            /// <typeparam name="T3">第三个参数的类型。</typeparam>
            /// <param name="arg1">输出的第一个参数。</param>
            /// <param name="arg2">输出的第二个参数。</param>
            /// <param name="arg3">输出的第三个参数。</param>
            /// <param name="args">原始参数列表。</param>
            public static void Decode<T1, T2, T3>(out T1 arg1, out T2 arg2, out T3 arg3, params object[] args)
            {
                arg1 = args != null && args.Length > 0 ? (T1)args[0] : default;
                arg2 = args != null && args.Length > 1 ? (T2)args[1] : default;
                arg3 = args != null && args.Length > 2 ? (T3)args[2] : default;
            }
        }

        public partial class Event : IPreprocessBuildWithReport, IPostprocessBuildWithReport
        {
            /// <summary>
            /// 获取回调执行顺序，值越小越先执行。
            /// </summary>
            int IOrderedCallback.callbackOrder => -1;

            /// <summary>
            /// 构建前处理回调，触发 OnPreprocessBuild 事件。
            /// </summary>
            /// <param name="report">构建报告。</param>
            void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report) { Notify<Internal.OnPreprocessBuild>(report); }

            /// <summary>
            /// 构建后处理回调，触发 OnPostprocessBuild 事件。
            /// </summary>
            /// <param name="report">构建报告。</param>
            void IPostprocessBuildWithReport.OnPostprocessBuild(BuildReport report) { Notify<Internal.OnPostprocessBuild>(report); }
        }
    }
}
