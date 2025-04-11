// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EFramework.Utility;

namespace EFramework.Editor
{
    public partial class XEditor
    {
        /// <summary>
        /// XEditor.Tasks 提供了一个编辑器任务调度系统，基于 C# Attribute 或 Npm Scripts 定义任务，支持可视化交互、命令行参数、脚本调用等方式同步/异步执行任务。
        /// </summary>
        /// <remarks>
        /// <code>
        /// 功能特性
        /// - 可视化任务管理器：直观地管理和执行任务
        /// - 任务分组和优先级：细粒度控制任务执行顺序
        /// - 多种任务声明方式：基于 C# Attribute 或 Npm Scripts 定义任务
        /// - 前置和后置处理器：完整的任务生命周期管理机制
        /// - 支持批处理模式：命令行自动执行无需界面交互
        /// 
        /// 使用手册
        /// 1. 基础用法
        /// 
        /// 1.1 打开视图
        ///     通过菜单项 `Tools/EFramework/Task Runner` 或快捷键 `Ctrl+P` 打开任务管理器。
        ///     
        ///     // 也可以通过代码打开
        ///     XEditor.Tasks.Panel.Open();
        /// 
        /// 1.2 执行任务
        ///     // 创建任务实例
        ///     var worker = new MyTask();
        ///     
        ///     // 设置任务参数
        ///     var args = new Dictionary&lt;string, string&gt; { 
        ///         { &quot;参数名&quot;, &quot;参数值&quot; } 
        ///     };
        ///     
        ///     // 执行任务
        ///     var report = XEditor.Tasks.Execute(worker, args);
        ///     
        ///     // 等待任务完成
        ///     report.Task.Wait();
        ///     
        ///     // 检查执行结果
        ///     if (report.Result == XEditor.Tasks.Result.Succeeded) {
        ///         Debug.Log(&quot;任务执行成功&quot;);
        ///     } else {
        ///         Debug.LogError($&quot;任务执行失败: {report.Error}&quot;);
        ///     }
        /// 
        /// 2. 自定义任务
        /// 
        /// 2.1 定义任务
        /// 
        /// 2.1.1 基于 C# Attribute 定义任务
        ///     [XEditor.Tasks.Worker(&quot;我的任务&quot;, &quot;任务分组&quot;, &quot;任务说明&quot;)]
        ///     public class MyTask : XEditor.Tasks.Worker
        ///     {
        ///         [XEditor.Tasks.Param(&quot;参数1&quot;, &quot;参数说明&quot;, &quot;默认值&quot;)]
        ///         public string MyParam;
        ///         
        ///         public override void Process(XEditor.Tasks.Report report)
        ///         {
        ///             // 获取任务参数
        ///             var value = report.Arguments.TryGetValue(&quot;参数1&quot;, out var val) ? val : &quot;&quot;;
        ///             
        ///             // 执行任务逻辑
        ///             Debug.Log($&quot;执行任务，参数值: {value}&quot;);
        ///             
        ///             // 任务成功完成
        ///             report.Result = XEditor.Tasks.Result.Succeeded;
        ///         }
        ///     }
        /// 
        /// 2.1.2 基于 Npm Scripts 定义任务
        ///     通过 package.json 中的 scriptsMeta 配置定义任务：
        ///     
        ///     {
        ///       &quot;name&quot;: &quot;my-package&quot;,
        ///       &quot;version&quot;: &quot;1.0.0&quot;,
        ///       &quot;scripts&quot;: {
        ///         &quot;build&quot;: &quot;echo 执行构建&quot;,
        ///         &quot;test&quot;: &quot;echo 执行测试&quot;
        ///       },
        ///       &quot;scriptsMeta&quot;: {
        ///         &quot;build&quot;: {
        ///           &quot;name&quot;: &quot;构建任务&quot;,
        ///           &quot;group&quot;: &quot;构建&quot;,
        ///           &quot;tooltip&quot;: &quot;执行项目构建&quot;,
        ///           &quot;priority&quot;: 1,
        ///           &quot;singleton&quot;: true,
        ///           &quot;runasync&quot;: true,
        ///           &quot;params&quot;: [
        ///             {
        ///               &quot;name&quot;: &quot;env&quot;,
        ///               &quot;tooltip&quot;: &quot;构建环境&quot;,
        ///               &quot;default&quot;: &quot;dev&quot;,
        ///               &quot;persist&quot;: true,
        ///               &quot;platform&quot;: &quot;Unknown&quot;
        ///             }
        ///           ]
        ///         },
        ///         &quot;test&quot;: {
        ///           &quot;name&quot;: &quot;测试任务&quot;,
        ///           &quot;group&quot;: &quot;测试&quot;,
        ///           &quot;priority&quot;: 2
        ///         }
        ///       }
        ///     }
        ///     
        ///     scriptsMeta 中的每个键对应 scripts 中的脚本名称，值为任务配置对象：
        ///     
        ///     name: 字符串，任务显示名称，默认为脚本名称
        ///     group: 字符串，任务分组，默认为 &quot;Npm Scripts&quot;
        ///     tooltip: 字符串，任务提示信息，默认为空
        ///     priority: 整数，任务优先级，默认为 0
        ///     singleton: 布尔值，是否为单例任务，默认为 false
        ///     runasync: 布尔值，是否异步执行，默认为 true
        ///     platform: 字符串，任务适用平台，默认为 &quot;Unknown&quot;(所有平台)
        ///     params: 数组，任务参数列表，默认为 []
        ///     
        ///     params 数组中的每个对象定义一个任务参数：
        ///     
        ///     name: 字符串，参数名称，必填
        ///     tooltip: 字符串，参数提示信息，默认为空
        ///     default: 字符串，参数默认值，默认为空
        ///     persist: 布尔值，是否持久化保存，默认为 false
        ///     platform: 字符串，参数适用平台，默认为 &quot;Unknown&quot;(所有平台)
        /// 
        /// 2.2 生命周期
        ///     public override void Preprocess(XEditor.Tasks.Report report)
        ///     {
        ///         // 任务执行前的准备工作
        ///         Debug.Log(&quot;开始预处理&quot;);
        ///     }
        ///     
        ///     public override void Process(XEditor.Tasks.Report report)
        ///     {
        ///         // 任务主要逻辑
        ///         Debug.Log(&quot;执行主处理&quot;);
        ///     }
        ///     
        ///     public override void Postprocess(XEditor.Tasks.Report report)
        ///     {
        ///         // 任务完成后的清理工作
        ///         Debug.Log(&quot;执行后处理&quot;);
        ///     }
        /// 
        /// 3. 高级功能
        /// 
        /// 3.1 流程处理器
        ///     // 定义处理器接口
        ///     internal interface MyPreHandler : XEditor.Event.Callback { void Process(params object[] args); }
        ///     
        ///     // 实现处理器
        ///     internal class MyPreProcessor : MyPreHandler
        ///     {
        ///         public int Priority =&gt; 0; // 处理器优先级
        ///         public bool Singleton =&gt; true; // 是否为单例处理器
        ///     
        ///         void MyPreHandler.Process(params object[] args)
        ///         {
        ///             // 解码参数
        ///             XEditor.Event.Decode(out var worker, out var report, args);
        ///             
        ///             // 处理逻辑
        ///             Debug.Log(&quot;执行前置处理&quot;);
        ///         }
        ///     }
        ///     
        ///     // 在任务上应用处理器
        ///     [XEditor.Tasks.Pre(typeof(MyPreHandler))]
        ///     [XEditor.Tasks.Post(typeof(MyPostHandler))]
        ///     public class MySequentialTask : XEditor.Tasks.Worker
        ///     {
        ///         // 任务实现
        ///     }
        /// 
        /// 3.2 批处理模式
        ///     命令行执行任务：
        ///     
        ///     # 执行单个任务
        ///     Unity.exe -batchmode -projectPath /path/to/project -runTasks -taskID &quot;MyTask&quot; -param1 &quot;value1&quot;
        ///     
        ///     # 执行多个任务并指定结果输出
        ///     Unity.exe -batchmode -projectPath /path/to/project -runTasks -taskID &quot;Task1&quot; -taskID &quot;Task2&quot; -runAsync -taskResults &quot;results.json&quot;
        ///     
        ///     命令行参数说明：
        ///     -runTasks: 启用批处理模式，必须参数
        ///     -taskID: 指定要执行的任务ID，可多次使用以执行多个任务
        ///     -runAsync: 设置任务为异步执行模式
        ///     -taskResults: 指定结果报告的输出文件路径
        ///     --参数名: 为任务提供参数
        /// 
        /// 3.3 参数优先级
        ///     参数读取遵循以下优先级：
        ///     1. 命令行参数（最高优先级）
        ///     2. 任务管理器界面设置的参数
        ///     3. XPrefs 中存储的持久化参数
        ///     4. 参数特性中定义的默认值（最低优先级）
        /// </code>
        /// 更多信息请参考模块文档。
        /// </remarks>
        public partial class Tasks
        {
            /// <summary>
            /// 正在执行的单例任务列表。
            /// </summary>
            internal static List<string> Singletons = new();

