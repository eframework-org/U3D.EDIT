// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using EFramework.Utility;

namespace EFramework.Editor
{
    public partial class XEditor
    {
        /// <summary>
        /// XEditor.Oss 提供了基于 MinIO 的对象存储服务集成，支持资源上传和下载，简化了云存储操作流程，适用于资源分发和远程部署场景。
        /// </summary>
        /// <remarks>
        /// <code>
        /// 功能特性
        /// - 支持主流云存储平台：基于任务系统实现的云存储接口，易于扩展
        /// - 提供资源上传功能：支持批量资源上传，适用于构建产物部署
        /// - 实现资源下载功能：支持资源下载和验证，适用于远程资源获取
        /// - 集成任务系统：自动处理上传下载任务，支持进度显示和错误处理
        /// 
        /// 使用手册
        /// 1. 基本配置
        /// 
        /// 1.1 创建任务
        ///     // 创建并配置 OSS 任务实例
        ///     var oss = new XEditor.Oss {
        ///         ID = "my-upload",                    // 任务标识
        ///         Host = "http://localhost:9000",      // 存储服务地址
        ///         Bucket = "default",                  // 存储桶名称
        ///         Access = "admin",                    // 访问密钥 ID
        ///         Secret = "adminadmin"                // 访问密钥 Secret
        ///     };
        /// 
        /// 2. 文件操作
        /// 
        /// 2.1 上传文件
        ///     // 配置上传路径
        ///     oss.Local = "/path/to/local/file";      // 本地文件路径
        ///     oss.Remote = "path/in/bucket";          // 远程存储路径
        ///     
        ///     // 执行上传任务
        ///     var report = XEditor.Tasks.Execute(oss);
        /// 
        /// 2.2 上传目录
        ///     // 配置目录路径
        ///     oss.Local = "/path/to/local/directory"; // 本地目录路径
        ///     oss.Remote = "path/in/bucket";          // 远程存储路径
        ///     
        ///     // 执行上传任务
        ///     var report = XEditor.Tasks.Execute(oss);
        /// 
        /// 2.3 路径处理
        ///     // 1. 基本路径
        ///     oss.Remote = "path/in/bucket";          // 基本路径格式
        ///     
        ///     // 2. 目录上传时的路径处理
        ///     oss.Local = "/path/to/MyFolder";        // 本地目录
        ///     oss.Remote = "remote/MyFolder";         // 如果远程路径末尾包含目录名，会自动去除重复
        ///                                            // 实际存储路径为：remote/MyFolder/*
        ///     
        ///     // 3. 路径规范化
        ///     oss.Remote = "path/with/trailing/";     // 末尾斜杠会被自动移除
        ///     oss.Remote = "path\\with\\backslash";   // 反斜杠会被转换为正斜杠
        /// 
        /// 2.4 检查结果
        ///     if (report.Result == XEditor.Tasks.Result.Succeeded) {
        ///         Debug.Log("上传成功");
        ///     } else {
        ///         Debug.LogError($"上传失败: {report.Error}");
        ///     }
        /// 
        /// 3. 执行流程
        /// 
        /// 3.1 预处理阶段
        /// - 根据平台确定客户端可执行文件名
        /// - 检查环境变量中是否存在客户端
        /// - 如果不存在则自动下载
        /// - 设置客户端配置别名
        /// 
        /// 3.2 处理阶段
        /// - 验证远程路径是否有效
        /// - 验证本地路径是否存在
        /// - 检查目录是否为空
        /// - 处理路径格式：
        ///   - 移除路径末尾的斜杠
        ///   - 处理目录名重复问题
        ///   - 规范化路径分隔符
        /// - 执行上传命令
        /// 
        /// 3.3 后处理阶段
        /// - 删除临时目录
        /// - 确保不留下任何临时文件
        /// </code>
        /// 更多信息请参考模块文档。
        /// </remarks>
        public class Oss : Tasks.Worker
        {
            /// <summary>
            /// 对象存储服务的主机地址。
            /// </summary>
            /// <remarks>
            /// 格式：http(s)://host:port，例如：http://localhost:9000
            /// </remarks>
            public string Host;

            /// <summary>
            /// 对象存储服务的存储桶名称。
            /// </summary>
            /// <remarks>
            /// 存储桶必须已存在且具有写入权限。
            /// </remarks>
            public string Bucket;

            /// <summary>
            /// 对象存储服务的访问密钥 ID。
            /// </summary>
            /// <remarks>
            /// 用于认证的 Access Key，通常由服务提供方提供。
            /// </remarks>
            public string Access;

