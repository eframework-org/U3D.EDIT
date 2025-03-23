# XEditor.Icons

[![Version](https://img.shields.io/npm/v/org.eframework.u3d.edit)](https://www.npmjs.com/package/org.eframework.u3d.edit)
[![Downloads](https://img.shields.io/npm/dm/org.eframework.u3d.edit)](https://www.npmjs.com/package/org.eframework.u3d.edit)  

XEditor.Icons 提供了 Unity 编辑器内置图标的预览和导出工具，支持图标搜索和批量导出，方便UI开发和编辑器扩展。

## 功能特性

- 支持预览所有Unity内置编辑器图标：可视化浏览所有可用图标资源
- 提供图标搜索功能：根据名称快速筛选所需图标
- 支持图标导出：将所选图标或全部图标导出为PNG格式
- 提供黑白背景切换：适应不同场景下的图标预览需求
- 支持图标尺寸调整：灵活调整图标显示大小

## 使用手册

### 1. 打开图标浏览器

#### 1.1 通过菜单打开
在Unity编辑器中选择 `Assets > Editor Icons` 菜单项打开图标浏览器窗口。

```csharp
// 也可以通过代码打开
XEditor.Icons.Open();
```

### 2. 浏览和搜索图标

#### 2.1 图标搜索
在窗口顶部的搜索框中输入关键字，可以根据图标名称进行筛选。

#### 2.2 调整视图
- 使用顶部的切换按钮在大图标和小图标视图之间切换
- 使用滑块调整图标显示尺寸
- 点击切换按钮在黑/白背景之间切换，以便在不同背景下预览图标效果

### 3. 导出图标

#### 3.1 导出单个图标
点击图标右键可以将其导出为PNG格式的文件。

#### 3.2 批量导出图标
点击窗口顶部的"Export All"按钮可以将所有图标批量导出为PNG文件。

## 常见问题

### 1. 为什么有些图标显示为空白?
某些内置图标在特定Unity版本中可能不可用，这些图标将显示为空白。

### 2. 导出的图标在哪里?
导出的图标默认保存在Unity项目的根目录下的"EditorIcons"文件夹中。

更多问题，请查阅[问题反馈](../CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](../CHANGELOG.md)
- [贡献指南](../CONTRIBUTING.md)
- [许可证](../LICENSE.md)
