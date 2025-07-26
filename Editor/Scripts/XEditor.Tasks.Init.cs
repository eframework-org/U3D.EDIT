// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using EFramework.Utility;

namespace EFramework.Editor
{
    public partial class XEditor
    {
        public partial class Tasks
        {
            /// <summary>
            /// Init 是任务系统的初始化器，负责在编辑器加载时解析和初始化任务配置。
            /// </summary>
            internal class Init : Event.Internal.OnEditorLoad
            {
                /// <summary>
                /// Priority 是事件处理优先级，设为 -1 确保在其他处理器之前执行。
                /// </summary>
                int Event.Callback.Priority => -1;

                /// <summary>
                /// Singleton 表示是否为单例事件处理器，确保只有一个实例在运行。
                /// </summary>
                bool Event.Callback.Singleton => true;

                /// <summary>
                /// Process 初始化任务系统，在 Unity 编辑器加载时调用。
                /// </summary>
                /// <param name="_">未使用的参数数组</param>
                void Event.Internal.OnEditorLoad.Process(params object[] _) { Parse(); }

                /// <summary>
                /// Parse 使用默认配置解析任务系统。
                /// </summary>
                internal static void Parse() { Parse(false, true, XFile.PathJoin(XEnv.ProjectPath, "package.json")); }