            /// <summary>
            /// 任务元数据列表。
            /// </summary>
            internal static readonly List<WorkerAttribute> Metas = new();

            /// <summary>
            /// 任务工作者字典，key 为任务元数据，value 为任务工作者实例。
            /// </summary>
            internal static readonly Dictionary<WorkerAttribute, IWorker> Workers = new();

            /// <summary>
            /// 任务参数特性，用于定义任务的参数信息。
            /// </summary>
            /// <remarks>
            /// 使用此特性可以为任务定义参数，支持：
            /// - 参数名称和描述
            /// - 默认值设置
            /// - 持久化配置
            /// - 平台限制
            /// </remarks>
            [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field, AllowMultiple = true)]
            public class Param : Attribute
            {
                /// <summary>
                /// 参数标识。
                /// </summary>
                public string ID { get; internal set; }

                /// <summary>
                /// 参数名称。
                /// </summary>
                public string Name { get; internal set; }

                /// <summary>
                /// 参数提示信息。
                /// </summary>
                public string Tooltip { get; internal set; }

                /// <summary>
                /// 参数默认值。
                /// </summary>
                public string Default { get; internal set; }

                /// <summary>
                /// 是否持久化参数。
                /// </summary>
                public bool Persist { get; internal set; }

                /// <summary>
                /// 参数适用的平台类型。
                /// </summary>
                public XEnv.PlatformType Platform { get; internal set; }

