# EFramework Editor for Unity

[![Version](https://img.shields.io/npm/v/org.eframework.u3d.edit)](https://www.npmjs.com/package/org.eframework.u3d.edit)
[![Downloads](https://img.shields.io/npm/dm/org.eframework.u3d.edit)](https://www.npmjs.com/package/org.eframework.u3d.edit)

EFramework Editor for Unity 是一个编辑器扩展工具库，提供了任务系统、配置管理、事件系统、快速构建等功能，用于提高开发效率。

## 功能特性

- [XEditor.Binary](Documentation~/XEditor.Binary.md) 提供了一套完整的构建流程管理系统，简化了多平台项目的构建配置，支持自动化和构建后处理
- [XEditor.Cmd](Documentation~/XEditor.Cmd.md) 是一个用于在编辑器中执行命令行操作的工具模块，提供了命令查找和执行功能，支持跨平台操作
- [XEditor.Const](Documentation~/XEditor.Const.md) 提供了编辑器常量配置的管理功能，支持通过特性标记的方式自定义常量值
- [XEditor.Event](Documentation~/XEditor.Event.md) 提供了基于接口的编辑器事件系统，支持自定义事件回调和通知，内置了一些常用的事件，如编辑器初始化、更新和退出等
- [XEditor.Icons](Documentation~/XEditor.Icons.md) 提供了 Unity 编辑器内置图标的预览和导出工具，支持图标搜索和批量导出，方便UI开发和编辑器扩展
- [XEditor.Npm](Documentation~/XEditor.Npm.md) 提供了在 Unity 编辑器中调用和执行 NPM 脚本的工具，支持异步执行、参数传递和错误处理
- [XEditor.Oss](Documentation~/XEditor.Oss.md) 提供了基于 MinIO 的对象存储服务集成，支持资源上传和下载，简化了云存储操作流程，适用于资源分发和远程部署场景
- [XEditor.Prefs](Documentation~/XEditor.Prefs.md) 提供了编辑器首选项的加载和应用功能，支持自动收集和组织首选项面板、配置持久化和构建预处理
- [XEditor.Tasks](Documentation~/XEditor.Tasks.md) 提供了一个编辑器任务调度系统，基于 C# Attribute 或 Npm Scripts 定义任务，支持可视化交互、命令行参数、脚本调用等方式同步/异步执行任务
- [XEditor.Title](Documentation~/XEditor.Title.md) 拓展了编辑器标题的功能，支持在标题中显示首选项信息和 Git 版本控制信息，方便开发者快速识别当前工作环境和项目状态
- [XEditor.Utility](Documentation~/XEditor.Utility.md) 提供了一系列编辑器实用工具函数，包括资源收集、依赖分析、文件操作等功能

## 常见问题

更多问题，请查阅[问题反馈](CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](CHANGELOG.md)
- [贡献指南](CONTRIBUTING.md)
- [许可证](LICENSE.md) 