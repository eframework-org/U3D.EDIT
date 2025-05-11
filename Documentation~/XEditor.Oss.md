# XEditor.Oss

[![Version](https://img.shields.io/npm/v/org.eframework.u3d.edit)](https://www.npmjs.com/package/org.eframework.u3d.edit)
[![Downloads](https://img.shields.io/npm/dm/org.eframework.u3d.edit)](https://www.npmjs.com/package/org.eframework.u3d.edit)  
[![DeepWiki](https://img.shields.io/badge/DeepWiki-Explore-blue)](https://deepwiki.com/eframework-org/U3D.EDIT)

XEditor.Oss 提供了基于 MinIO 的对象存储服务集成，支持资源上传和下载，简化了云存储操作流程，适用于资源分发和远程部署场景。

## 功能特性

- 支持主流云存储平台：基于任务系统实现的云存储接口，易于扩展
- 提供资源上传功能：支持批量资源上传，适用于构建产物部署
- 实现资源下载功能：支持资源下载和验证，适用于远程资源获取
- 集成任务系统：自动处理上传下载任务，支持进度显示和错误处理

## 使用手册

### 1. 基本配置

#### 1.1 创建任务
```csharp
// 创建并配置 OSS 任务实例
var oss = new XEditor.Oss {
    ID = "my-upload",                    // 任务标识
    Host = "http://localhost:9000",      // 存储服务地址
    Bucket = "default",                  // 存储桶名称
    Access = "admin",                    // 访问密钥 ID
    Secret = "adminadmin"                // 访问密钥 Secret
};
```

### 2. 文件操作

#### 2.1 上传文件
```csharp
// 配置上传路径
oss.Local = "/path/to/local/file";      // 本地文件路径
oss.Remote = "path/in/bucket";          // 远程存储路径

// 执行上传任务
var report = XEditor.Tasks.Execute(oss);
```

#### 2.2 上传目录
```csharp
// 配置目录路径
oss.Local = "/path/to/local/directory"; // 本地目录路径
oss.Remote = "path/in/bucket";          // 远程存储路径

// 执行上传任务
var report = XEditor.Tasks.Execute(oss);
```

#### 2.3 路径处理
```csharp
// 1. 基本路径
oss.Remote = "path/in/bucket";          // 基本路径格式

// 2. 目录上传时的路径处理
oss.Local = "/path/to/MyFolder";        // 本地目录
oss.Remote = "remote/MyFolder";         // 如果远程路径末尾包含目录名，会自动去除重复
                                       // 实际存储路径为：remote/MyFolder/*

// 3. 路径规范化
oss.Remote = "path/with/trailing/";     // 末尾斜杠会被自动移除
oss.Remote = "path\\with\\backslash";   // 反斜杠会被转换为正斜杠
```

#### 2.4 检查结果
```csharp
if (report.Result == XEditor.Tasks.Result.Succeeded) {
    Debug.Log("上传成功");
} else {
    Debug.LogError($"上传失败: {report.Error}");
}
```

### 3. 执行流程

#### 3.1 预处理阶段
- 根据平台确定客户端可执行文件名
- 检查环境变量中是否存在客户端
- 如果不存在则自动下载
- 设置客户端配置别名

#### 3.2 处理阶段
- 验证远程路径是否有效
- 验证本地路径是否存在
- 检查目录是否为空
- 处理路径格式：
  - 移除路径末尾的斜杠
  - 处理目录名重复问题
  - 规范化路径分隔符
- 执行上传命令

#### 3.3 后处理阶段
- 删除临时目录
- 确保不留下任何临时文件

## 常见问题

### 1. MinIO 客户端下载失败
- 现象：无法自动下载 MinIO 客户端
- 原因：网络连接问题或权限不足
- 解决：
  1. 检查网络连接是否正常
  2. 确保有足够的磁盘权限
  3. 尝试手动下载并放置到 Library 目录

### 2. 上传失败
- 现象：文件上传返回错误
- 原因：认证信息错误或存储桶配置问题
- 解决：
  1. 验证 Access 和 Secret 是否正确
  2. 确认存储桶是否存在且有写入权限
  3. 检查网络连接是否稳定

### 3. 目录上传不完整
- 现象：部分文件未能成功上传
- 原因：文件权限或网络不稳定
- 解决：
  1. 检查文件访问权限
  2. 确保网络稳定
  3. 尝试分批上传大目录

### 4. 远程路径问题
- 现象：文件上传位置不符合预期
- 原因：路径格式问题或目录名重复
- 解决：
  1. 确保使用正斜杠（/）作为路径分隔符
  2. 注意远程路径中是否已包含目标目录名
  3. 检查路径中是否有多余的斜杠

更多问题，请查阅[问题反馈](../CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](../CHANGELOG.md)
- [贡献指南](../CONTRIBUTING.md)
- [许可证](../LICENSE.md)
