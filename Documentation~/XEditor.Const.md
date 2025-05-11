# XEditor.Const

[![Version](https://img.shields.io/npm/v/org.eframework.u3d.edit)](https://www.npmjs.com/package/org.eframework.u3d.edit)
[![Downloads](https://img.shields.io/npm/dm/org.eframework.u3d.edit)](https://www.npmjs.com/package/org.eframework.u3d.edit)  
[![DeepWiki](https://img.shields.io/badge/DeepWiki-Explore-blue)](https://deepwiki.com/eframework-org/U3D.EDIT)

XEditor.Const 提供了编辑器常量配置的管理功能，支持通过特性标记的方式自定义常量值。

## 功能特性

- 支持特性标记自定义：使用 `Const` 特性标记类来定义常量配置
- 提供默认配置路径：为常用路径提供默认配置，无需手动设置
- 支持运行时动态获取：在运行时自动检测并应用自定义配置值
- 提供类型安全访问：通过泛型方法确保类型安全的配置获取
- 实现配置覆盖机制：允许通过特性标记方式灵活覆盖默认配置

## 使用手册

### 1. 常量配置类定义

#### 1.1 标记配置类
通过 `Const` 特性标记类，使其被识别为常量配置类：

```csharp
[XEditor.Const]
public static class MyConst
{
    // 常量配置属性
}
```

### 2. 配置值获取

#### 2.1 获取自定义配置
使用 `GetCustom` 方法获取自定义配置值：

```csharp
object value = XEditor.Const.GetCustom(typeof(MyAttribute), ref sig, ref prop, defaultValue);
```

#### 2.2 泛型配置获取
使用泛型方法 `GetCoustom` 实现类型安全的配置获取：

```csharp
string value = XEditor.Const.GetCoustom<MyAttribute, string>(ref sig, ref prop, "default");
```

## 常见问题

### 1. 配置无法生效
检查以下几点：
- 确保类已标记 `[XEditor.Const]`
- 确保属性使用了正确的特性标记
- 确保属性为 public static

### 2. 多配置项处理
当多个配置类定义相同配置项时，系统使用首个检测到的配置值。建议在单一配置类中集中管理。

更多问题，请查阅[问题反馈](../CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](../CHANGELOG.md)
- [贡献指南](../CONTRIBUTING.md)
- [许可证](../LICENSE.md)