                /// <summary>
                /// 初始化任务参数。
                /// </summary>
                /// <param name="name">参数名称</param>
                /// <param name="tooltip">参数提示信息</param>
                /// <param name="defval">参数默认值</param>
                /// <param name="persist">是否持久化参数</param>
                /// <param name="platform">参数适用的平台类型</param>
                public Param(string name = "", string tooltip = "", string defval = "", bool persist = true, XEnv.PlatformType platform = XEnv.PlatformType.Unknown)
                {
                    Name = name;
                    Tooltip = tooltip;
                    Default = defval;
                    Persist = persist;
                    Platform = platform;
                }
            }

            /// <summary>
            /// 任务执行结果状态。
            /// </summary>
            public enum Result
            {
                /// <summary>
                /// 未知状态。
                /// </summary>
                Unknown,

                /// <summary>
                /// 执行成功。
                /// </summary>
                Succeeded,

                /// <summary>
                /// 执行失败。
                /// </summary>
                Failed,

                /// <summary>
                /// 已取消。
                /// </summary>
                Cancelled
            }

            /// <summary>
            /// 任务执行阶段，记录任务执行的各个步骤信息。
            /// </summary>
            public class Phase
            {
                /// <summary>
                /// 阶段名称。
                /// </summary>
                public string Name { get; set; }

                /// <summary>
                /// 错误信息。
                /// </summary>
                internal string error;

                /// <summary>
                /// 格式化的错误信息，包含阶段名称。
                /// </summary>
                public string Error { get => string.IsNullOrEmpty(error) ? error : $"{Name}: {error}"; set => error = value; }

                /// <summary>
                /// 阶段执行结果。
                /// </summary>
                public Result Result { get; set; }

                /// <summary>
                /// 阶段执行耗时（秒）。
                /// </summary>
                public int Elapsed { get; set; }
            }

            /// <summary>
            /// 任务执行报告，记录任务执行的完整信息。
            /// </summary>
            /// <remarks>
            /// 包含以下信息：
            /// - 任务参数和状态
            /// - 执行阶段和结果
            /// - 错误信息和耗时
            /// - 异步执行支持
            /// </remarks>
            public class Report : IAsyncResult
            {
                #region Sync/Async Task
                /// <summary>
                /// 异步任务对象。
                /// </summary>
                [XObject.Json.Exclude] public Task Task;

                /// <summary>
                /// 异步状态对象。
                /// </summary>
                [XObject.Json.Exclude] public object AsyncState => (Task as IAsyncResult).AsyncState;

                /// <summary>
                /// 异步等待句柄。
                /// </summary>
                [XObject.Json.Exclude] public WaitHandle AsyncWaitHandle => (Task as IAsyncResult).AsyncWaitHandle;

                /// <summary>
                /// 是否同步完成。
                /// </summary>
                [XObject.Json.Exclude] public bool CompletedSynchronously => (Task as IAsyncResult).CompletedSynchronously;

