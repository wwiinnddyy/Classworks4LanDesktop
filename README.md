# Classworks 作业组件插件

本插件示例演示了如何为阑山桌面 (LanMountainDesktop) 开发一个可配置的作业板组件，用于展示、添加并同步来自 Classworks 平台的作业，同时提供设置界面支持直接填写登录信息或通过浏览器登录获取令牌。

> **注意**：由于当前无法访问 GitHub 上的官方插件文档，以下实现根据阑山桌面仓库中公开的信息【203828600613967†L449-L506】推测而来，仅用于演示插件结构和基本的 MVVM 模式。实际开发时请参照官方 `LanMountainDesktop.PluginSdk` 和示例插件仓库调整代码。

## 项目结构

```
ClassworksPlugin/
├── ClassworksPlugin.csproj            — 插件项目文件
├── plugin.json                        — 插件清单，定义 ID、名称、版本等元数据
├── ClassworksPlugin.cs                — 插件入口类，继承自 `WidgetPluginBase`
├── ClassworksWidget.cs                — 插件的组件类，实现桌面组件逻辑
├── ClassworksViewModel.cs             — 视图模型，负责加载作业数据并通知 UI 更新
├── ViewModels/Settings/               — 设置界面的视图模型
│   └── ClassworksSettingsViewModel.cs — 设置页 ViewModel
├── Models/Assignment.cs               — 作业数据模型
├── Services/ClassworksService.cs      — 调用 Classworks API 的服务（负责获取和写入作业）
├── Settings/PluginConfig.cs            — 本地配置读取与保存工具
├── Views/
│   ├── ClassworksWidgetView.axaml      — 组件界面 XAML
│   └── ClassworksWidgetView.axaml.cs   — 组件界面代码隐藏
├── Views/Settings/
│   ├── ClassworksSettingsPage.axaml    — 插件设置界面 XAML
│   └── ClassworksSettingsPage.axaml.cs — 插件设置界面代码隐藏
└── .github/workflows/build.yml          — GitHub Action 工作流，自动构建和打包插件
```

## 编译与调试

1. 确保在本机已经克隆了 `LanMountainDesktop` 仓库，并安装了 .NET 10 SDK。
2. 将此插件放在阑山桌面的插件目录下或通过 `dotnet new install` 安装插件模板并调整路径。
3. 在 `LanMountainDesktop.slnx` 解决方案中添加 `ClassworksPlugin.csproj`，还需要在主程序的插件配置文件中注册 `com.example.classworkswidget`。
4. 构建解决方案并运行桌面宿主，在插件市场中启用该插件即可看到组件。

5. 本仓库包含 `.github/workflows/build.yml` 工作流。推送或创建拉取请求后，GitHub Action 会自动安装 .NET 10 SDK、还原依赖、构建插件并生成 `.laapp` 插件包。工作流还会计算 MD5 和 SHA256 校验和，并根据 `plugin.json` 生成一个 `market-manifest.json` 文件。这些文件会作为构建工件上传，可直接用于发布到阑山插件市场。

## 后续开发建议

* **集成真实 API**：文档指出，访问令牌通过 `POST /apps/auth/token` 获取，需要提供命名空间、密码和 appId【250439475987970†L42-L69】。获取令牌后，作业存储在 `classworks-data-YYYYMMDD` 键中，其 `homework` 字段包含科目名称和内容【706689548766089†L85-L116】。插件中已实现这些接口，开发者需在设置中填写自己的参数即可。
* **完善交互**：已实现独立的设置界面，并提供命名空间、密码、AppId 输入框和浏览器登录选项。后续可进一步持久化 UI 状态、增加错误提示、本地缓存等功能。
* **适配移动端**：阑山桌面面向跨平台，若计划在平板或手机上使用，还需测试各类分辨率下的布局。

## 添加作业功能

最新版插件在作业列表下方增加了“科目”和“内容”输入框以及“添加”按钮。用户输入科目和内容后点击“添加”，插件会：

1. 将新作业添加到本地 `Assignments` 集合并立即显示在列表中。
2. 调用 `ClassworksService.AddAssignmentAsync`，读取当前日期对应的 `classworks-data-YYYYMMDD` 键并更新其 `homework` 字段，然后使用 `POST /kv/<key>` 写回云端【250439475987970†L88-L125】。
3. 如果该键不存在，则会创建一个新对象，只包含 `homework` 字段；已有的 `attendance` 字段会被保留。

因此，作业添加过程不仅在本地生效，还会与 Classworks KV 服务同步，其他客户端调用相同日期的键即可看到更新后的作业列表。【706689548766089†L85-L116】详细描述了 `homework` 字段的结构。

## 发布到阑山插件市场

阑山桌面的官方插件市场由姊妹仓库 **LanAirApp** 提供元数据聚合。要让你的插件在市场中可见，需要在 GitHub Release 中附带正确的资产。官方示例插件的说明指出，发布资产应包含：

- 生成的 `.laapp` 包
- `market-manifest.json` —— 描述插件元数据、兼容性、仓库地址以及包下载地址【288643679924299†L23-L34】
- `sha256.txt` 和 `md5.txt` —— 包文件的校验和【288643679924299†L23-L34】

插件市场会优先从已打标签的 GitHub Release 读取这些文件并聚合到市场中【288643679924299†L32-L34】。因此，在你的插件仓库中创建一个以版本号为前缀的标签（例如 `v0.1.0`）并发布 Release，上传 `.laapp` 包及配套的 manifest 和 checksum 文件，即可完成发布。我们的 GitHub Action 工作流已经自动生成这些文件并通过工件提供下载，你可以将其放入 Release 中。

## 设置页面

插件提供了独立的设置界面。在阑山桌面的插件管理面板中点击该插件的“设置”即可打开。设置界面由 FluentAvalonia 控件实现，外观与宿主应用保持一致，并支持两种登录方式：

1. **直接填写**：用户在文本框中输入命名空间、密码和应用 ID，并点击“保存设置”。这些信息会保存到本地 `classworks.config.json` 文件中，插件刷新作业时自动读取这些值【250439475987970†L42-L69】。
2. **浏览器登录**：点击“通过浏览器登录”按钮会在默认浏览器中打开 ZeroCat OAuth 登录页面。完成授权后，需要将获取到的令牌填回设置中（完整的 OAuth 回调处理在此示例中尚未实现）。

通过合理利用设置界面，用户可以灵活地管理凭据和登录方式，而不必在组件界面内直接输入敏感信息。

## 引用

阑山桌面 README 描述了插件生态、插件 SDK 和示例插件【203828600613967†L449-L506】。由于网络限制无法访问完整文档，建议在网络恢复后查看官方文档以调整接口和版本要求。