            /// <summary>
            /// 对象存储服务的访问密钥 Secret。
            /// </summary>
            /// <remarks>
            /// 用于认证的 Secret Key，需要妥善保管，避免泄露。
            /// </remarks>
            public string Secret;

            /// <summary>
            /// MinIO 客户端可执行文件路径。
            /// </summary>
            /// <remarks>
            /// 如果未指定，将自动下载对应平台的客户端。
            /// Windows 平台为 mc.exe，其他平台为 mc。
            /// </remarks>
            public virtual string Bin { get; set; }

            /// <summary>
            /// MinIO 客户端配置的别名。
            /// </summary>
            /// <remarks>
            /// 用于在多个配置之间区分不同的连接。
            /// </remarks>
            private string alias;

            /// <summary>
            /// 获取或设置 MinIO 客户端配置的别名。
            /// </summary>
            /// <remarks>
            /// 默认使用当前类型的全名作为别名：
            /// 1. 替换 + 为 .
            /// 2. 替换 . 为 -
            /// 例如：EFramework.Editor.XEditor-Oss
            /// </remarks>
            public virtual string Alias
            {
                get
                {
                    if (string.IsNullOrEmpty(alias))
                    {
                        alias = GetType().FullName.Replace("+", ".").Replace(".", "-");
                    }
                    return alias;
                }
                set => alias = value;
            }

            /// <summary>
            /// 临时目录路径。
            /// </summary>
            /// <remarks>
            /// 用于存储 MinIO 客户端配置和临时文件。
            /// </remarks>
            private string temp;

            /// <summary>
            /// 获取或设置临时目录路径。
            /// </summary>
            /// <remarks>
            /// 默认在项目 Temp 目录下创建以时间戳命名的临时目录。
            /// 目录格式：ProjectPath/Temp/Oss-{timestamp}
            /// 目录会在任务完成后自动清理。
            /// </remarks>
            public virtual string Temp
            {
                get
                {
                    if (string.IsNullOrEmpty(temp))
                    {
                        temp = XFile.PathJoin(XEnv.ProjectPath, "Temp", $"Oss-{XTime.GetMillisecond()}");
                        if (XFile.HasDirectory(temp)) XFile.DeleteDirectory(temp);
                        XFile.CreateDirectory(temp);
                    }
                    return temp;
                }
                set => temp = value;
            }

            /// <summary>
            /// 本地文件或目录路径。
            /// </summary>
            /// <remarks>
            /// 支持上传单个文件或整个目录。
            /// 路径必须存在且具有读取权限。
            /// </remarks>
            public virtual string Local { get; set; }

            /// <summary>
            /// 远程存储路径。
            /// </summary>
            /// <remarks>
            /// 指定文件在存储桶中的存储路径。
            /// 如果路径不存在会自动创建。
            /// </remarks>
            public virtual string Remote { get; set; }