                /// <summary>
                /// 是否已完成。
                /// </summary>
                [XObject.Json.Exclude] public bool IsCompleted => (Task as IAsyncResult).IsCompleted;
                #endregion

                /// <summary>
                /// 任务参数字典。
                /// </summary>
                public Dictionary<string, string> Arguments { get; internal set; } = new();

                /// <summary>
                /// 当前执行阶段。
                /// </summary>
                [XObject.Json.Exclude]
                public Phase Current
                {
                    get
                    {
                        if (phases.Count == 0) return null;
                        return phases[^1];
                    }
                    set
                    {
                        if (Current != null)
                        {
                            if (Current.Result == Result.Unknown)
                            {
                                Current.Result = string.IsNullOrEmpty(Current.Error) ? Result.Succeeded : Result.Failed;
                            }
                        }
                        phases.Add(value);
                    }
                }

                /// <summary>
                /// 任务执行过程中的错误信息。
                /// </summary>
                [XObject.Json.Exclude]
                public string Error
                {
                    get
                    {
                        if (Result == Result.Failed)
                        {
                            var sb = new StringBuilder();
                            foreach (var phase in Phases) sb.AppendLine($"{phase.Name}: {phase.Error}");
                            return sb.ToString();
                        }
                        return string.Empty;
                    }
                    set { Current.Error = value; }
                }

                /// <summary>
                /// 任务执行结果。
                /// </summary>
                public Result Result
                {
                    get
                    {
                        foreach (var phase in Phases)
                        {
                            if (phase.Result == Result.Unknown)
                            {
                                phase.Result = string.IsNullOrEmpty(phase.Error) ? Result.Succeeded : Result.Failed;
                            }
                            if (phase.Result != Result.Succeeded) return Result.Failed;
                        }
                        return Result.Succeeded;
                    }
                    set { Current.Result = value; }
                }

                /// <summary>
                /// 任务总执行时间（秒）。
                /// </summary>
                public int Elapsed
                {
                    get
                    {
                        var total = 0;
                        foreach (var phase in Phases) total += phase.Elapsed;
                        return total;
                    }
                }

                /// <summary>
                /// 任务执行阶段列表。
                /// </summary>
                [XObject.Json.Exclude] internal readonly List<Phase> phases = new();
                public List<Phase> Phases { get => phases; }

                /// <summary>
                /// 任务额外数据信息。
                /// </summary>
                [XObject.Json.Exclude] public object Extras;
            }

