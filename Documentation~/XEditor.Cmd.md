# XEditor.Cmd

[![Version](https://img.shields.io/npm/v/org.eframework.u3d.edit)](https://www.npmjs.com/package/org.eframework.u3d.edit)
[![Downloads](https://img.shields.io/npm/dm/org.eframework.u3d.edit)](https://www.npmjs.com/package/org.eframework.u3d.edit)
[![DeepWiki](https://img.shields.io/badge/DeepWiki-Explore-blue)](https://deepwiki.com/eframework-org/U3D.EDIT)

XEditor.Cmd 是一个用于在编辑器中执行命令行操作的工具模块，提供了命令查找和执行功能，支持跨平台操作。

## 功能特性

- 支持 Windows/Linux/macOS 跨平台：自动适配不同系统的命令路径
- 提供命令路径查找：支持在系统 PATH、环境变量和自定义路径中查找命令
- 实现异步命令执行：支持命令执行进度显示和取消操作
- 支持 UTF-8 编码：自动处理命令输出的编码问题

## 使用手册

### 1. 命令查找

#### 1.1 查找系统命令
```csharp
// 在系统PATH中查找git命令
string gitPath = XEditor.Cmd.Find("git");
if (!string.IsNullOrEmpty(gitPath))
{
    Debug.Log($"找到git命令：{gitPath}");
}
```

#### 1.2 查找自定义路径命令
```csharp
// 在指定路径中查找命令
string customPath = XEditor.Cmd.Find("custom.exe", "C:/Tools", "D:/Apps");
```

### 2. 命令执行

#### 2.1 基本执行
```csharp
// 执行git status命令
var result = await XEditor.Cmd.Run("git", XEnv.ProjectPath, false, "status");
if (result.Code == 0)
{
    Debug.Log($"命令输出：{result.Data}");
}
else
{
    Debug.LogError($"命令错误：{result.Error}");
}
```

#### 2.2 静默执行
```csharp
// 静默执行命令（不显示进度条）
var result = await XEditor.Cmd.Run("git", XEnv.ProjectPath, true, "pull");
```

## 常见问题

### 1. macOS 下找不到命令
- 现象：在 macOS 系统下某些命令无法被正确找到
- 原因：Unity/Mono 在启动时可能不会完全继承系统的环境变量，特别是 `/usr/local/bin` 等目录
- 解决：模块会自动添加以下路径到 PATH 环境变量：
  - `/usr/local/bin`：常用命令目录
  - `/usr/local/share/dotnet`：.NET SDK 目录

### 2. 命令执行被阻塞
命令执行支持取消操作，在非批处理模式下可以通过进度条界面取消执行。如果命令执行时间过长，建议使用静默模式执行。

更多问题，请查阅[问题反馈](../CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](../CHANGELOG.md)
- [贡献指南](../CONTRIBUTING.md)
- [许可证](../LICENSE.md)
