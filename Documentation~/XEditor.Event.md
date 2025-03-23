# XEditor.Event

[![Version](https://img.shields.io/npm/v/org.eframework.u3d.edit)](https://www.npmjs.com/package/org.eframework.u3d.edit)
[![Downloads](https://img.shields.io/npm/dm/org.eframework.u3d.edit)](https://www.npmjs.com/package/org.eframework.u3d.edit)  

XEditor.Event 提供了基于接口的编辑器事件系统，支持自定义事件回调和通知，内置了一些常用的事件，如编辑器初始化、更新和退出等。

## 功能特性

- 支持自定义事件回调：通过接口方式定义事件，支持优先级排序和单例模式
- 提供参数解析工具：简化事件参数的获取和类型转换
- 内置常用事件：提供初始化、加载、更新和退出等常用的事件

## 使用手册

### 1. 事件回调定义

#### 1.1 创建事件回调类
实现 `XEditor.Event.Callback` 接口和对应的事件接口，创建自定义事件处理器。

```csharp
// 创建一个处理编辑器初始化事件的回调类
public class MyEditorInitHandler : XEditor.Event.Internal.OnEditorInit
{
    // 设置回调优先级，数值越小优先级越高
    public int Priority => 0;
    
    // 设置是否为单例模式
    public bool Singleton => true;
    
    // 必须使用显式接口实现
    void XEditor.Event.Internal.OnEditorInit.Process(params object[] args)
    {
        // 处理初始化逻辑
        Debug.Log("编辑器初始化完成");
    }
}
```

#### 1.2 创建自定义事件
通过继承 `XEditor.Event.Callback` 接口创建自定义事件标识。

```csharp
// 定义自定义事件接口
public interface OnMyCustomEvent : XEditor.Event.Callback
{
    void Process(params object[] args);
}

// 创建事件处理器
public class MyCustomEventHandler : OnMyCustomEvent
{
    public int Priority => 0;
    public bool Singleton => false;
    
    // 必须使用显式接口实现
    void OnMyCustomEvent.Process(params object[] args)
    {
        // 解析参数
        XEditor.Event.Decode<string, int>(out var message, out var value, args);
        Debug.Log($"收到自定义事件: {message}, 值: {value}");
    }
}
```

### 2. 事件通知

#### 2.1 内置事件清单
XEditor.Event 提供了以下内置事件：

| 事件接口 | 说明 | 触发时机 | 参数说明 |
|---------|------|---------|----------|
| `OnEditorInit` | 编辑器初始化事件 | 编辑器首次启动时 | 无 |
| `OnEditorLoad` | 编辑器加载事件 | 每次编辑器启动时 | 无 |
| `OnEditorUpdate` | 编辑器更新事件 | 编辑器每帧更新时 | 增量时间 |
| `OnEditorQuit` | 编辑器退出事件 | 编辑器关闭时 | 无 |
| `OnPreferencesApply` | 首选项应用事件 | 首选项数据应用时 | 无 |
| `OnPreprocessBuild` | 构建前处理事件 | 项目构建开始前 | `BuildReport report`: 构建报告 |
| `OnPostprocessBuild` | 构建后处理事件 | 项目构建完成后 | `BuildReport report`: 构建报告 |

#### 2.2 触发事件通知
使用 `XEditor.Event.Notify<T>` 方法触发事件。

```csharp
// 触发事件
XEditor.Event.Notify<OnMyCustomEvent>("自定义消息", 200);
```

### 3. 参数解析

#### 3.1 单参数解析
使用 `XEditor.Event.Decode<T>` 方法解析单个参数。

```csharp
void IMyEventHandler.Process(params object[] args)
{
    // 解析单个参数
    XEditor.Event.Decode<string>(out var message, args);
    Debug.Log($"消息: {message}");
}
```

#### 3.2 多参数解析
使用 `XEditor.Event.Decode<T1, T2>` 或 `XEditor.Event.Decode<T1, T2, T3>` 方法解析多个参数。

```csharp
void IMyEventHandler.Process(params object[] args)
{
    // 解析两个参数
    XEditor.Event.Decode<string, int>(out var message, out var value, args);
    Debug.Log($"消息: {message}, 值: {value}");
    
    // 解析三个参数
    XEditor.Event.Decode<string, int, bool>(out var msg, out var val, out var flag, args);
    Debug.Log($"消息: {msg}, 值: {val}, 标志: {flag}");
}
```

## 常见问题

### 1. 事件回调未触发
- 确保回调类正确实现了对应的事件接口
- 确保使用显式接口实现（`void InterfaceName.Process`）来声明处理方法
- 如果使用单例模式，确保 `Singleton` 属性返回 `true` 并提供了公共的 `Instance` 静态属性/字段

### 2. 参数解析失败
确保传递的参数类型与解析时指定的泛型类型匹配，或者可以进行类型转换。参数解析按照顺序进行，确保参数顺序正确。

### 3. 回调执行顺序不符合预期
检查各回调类的 `Priority` 属性返回值，数值越小优先级越高，相同优先级的回调执行顺序不保证。

更多问题，请查阅[问题反馈](../CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](../CHANGELOG.md)
- [贡献指南](../CONTRIBUTING.md)
- [许可证](../LICENSE.md)