            /// <summary>
            /// 任务预处理特性，用于定义任务执行前的处理器。
            /// </summary>
            /// <remarks>
            /// 预处理器在任务执行前被调用，可以：
            /// - 验证执行环境
            /// - 准备必要资源
            /// - 初始化任务状态
            /// </remarks>
            [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
            public class Pre : Attribute
            {
                /// <summary>
                /// 预处理器类型。
                /// </summary>
                public Type Handler;

                /// <summary>
                /// 初始化预处理特性。
                /// </summary>
                /// <param name="handler">预处理器类型</param>
                public Pre(Type handler) { Handler = handler ?? throw new ArgumentNullException("handler"); }
            }

            /// <summary>
            /// 任务后处理特性，用于定义任务执行后的处理器。
            /// </summary>
            /// <remarks>
            /// 后处理器在任务执行后被调用，可以：
            /// - 清理资源
            /// - 更新状态
            /// - 触发后续操作
            /// </remarks>
            [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
            public class Post : Attribute
            {
                /// <summary>
                /// 后处理器类型。
                /// </summary>
                public Type Handler;

                /// <summary>
                /// 初始化后处理特性。
                /// </summary>
                /// <param name="handler">后处理器类型</param>
                public Post(Type handler) { Handler = handler ?? throw new ArgumentNullException("handler"); }
            }

            /// <summary>
            /// 任务工作者接口，定义任务的基本行为。
            /// </summary>
            /// <remarks>
            /// 实现此接口的类需要提供：
            /// - 任务标识和配置
            /// - 执行流程实现
            /// - 状态管理方法
            /// </remarks>
            public interface IWorker
            {
                /// <summary>
                /// 任务标识。
                /// </summary>
                string ID { get; set; }

                /// <summary>
                /// 是否为单例任务。
                /// </summary>
                bool Singleton { get; set; }

                /// <summary>
                /// 是否异步执行。
                /// </summary>
                bool Runasync { get; set; }

                /// <summary>
                /// 是否在批处理模式下执行。
                /// </summary>
                bool Batchmode { get; set; }

                /// <summary>
                /// 任务优先级。
                /// </summary>
                int Priority { get; set; }

                /// <summary>
                /// 任务预处理。
                /// </summary>
                /// <param name="report">任务报告</param>
                void Preprocess(Report report);

                /// <summary>
                /// 任务处理。
                /// </summary>
                /// <param name="report">任务报告</param>
                void Process(Report report);

                /// <summary>
                /// 任务后处理。
                /// </summary>
                /// <param name="report">任务报告</param>
                void Postprocess(Report report);
            }

            /// <summary>
            /// 任务工作者基类，提供任务基本行为的默认实现。
            /// </summary>
            /// <remarks>
            /// 继承此类可以：
            /// - 快速创建新任务
            /// - 复用通用实现
            /// - 专注业务逻辑
            /// </remarks>
            public class Worker : IWorker
            {
                /// <summary>
                /// 任务标识。
                /// </summary>
                public virtual string ID { get; set; }

                /// <summary>
                /// 是否为单例任务，默认为 true。
                /// </summary>
                public virtual bool Singleton { get; set; } = true;

                /// <summary>
                /// 是否异步执行，默认为 false。
                /// </summary>
                public virtual bool Runasync { get; set; } = false;

                /// <summary>
                /// 是否在批处理模式下执行，默认为 false。
                /// </summary>
                public virtual bool Batchmode { get; set; } = false;

                /// <summary>
                /// 任务优先级，默认为 0。
                /// </summary>
                public virtual int Priority { get; set; } = 0;

                /// <summary>
                /// 任务预处理，默认为空实现。
                /// </summary>
                /// <param name="report">任务报告</param>
                public virtual void Preprocess(Report report) { }

                /// <summary>
                /// 任务处理，默认为空实现。
                /// </summary>
                /// <param name="report">任务报告</param>
                public virtual void Process(Report report) { }

                /// <summary>
                /// 任务后处理，默认为空实现。
                /// </summary>
                /// <param name="report">任务报告</param>
                public virtual void Postprocess(Report report) { }
            }

            /// <summary>
            /// 任务特性，用于定义任务的基本信息。
            /// </summary>
            /// <remarks>
            /// 使用此特性可以定义：
            /// - 任务的名称和分组
            /// - 执行方式和优先级
            /// - 平台限制和参数配置
            /// </remarks>
            [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
            public class WorkerAttribute : Attribute
            {
                /// <summary>
                /// 任务名称。
                /// </summary>
                public string Name;

                /// <summary>
                /// 任务组名称。
                /// </summary>
                public string Group;

                /// <summary>
                /// 任务提示信息。
                /// </summary>
                public string Tooltip;

                /// <summary>
                /// 任务优先级。
                /// </summary>
                public int Priority;

                /// <summary>
                /// 是否为单例任务。
                /// </summary>
                public bool Singleton;

                /// <summary>
                /// 是否异步执行。
                /// </summary>
                public bool Runasync;

                /// <summary>
                /// 任务适用的平台类型。
                /// </summary>
                public XEnv.PlatformType Platform;

                /// <summary>
                /// 任务工作者类型。
                /// </summary>
                public Type Worker;

                /// <summary>
                /// 任务参数列表。
                /// </summary>
                public List<Param> Params;

                /// <summary>
                /// 是否单元测试任务。
                /// </summary>
                internal bool Test;

                /// <summary>
                /// 初始化任务特性。
                /// </summary>
                /// <param name="name">任务名称</param>
                /// <param name="group">任务组名称</param>
                /// <param name="tooltip">任务提示信息</param>
                /// <param name="priority">任务优先级</param>
                /// <param name="singleton">是否为单例任务</param>
                /// <param name="runasync">是否异步执行</param>
                /// <param name="platform">任务适用的平台类型</param>
                /// <param name="worker">任务工作者类型</param>
                public WorkerAttribute(string name, string group = "Unknown", string tooltip = "", int priority = 0, bool singleton = true,
                    bool runasync = false, XEnv.PlatformType platform = XEnv.PlatformType.Unknown, Type worker = null, bool test = false)
                {
                    if (string.IsNullOrEmpty(name)) throw new ArgumentNullException("name");
                    Name = name;
                    Group = group;
                    Tooltip = tooltip;
                    Priority = priority;
                    Singleton = singleton;
                    Runasync = runasync;
                    Platform = platform;
                    Worker = worker;
                    Test = test;
                }
            }
        }
    }
}
