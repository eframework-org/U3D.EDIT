# XEditor.Utility

[![Version](https://img.shields.io/npm/v/org.eframework.u3d.edit)](https://www.npmjs.com/package/org.eframework.u3d.edit)
[![Downloads](https://img.shields.io/npm/dm/org.eframework.u3d.edit)](https://www.npmjs.com/package/org.eframework.u3d.edit)
[![DeepWiki](https://img.shields.io/badge/DeepWiki-Explore-blue)](https://deepwiki.com/eframework-org/U3D.EDIT)

XEditor.Utility 提供了一系列编辑器实用工具函数，包括资源收集、依赖分析、文件操作等功能。

## 功能特性

- 资源接口：递归扫描目录并收集满足条件的文件，分析资源文件之间的依赖关系
- 文件操作：提供压缩/解压文件，文件浏览等功能
- 消息通知：在 Console 窗口显示提示 Toast 信息

## 使用手册

### 1. 消息通知

#### 1.1 显示Toast消息
在 Console 窗口中显示的消息通知。

```csharp
// 显示消息，显示时间4秒
XEditor.Utility.ShowToast("操作成功完成");

// 自定义显示时间并焦点
XEditor.Utility.ShowToast("重要消息", true, 8.0f);
```

### 2. 文件操作

#### 2.1 收集文件
递归扫描目录并收集满足条件的文件。

```csharp
List<string> files = new List<string>();
XEditor.Utility.CollectFiles("Assets/MyFolder", files, ".meta", ".tmp");
foreach (var file in files)
{
    Debug.Log($"找到文件: {file}");
}
```

#### 2.2 压缩文件
将指定目录压缩为zip文件。

```csharp
bool success = XEditor.Utility.ZipDirectory("Assets/MyFolder", "Output/archive.zip");
if (success)
{
    Debug.Log("压缩成功");
}
```

### 3. 资源操作

#### 3.1 收集资源
收集Unity资源文件。

```csharp
List<string> assets = new List<string>();
XEditor.Utility.CollectAssets("Assets/MyFolder", assets, ".meta");
```

#### 3.2 分析资源依赖
分析资源文件之间的依赖关系。

```csharp
List<string> sourceAssets = new List<string> { "Assets/MyPrefab.prefab" };
Dictionary<string, List<string>> dependencies = XEditor.Utility.CollectDependency(sourceAssets);
foreach (var pair in dependencies)
{
    Debug.Log($"资源 {pair.Key} 依赖于:");
    foreach (var dep in pair.Value)
    {
        Debug.Log($"  - {dep}");
    }
}
```

### 4. 编辑器扩展

#### 4.1 获取选中资源
获取当前在Project窗口中选中的资源。

```csharp
List<string> selectedAssets = XEditor.Utility.GetSelectedAssets();
```

#### 4.2 获取选中路径
获取当前在Project窗口中选中的路径。

```csharp
string path = XEditor.Utility.GetSelectedPath();
```

#### 4.3 在文件浏览器中显示
在系统文件浏览器中显示指定路径。

```csharp
XEditor.Utility.ShowInExplorer("Assets/MyFolder");
```

## 常见问题

### 1. Toast消息不显示或位置不正确
Toast消息依赖于 Console 窗口，需确保窗口是打开的状态。

### 2. 如何处理大量文件收集的性能问题?
对于大型项目，可以指定更具体的目录和排除规则，减少需要扫描的文件数量。

更多问题，请查阅[问题反馈](../CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](../CHANGELOG.md)
- [贡献指南](../CONTRIBUTING.md)
- [许可证](../LICENSE.md)
