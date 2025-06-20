# XEditor.Title

[![Version](https://img.shields.io/npm/v/org.eframework.u3d.edit)](https://www.npmjs.com/package/org.eframework.u3d.edit)
[![Downloads](https://img.shields.io/npm/dm/org.eframework.u3d.edit)](https://www.npmjs.com/package/org.eframework.u3d.edit)
[![DeepWiki](https://img.shields.io/badge/DeepWiki-Explore-blue)](https://deepwiki.com/eframework-org/U3D.EDIT)

XEditor.Title 拓展了编辑器标题的功能，支持在标题中显示首选项信息和 Git 版本控制信息，方便开发者快速识别当前工作环境和项目状态。

## 功能特性

- 集成首选项信息显示：作者、渠道、版本、模式和日志级别
- 集成 Git 信息显示：自动更新并显示当前工作的 Git 版本控制信息

## 使用手册

### 1. 基本功能

#### 1.1 标题信息格式
首选项信息格式：`[Preferences<是否修改>: <作者>/<渠道>/<版本>/<模式>/<日志级别>]，示例：[Preferences*: Admin/Default/1.0/Test/Debug]`

Git 信息格式：`[Git<是否存在未提交的修改>: <分支名> <待推送数量> <待拉取数量>]，示例：[Git*: master ↑1 ↓2]，[Git*: ⟳]`

## 常见问题

### 1. 标题信息没有显示Git分支
如果项目不在 Git 版本控制下，或者 .git 目录不在项目根目录，Git 分支信息将不会显示。

### 2. Git状态指示器显示异常
确保 Git 命令可在系统环境变量中访问，且当前用户有权限执行 Git 操作。

### 3. 标题信息更新不及时
标题信息在特定事件（如焦点变化、播放模式改变等）触发时会自动更新。

更多问题，请查阅[问题反馈](../CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](../CHANGELOG.md)
- [贡献指南](../CONTRIBUTING.md)
- [许可证](../LICENSE.md)
