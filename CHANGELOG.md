# 更新记录

## [0.1.4] - 
### 变更
- 优化 Task Runner 面板状态的持久化功能，提升用户体验
- 优化 Task Runner 面板的首选项显示，提升用户体验
- 重构 XEditor.Tasks 模块的 Metas 和 Workers 的数据维护方式
- 整理 XEditor.Tasks 模块的单元测试及代码注释

### 修复
- 修复 XEditor.Tasks 面板 Group 的 Tooltip 显示错误问题

## [0.1.3] - 2025-06-17
### 变更
- 新增 XEditor.Cmd.Run/Find 时自动补充项目中的 node_modules/.bin 目录

### 修复
- 修复 XEditor.Tasks.Batch 异步参数 runAsync 的解析逻辑错误

## [0.1.2] - 2025-06-12
### 变更
- 调低 XEditor.Tasks.Batch 响应 OnEditorLoad 事件的优先级，使得高优先级关联业务先执行

### 修复
- 修复 XEditor.Prefs 在 OnPreprocessBuild 未序列化内存中的键值对的问题

## [0.1.1] - 2025-06-11
### 变更
- 优化 XEditor.Tasks.Batch 的错误退出码
- 移除 XEditor.Utility.GetSelectedPath() 函数

### 修复
- 修复 XEditor.Binary 的父类可重写变量缓存重置问题
- 校正若干代码的日志输出信息
- 修复 XEditor.Tasks.Report.Extras 字段未被 JSON 序列化的问题

## [0.1.0] - 2025-06-10
### 变更
- 优化 XEditor.Title 的刷新调用时机及潜在的卡顿（ANR）问题

### 修复
- 修复 XEditor.Cmd.Run 在编辑器构建脚本或启动时（如：InitializeOnLoad）卡顿问题
- 修复 XEditor.Tasks.Run 在 batchMode 环境下执行异步任务的卡顿（ANR）问题
- 修复 Task Runner 面板在 Unity 2021 版本无法被序列化的问题

## [0.0.9] - 2025-06-04
### 修复
- 修复 XEditor.Title 刷新 Git 项目拉取（pull）和推送（push）状态错误的问题

## [0.0.8] - 2025-06-04
### 修复
- 修复 XEditor.Title 在 playModeStateChanged 事件触发时刷新的阻塞问题

## [0.0.7] - 2025-05-28
### 变更
- 重构所有资源的 GUID，提高兼容性

## [0.0.6] - 2025-05-28
### 变更
- 优化 XEditor.Task.Panel 面板的结果显示

### 修复
- 修复 Unity 2021 版本 XEditor.Title 的兼容性问题

## [0.0.5] - 2025-05-22
### 变更
- 移除 Preferences 和 Task Runner 面板的快捷键，由用户自行设定
- 移除 XEditor.Utility.ShowInExplorer 的快捷键绑定，由内置的 Assets/Show in Explorer 替代之

### 新增
- 新增 XEditor.Task.Panel 任务状态及结果显示

### 修复
- 修复 XEditor.Task.Panel 面板的刷新问题

## [0.0.4] - 2025-05-11
### 变更
- 优化 Preferences 和 Task Runner 的菜单栏路径，符合 Unity 插件规范
- 更新依赖库版本

### 新增
- 新增 [DeepWiki](https://deepwiki.com) 智能索引，方便开发者快速查找相关文档

### 修复
- 修复 XEditor.Title.Test 潜在的测试错误

## [0.0.3] - 2025-03-31
### 修复
- 修复 XEditor.Cmd 标准输出异常
- 修复 XEditor.Title 多引擎兼容问题

### 变更
- 优化首选项面板的交互提示
- 更新依赖库版本

### 新增
- 支持多引擎测试工作流

## [0.0.2] - 2025-03-26
### 变更
- 更新依赖库版本

## [0.0.1] - 2025-03-23
### 新增
- 首次发布
