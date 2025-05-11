# XEditor.Prefs

[![Version](https://img.shields.io/npm/v/org.eframework.u3d.edit)](https://www.npmjs.com/package/org.eframework.u3d.edit)
[![Downloads](https://img.shields.io/npm/dm/org.eframework.u3d.edit)](https://www.npmjs.com/package/org.eframework.u3d.edit)  
[![DeepWiki](https://img.shields.io/badge/DeepWiki-Explore-blue)](https://deepwiki.com/eframework-org/U3D.EDIT)

XEditor.Prefs 提供了编辑器首选项的加载和应用功能，支持自动收集和组织首选项面板、配置持久化和构建预处理。

## 功能特性

- 面板管理：基于 Unity SettingsProvider 组织首选项面板，提供可视化的配置管理界面
- 构建预处理：在构建时处理和验证首选项，支持变量求值和编辑器配置清理

## 使用手册

### 1. 用户交互

#### 1.1 打开界面
- 通过菜单：`Tools/EFramework/Preferences`
- 快捷键：`Ctrl+R`
- 代码调用：`XEditor.Prefs.Open()`

#### 1.2 配置操作
- 保存配置：点击底部工具栏的"Save"按钮
- 应用配置：点击底部工具栏的"Apply"按钮
- 克隆配置：点击顶部工具栏的"Clone"按钮
- 删除配置：点击顶部工具栏的"Delete"按钮

#### 1.3 面板导航
- 区域折叠：点击区域标题前的折叠箭头
- 配置切换：使用顶部下拉列表切换不同配置文件
- 文件定位：点击配置文件右侧的"定位"按钮

### 2. 自定义面板

#### 2.1 面板定义
```csharp
public class MyPrefsPanel : XPrefs.Panel
{
    // 面板所属区域
    public override string Section => "MySection";
    
    // 面板提示信息
    public override string Tooltip => "My Panel";
    
    // 是否支持折叠
    public override bool Foldable => true;
    
    // 面板优先级（数值越小越靠前）
    public override int Priority => 0;
}
```

#### 2.2 生命周期
```csharp
public class MyPrefsPanel : XPrefs.Panel
{
    // 面板激活时调用
    public override void OnActivate(string searchContext, VisualElement root)
    {
        // 初始化面板
    }
    
    // 绘制界面时调用
    public override void OnVisualize(string searchContext)
    {
        // 绘制配置界面
    }

    // 面板停用时调用
    public override void OnDeactivate()
    {
        // 清理资源
    }

    // 保存配置时调用
    public override bool Validate()
    {
        // 验证配置有效性
        return true;
    }

    // 保存配置时调用
    public override void OnSave()
    {
        // 保存配置
    }
    
    // 应用配置时调用
    public override void OnApply()
    {
        // 应用配置
    }
}
```

### 3. 构建预处理

#### 3.1 变量求值
```json
{
    "build_path": "${Env.ProjectPath}/Build",
    "version": "${Env.Version}",
    "const_value@Const": "${Env.LocalPath}"  // @Const 标记的值不会被求值
}
```

#### 3.2 编辑器配置
```json
{
    "normal_key": "runtime_value",
    "editor_key@Editor": "editor_value"  // @Editor 标记的配置在构建时会被移除
}
```

#### 3.3 预处理流程
构建时会进行以下检查：
1. 检查首选项文件是否存在
2. 验证首选项内容是否有效
3. 对首选项进行变量引用求值
4. 保存处理后的首选项文件至 StreamingAssets/Preferences.json

## 常见问题

### 1. 配置未生效
- 现象：修改配置后未生效
- 原因：配置未保存或未应用
- 解决：点击保存按钮并应用配置

### 2. 构建失败
- 现象：构建时报错
- 原因：首选项验证失败
- 解决：检查首选项配置是否完整

### 3. 变量求值异常
- 现象：配置中的变量未被正确替换
- 原因：变量引用格式错误或循环引用
- 解决：检查变量引用格式，避免循环引用

更多问题，请查阅[问题反馈](../CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](../CHANGELOG.md)
- [贡献指南](../CONTRIBUTING.md)
- [许可证](../LICENSE.md)