                /// <summary>
                /// Parse 使用指定配置解析任务系统。
                /// </summary>
                /// <param name="test">是否为测试模式</param>
                /// <param name="attribute">是否解析特性定义</param>
                /// <param name="packageFile">package.json 文件路径</param>
                internal static void Parse(bool test = false, bool attribute = false, string packageFile = "")
                {
                    Singletons.Clear();
                    Metas.Clear();
                    Workers.Clear();

                    var packageMd5 = "";

                    // 解析 package.json 定义的任务
                    bool parsePackage()
                    {
                        try
                        {
                            if (XFile.HasFile(packageFile))
                            {
                                var md5 = XFile.FileMD5(packageFile);
                                if (packageMd5 != md5)
                                {
                                    var exists = false;
                                    packageMd5 = md5;
                                    var deletes = new List<string>();
                                    foreach (var kvp in Workers)
                                    {
                                        if (kvp.Value is Npm)
                                        {
                                            exists = true;
                                            deletes.Add(kvp.Key);
                                        }
                                    }
                                    foreach (var meta in deletes)
                                    {
                                        Metas.Remove(meta);
                                        Workers.Remove(meta);
                                    }

                                    var pkgJson = JSON.Parse(XFile.OpenText(packageFile));
                                    var scriptsMeta = pkgJson["scriptsMeta"];
                                    var count = 0;
                                    if (scriptsMeta != null)
                                    {
                                        foreach (var kvp0 in scriptsMeta)
                                        {
                                            var script = kvp0.Key;
                                            var name = "";
                                            var group = "";
                                            var tooltip = "";
                                            var priority = 0;
                                            var singleton = false;
                                            var runasync = true;
                                            var platform = XEnv.PlatformType.Unknown;
                                            var @params = new List<Param>();

                                            foreach (var kvp1 in kvp0.Value)
                                            {
                                                if (kvp1.Key == "name") name = kvp1.Value;
                                                else if (kvp1.Key == "group") group = kvp1.Value;
                                                else if (kvp1.Key == "tooltip") tooltip = kvp1.Value;
                                                else if (kvp1.Key == "priority") priority = kvp1.Value;
                                                else if (kvp1.Key == "singleton") singleton = kvp1.Value;
                                                else if (kvp1.Key == "runasync") runasync = kvp1.Value;
                                                else if (kvp1.Key == "platform") Enum.TryParse(kvp1.Value, out platform);
                                                else if (kvp1.Key == "params")
                                                {
                                                    foreach (var kvp2 in kvp1.Value)
                                                    {
                                                        Enum.TryParse<XEnv.PlatformType>(kvp2.Value["platform"], out var pplatform);
                                                        if (pplatform != XEnv.PlatformType.Unknown && pplatform != XEnv.Platform) continue;

                                                        var pname = kvp2.Value["name"];
                                                        var pdefault = kvp2.Value["default"];
                                                        var ppersist = kvp2.Value["persist"].AsBool;
                                                        var ptooltip = kvp2.Value["tooltip"];
                                                        var param = new Param(name: pname, tooltip: ptooltip, defval: pdefault, persist: ppersist, platform: pplatform);
                                                        @params.Add(param);
                                                    }
                                                }
                                                //else if (kvp1.Key == "triggers")
                                                //{
                                                //    foreach (var kvp2 in kvp1.Value)
                                                //    {
                                                //        var ptype = kvp2.Value["type"];
                                                //        var ptime = kvp2.Value["default"];
                                                //        var ppersist = kvp2.Value["persist"].AsBool;
                                                //        var ptooltip = kvp2.Value["tooltip"];
                                                //        var param = new Param(pname, ptooltip, pdefault, ppersist);
                                                //        @params.Add(param);
                                                //    }
                                                //}
                                            }

                                            if (platform != XEnv.PlatformType.Unknown && platform != XEnv.Platform) continue;

                                            var task = new WorkerAttribute(string.IsNullOrEmpty(name) ? script : name, string.IsNullOrEmpty(group) ? "Npm Scripts" : group, tooltip, priority, singleton, runasync);
                                            var worker = new Npm($"{task.Group}/{task.Name}", script, singleton, runasync, Application.isBatchMode, priority);
                                            task.Params = new List<Param>();
                                            foreach (var param in @params)
                                            {
                                                var nparam = new Param(name: param.Name, tooltip: param.Tooltip, defval: param.Default, persist: param.Persist, platform: param.Platform)
                                                {
                                                    ID = $"Task/{XEnv.Platform}/{worker.ID}/{param.Name}@Editor"
                                                };
                                                task.Params.Add(nparam);
                                            }
                                            task.Worker = worker.GetType();

                                            Metas.Add(worker.ID, task);
                                            Workers.Add(worker.ID, worker);
                                            count++;
                                        }

                                        if (count > 0) XLog.Debug("XEditor.Tasks.Init: parsed {0} task(s) from package.json.", count);
                                    }

                                    return count > 0 || exists;
                                }
                            }
                        }
                        catch (Exception e) { XLog.Panic(e); }
                        return false;
                    }

                    // 解析 Attribute 标记的任务
                    void parseAttribute()
                    {
                        var types = TypeCache.GetTypesWithAttribute<WorkerAttribute>();
                        var count = 0;
                        foreach (var type in types)
                        {
                            var metas = type.GetCustomAttributes<WorkerAttribute>(false);
                            if (metas.Count() == 0) continue;

                            var @params = type.GetCustomAttributes<Param>(true).ToList();
                            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                            fields = fields.OrderBy(field => field.DeclaringType == type ? 1 : 0).ToArray();
                            foreach (var field in fields)
                            {
                                var pattr = field.GetCustomAttribute<Param>();
                                if (pattr != null)
                                {
                                    if (pattr.Platform != XEnv.PlatformType.Unknown && pattr.Platform != XEnv.Platform) continue;
                                    if (string.IsNullOrEmpty(pattr.Name)) pattr.Name = field.Name;
                                    @params.Add(pattr);
                                }
                            }

                            foreach (var meta in metas)
                            {
                                try
                                {
                                    if (meta.Platform != XEnv.PlatformType.Unknown && meta.Platform != XEnv.Platform) continue;
                                    if (meta.Worker == null && typeof(IWorker).IsAssignableFrom(type))
                                    {
                                        meta.Worker = type;
                                    }
                                    var worker = Activator.CreateInstance(meta.Worker) as IWorker;
                                    worker.ID = $"{meta.Group}/{meta.Name}";
                                    worker.Singleton = meta.Singleton;
                                    worker.Runasync = meta.Runasync;
                                    worker.Priority = meta.Priority;
                                    worker.Batchmode = Application.isBatchMode;
                                    meta.Params = new List<Param>();
                                    foreach (var param in @params)
                                    {
                                        var nparam = new Param(name: param.Name, tooltip: param.Tooltip, defval: param.Default, persist: param.Persist, platform: param.Platform)
                                        {
                                            ID = $"Task/{XEnv.Platform}/{worker.ID}/{param.Name}@Editor"
                                        };
                                        meta.Params.Add(nparam);
                                    }
                                    Metas.Add(worker.ID, meta);
                                    Workers.Add(worker.ID, worker);
                                    count++;
                                }
                                catch (Exception e) { XLog.Panic(e, meta.Name); }
                            }
                        }

                        if (count > 0) XLog.Debug("XEditor.Tasks.Init: parsed {0} task(s) from attribute.", count);
                    }

                    if (XFile.HasFile(packageFile)) parsePackage();
                    if (attribute) parseAttribute();
                    Panel.Reset();

                    if (XFile.HasFile(packageFile)) XLoom.SetInterval(() =>
                    {
                        if (parsePackage()) Panel.Reset();
                    }, 1000);
                }
            }
        }
    }
}