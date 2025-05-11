# XEditor.Npm

[![Version](https://img.shields.io/npm/v/org.eframework.u3d.edit)](https://www.npmjs.com/package/org.eframework.u3d.edit)
[![Downloads](https://img.shields.io/npm/dm/org.eframework.u3d.edit)](https://www.npmjs.com/package/org.eframework.u3d.edit)
[![DeepWiki](https://img.shields.io/badge/DeepWiki-Explore-blue)](https://deepwiki.com/eframework-org/U3D.EDIT)

XEditor.Npm 提供了在 Unity 编辑器中调用和执行 NPM 脚本的工具，支持异步执行、参数传递和错误处理。

## 功能特性

- 支持在 Unity 编辑器中异步执行 NPM 脚本：通过后台任务执行不阻塞主线程
- 支持传递参数给 NPM 脚本：灵活配置脚本执行行为
- 支持错误处理和日志输出：可靠捕获和展示执行结果
- 支持单例模式避免重复执行：优化资源使用
- 自动刷新 AssetDatabase 以同步资源变更：保持项目资源状态一致

## 使用手册

### 1. 命令执行

#### 1.1 创建 NPM 任务
创建一个 NPM 任务实例，指定任务 ID、脚本名称和执行选项：

```csharp
// 创建 NPM 任务，指定 ID、脚本名称、同步执行和工作目录
var npm = new XEditor.Npm(
    id: "my-task",         // 任务唯一标识符
    script: "my-task",     // package.json 中定义的脚本名称
    runasync: false,       // 是否异步执行
    cwd: "path/to/dir"     // 工作目录路径
);
```

#### 1.2 传递参数并执行任务
通过字典传递参数并执行 NPM 任务：

```csharp
// 准备参数字典
var args = new Dictionary<string, string>
{
    { "param1", "value1" },
    { "param2", "value2" }
};

// 执行任务并传递参数
var report = XEditor.Tasks.Execute(npm, args);

// 等待任务完成（如果是同步任务，可以省略此步骤）
report.Task.Wait();
```

### 2. 结果处理

#### 2.1 验证执行结果
检查任务是否成功执行：

```csharp
// 检查任务执行结果
if (report.Result == XEditor.Tasks.Result.Succeeded)
{
    // 任务成功执行
    Debug.Log("NPM 任务执行成功");
}
else
{
    // 任务执行失败
    Debug.LogError($"NPM 任务执行失败: {report.Error}");
}
```

#### 2.2 获取命令输出
从任务报告中获取命令执行的详细输出：

```csharp
// 获取命令执行结果
var cmdResult = report.Extras as XEditor.Cmd.Result;
if (cmdResult != null)
{
    // 输出命令执行的标准输出
    Debug.Log($"命令输出: {cmdResult.Data}");
    
    // 检查退出码
    Debug.Log($"退出码: {cmdResult.Code}");
}
```

## 常见问题

### 1. NPM 命令执行失败
- 现象：NPM 命令执行返回非零退出码
- 原因：系统未正确安装 Node.js 或 NPM，或环境变量配置错误
- 解决：检查 Node.js 和 NPM 安装，确保环境变量正确配置

### 2. 参数传递格式错误
- 现象：NPM 脚本无法正确接收参数
- 原因：参数格式不符合 NPM 脚本的预期
- 解决：检查参数键名是否正确，确保参数值不包含特殊字符

更多问题，请查阅[问题反馈](../CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](../CHANGELOG.md)
- [贡献指南](../CONTRIBUTING.md)
- [许可证](../LICENSE.md)