            /// <summary>
            /// 预处理阶段。
            /// </summary>
            /// <param name="report">任务执行报告，用于记录处理结果和错误信息</param>
            /// <remarks>
            /// 执行步骤：
            /// 1. 根据平台确定客户端可执行文件名
            /// 2. 检查环境变量中是否存在客户端
            /// 3. 如果不存在则自动下载
            /// 4. 设置客户端配置别名
            /// </remarks>
            public override void Preprocess(Tasks.Report report)
            {
                // 根据平台确定MinIO客户端可执行文件名
                var name = Application.platform == RuntimePlatform.WindowsEditor ? "mc.exe" : "mc";

                // 尝试从环境变量中查找MinIO客户端
                Bin = Cmd.Find(name);

                // 如果未找到，则设为项目Library目录下的路径
                if (string.IsNullOrEmpty(Bin) || XFile.HasFile(Bin) == false) Bin = XFile.PathJoin(XEnv.ProjectPath, "Library", name);

                // 如果MinIO客户端不存在，则尝试下载
                if (!XFile.HasFile(Bin))
                {
                    var url = "";
                    var isCN = System.Globalization.RegionInfo.CurrentRegion.Name == "CN" ||
                                  System.Threading.Thread.CurrentThread.CurrentCulture.Name.StartsWith("zh-CN");
                    var baseUrl = isCN ? "https://dl.minio.org.cn/client/mc/release/" : "https://dl.min.io/client/mc/release/";

                    // 根据平台选择下载地址
                    if (Application.platform == RuntimePlatform.WindowsEditor)
                        url = baseUrl + "windows-amd64/mc.exe";
                    else if (Application.platform == RuntimePlatform.LinuxEditor)
                        url = baseUrl + "linux-amd64/mc";
                    else if (Application.platform == RuntimePlatform.OSXEditor)
                        url = baseUrl + "darwin-amd64/mc";

                    XLog.Debug($"XEditor.OSS.Process: start to download minio client from <a href=\"file:///{url}\">{url}</a>");

                    if (!string.IsNullOrEmpty(url))
                    {
                        // 在主线程中执行下载操作
                        XLoom.RunInMain(() =>
                        {
                            using var req = UnityWebRequest.Get(url);
                            req.SendWebRequest();
                            while (!req.isDone)
                            {
                                // 显示下载进度
                                if (EditorUtility.DisplayCancelableProgressBar("Download MinIO",
                                    $"Downloading MinIO Client...({XString.ToSize((long)req.downloadedBytes)})", req.downloadProgress))
                                {
                                    XLog.Error($"XEditor.OSS.Process: download minio client from <a href=\"file:///{url}\">{url}</a> has been canceled.");
                                    break;
                                }
                            }
                            if (req.responseCode != 200 || !string.IsNullOrEmpty(req.error))
                            {
                                XLog.Error($"XEditor.OSS.Process: download minio client from <a href=\"file:///{url}\">{url}</a> failed({req.responseCode}): {req.error}");
                            }
                            else if (req.isDone)
                            {
                                // 保存下载的客户端文件
                                XFile.SaveFile(Bin, req.downloadHandler.data);
#if !UNITY_EDITOR_WIN
                                // 非Windows平台设置可执行权限
                                Cmd.Run(bin: "chmod", args: new string[] { "755", Bin }).Wait();
#endif
                                XLog.Debug($"XEditor.OSS.Process: download minio client from <a href=\"file:///{url}\">{url}</a> succeeded.");
                            }
                            EditorUtility.ClearProgressBar();
                        }).Wait();
                    }
                }

                // 如果MinIO客户端仍不存在，则报错
                if (XFile.HasFile(Bin) == false) report.Error = "MinIO Client was not found.";
                else
                {
                    // 设置MinIO客户端别名配置
                    var task = Cmd.Run(bin: Bin, args: new string[] { "alias", "set", Alias, Host, Access, Secret, "--config-dir", Temp });
                    task.Wait();
                    if (task.Result.Code != 0)
                    {
                        report.Error = $"Run set alias with error: {task.Result.Error}";
                    }
                }
            }

            /// <summary>
            /// 处理阶段。
            /// </summary>
            /// <param name="report">任务执行报告，用于记录处理结果和错误信息</param>
            /// <remarks>
            /// 执行步骤：
            /// 1. 验证远程路径是否有效
            /// 2. 验证本地路径是否存在
            /// 3. 检查目录是否为空
            /// 4. 处理路径格式
            /// 5. 执行上传命令
            /// </remarks>
            public override void Process(Tasks.Report report)
            {
                // 验证远程路径
                if (string.IsNullOrEmpty(Remote))
                {
                    report.Error = "Remote uri is null.";
                    return;
                }

                // 验证本地路径是否存在
                if (!XFile.HasDirectory(Local) && !XFile.HasFile(Local))
                {
                    report.Error = "Local uri doesn't existed.";
                    return;
                }

                // 检查本地目录是否为空，若为空则无需上传
                if (XFile.HasDirectory(Local) && Directory.GetFiles(Local).Length == 0)
                {
                    XLog.Debug("XEditor.Oss.Process: local uri was empty, no need to cp.");
                    return;
                }
                else
                {
                    // 处理本地和远程路径，确保格式正确
                    var local = Local;
                    if (local.EndsWith("/")) local = local[..^1];
                    var remote = Remote;
                    if (remote.EndsWith("/")) remote = remote[..^1];

                    // 处理目录名重复问题
                    var dir = Path.GetFileName(local);
                    if (remote.EndsWith(dir)) remote = remote[..^dir.Length];

                    // 执行上传命令
                    var task = Cmd.Run(bin: Bin, args: new string[] { "cp", "--recursive", $"\"{local}\"", $"\"{Alias}/{Bucket}/{remote}\"" });
                    task.Wait();
                    if (task.Result.Code != 0)
                    {
                        report.Error = $"Run cp with error: {task.Result.Error}";
                    }
                }
            }

            /// <summary>
            /// 后处理阶段。
            /// </summary>
            /// <param name="report">任务执行报告，用于记录处理结果和错误信息</param>
            /// <remarks>
            /// 执行步骤：
            /// 1. 删除临时目录
            /// 2. 确保不留下任何临时文件
            /// </remarks>
            public override void Postprocess(Tasks.Report report)
            {
                // 删除临时目录
                if (XFile.HasDirectory(Temp)) XFile.DeleteDirectory(Temp);
            }
        }
    }
}
