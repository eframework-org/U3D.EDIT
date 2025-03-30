# XEditor.Binary

[![Version](https://img.shields.io/npm/v/org.eframework.u3d.edit)](https://www.npmjs.com/package/org.eframework.u3d.edit)
[![Downloads](https://img.shields.io/npm/dm/org.eframework.u3d.edit)](https://www.npmjs.com/package/org.eframework.u3d.edit)

XEditor.Binary 提供了一套完整的构建流程管理系统，简化了多平台项目的构建配置，支持自动化和构建后处理。

## 功能特性

- 多平台构建：支持 Windows、Linux、macOS、Android、iOS、WebGL 等平台
- 构建配置管理：通过 BuildProfile 管理构建配置，支持版本号、签名证书等参数设置
- 构建流程管理：包含预处理、构建、后处理三个阶段，支持符号表备份等功能
- 可视化管理：支持构建文件的搜索、重命名、运行和目录管理

## 使用手册

### 1. 构建配置

#### 1.1 构建名称

默认的构建名称由以下部分组成：`{Solution}-{Channel}-{Mode}{LogLevel}-{DateTime}{Index}`

| 字段 | 说明 | 示例 |
|------|------|------|
| Solution | 解决方案名称前 3 字符(大写) | EFU |
| Channel | 渠道名称前 3 字符(大写) | DEV |
| Mode | 应用模式首字母 | D(Dev) |
| LogLevel | 日志等级数字 | 1 |
| DateTime | 日期(yyyyMMdd) | 20250325 |
| Index | 当天构建序号 | 1 |

示例：
```csharp
// 默认构建名称
var handler = new XEditor.Binary();
// 输出: EFU-DEV-D1-202503251
Assert.That(handler.Name, Does.Match(@"^EFU-DEV-D1-\d{8}\d+$"));

// 自定义构建名称
class MyBinary : XEditor.Binary 
{
    public override string Name => "CustomName";
}
```

#### 1.2 参数配置

##### 面板参数

通过面板可配置以下参数：

```csharp
// BuildProfile 配置文件
[Tasks.Param(name: "Profile", tooltip: "Build Profile.", persist: true)]
protected string ProfileFile;

// Android 签名配置
[Tasks.Param(name: "KeyName", tooltip: "Android Keystore Name.", 
    persist: true, platform: XEnv.PlatformType.Android)]
protected string KeystoreName;

[Tasks.Param(name: "KeyPass", tooltip: "Android Keystore Pass.", 
    persist: true, platform: XEnv.PlatformType.Android)]
protected string KeystorePass;

[Tasks.Param(name: "AliasName", tooltip: "Android Keyalias Name.", 
    persist: true, platform: XEnv.PlatformType.Android)]
protected string KeyaliasName;

[Tasks.Param(name: "AliasPass", tooltip: "Android Keyalias Pass.", 
    persist: true, platform: XEnv.PlatformType.Android)]
protected string KeyaliasPass;

// iOS 签名配置
[Tasks.Param(name: "Signing", tooltip: "iOS Signing Team.", 
    persist: true, platform: XEnv.PlatformType.iOS)]
protected string SigningTeam;
```

##### 继承参数

通过继承 XEditor.Binary 类可以覆盖以下参数：

```csharp
// 构建输出目录
public virtual string Output { get; }

// 构建名称
public virtual string Name { get; }

// 构建版本号
public virtual string Code { get; }

// 构建选项
public virtual BuildOptions Options { get; }

// 构建场景列表
public virtual string[] Scenes { get; }

// 构建定义符号列表
public virtual string[] Defines { get; }

// 构建输出文件
public virtual string File { get; }
```

示例：自定义构建参数
```csharp
internal class MyBinary : XEditor.Binary
{
    // 自定义输出目录
    public override string Output => XFile.PathJoin(Root, "CustomOutput");

    // 自定义构建名称
    public override string Name => "CustomName";

    // 自定义版本号
    public override string Code => "202501011";

    // 自定义构建选项
    public override BuildOptions Options => BuildOptions.Development | BuildOptions.AllowDebugging;

    // 自定义场景列表
    public override string[] Scenes => new string[] { "Assets/Scenes/Test.unity" };
}
```

### 2. 构建流程

#### 2.1 预处理阶段

```csharp
public override void Preprocess(Tasks.Report report)
{
    // 1. 加载 BuildProfile 配置
    if (!string.IsNullOrEmpty(ProfileFile))
    {
        profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(ProfileFile);
        BuildProfile.SetActiveBuildProfile(profile);
    }

    // 2. 设置构建选项
    options = BuildOptions.None;
    if (Debug.isDebugBuild)
    {
        options |= BuildOptions.Development;
        options |= BuildOptions.AllowDebugging;
    }

    // 3. 生成构建路径和名称
    output = XFile.PathJoin(Root, XEnv.Channel, XEnv.Platform.ToString());
    name = $"{Solution}-{Channel}-{Mode}{LogLevel}-{DateTime}{Index}";
    
    // 4. 配置平台参数
    PlayerSettings.bundleVersion = XEnv.Version;
    if (XEnv.Platform == XEnv.PlatformType.Android)
    {
        PlayerSettings.Android.keystoreName = KeystoreName;
        PlayerSettings.Android.keystorePass = KeystorePass;
    }
}
```

#### 2.2 构建阶段

```csharp
public override void Process(Tasks.Report report)
{
    // 1. 执行平台构建
    var buildReport = BuildPipeline.BuildPlayer(Scenes, File, 
        BuildTarget.StandaloneWindows64, Options);
    
    // 2. 备份符号表
    var symbolRoot = XFile.PathJoin(Root, "Symbol", XEnv.Channel, 
        XEnv.Platform.ToString());
    var symbolPath = XFile.PathJoin(symbolRoot, Name);
    
    // 3. 生成符号表压缩包
    Utility.ZipDirectory(symbolPath, symbolPath + ".zip");
}
```

#### 2.3 后处理阶段

```csharp
public override void Postprocess(Tasks.Report report)
{
    // 恢复 BuildProfile 配置
    if (lastProfile) BuildProfile.SetActiveBuildProfile(lastProfile);
}
```

### 3. 可视化面板

#### 3.1 界面布局

```
+-----------------------+
|         Search        |
+-----------------------+
|    Name  Path  Run    |
|    Name  Path  Run    |
|          ...          |
+-----------------------+
```

#### 3.2 操作说明

| 功能 | 操作 | 说明 |
|------|------|------|
| 搜索 | 输入关键字 | 按名称过滤构建文件 |
| 重命名 | 双击名称 | 修改构建文件名称 |
| 打开目录 | Path 按钮 | 打开构建文件所在目录 |
| 运行程序 | Run 按钮 | 运行或安装构建文件 |

```csharp
// 运行构建文件示例
var handler = new XEditor.Binary();
handler.Run(path: "path/to/build", name: "build_name");
```

## 常见问题

### 1. Unity 6.0 构建配置恢复失败

**问题描述**
- 版本：Unity 6.0.32f1
- 现象：编译后恢复配置报错 "AssertionException: Build profile is null"
- 原因：ScriptableObject 在构建时被销毁，AssetDatabase.LoadAssetAtPath 重新加载的对象也不行
- 解决：使用 try-catch 捕获异常，可以正常恢复

更多问题，请查阅[问题反馈](../CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](../CHANGELOG.md)
- [贡献指南](../CONTRIBUTING.md)
- [许可证](../LICENSE.md)