# Classworks for Lan Desktop

Classworks 官方数据协议的阑山桌面作业组件。插件读取并更新 Classworks 每日作业板数据，让班级大屏网页端与阑山桌面组件共享同一份作业内容。

## 当前基线

- 插件版本：`0.2.0`
- Target framework：`net10.0`
- Plugin SDK / API：`5.0.0`
- 最低宿主版本：`0.8.6`（首个包含 Plugin SDK 5 的宿主版本）
- UI 基线：Avalonia `12.1.0`（由 Plugin SDK 5 提供）
- 生产清单：`airapp.json`
- 运行模式：`in-proc`
- 发布资产：`Classworks4LanDesktop.0.2.0.laapp`

这是传统 LanMountainDesktop 插件，不是第三方 AirApp 原型。生产安装和市场发布只使用 `airapp.json + .laapp`。

## 与 Classworks 的集成

插件与 [Classworks](https://github.com/ZeroCatDev/Classworks) 使用相同的数据约定：

- 默认 KV 服务：`https://kv-service.houlang.cloud`
- 兼容旧服务：`https://kv-service.wuyuan.dev`
- 每日数据键：`classworks-data-YYYYMMDD`
- 作业数据：`homework.<subject>.content`
- 鉴权请求头：`x-app-token`
- Classworks App ID：`d158067f53627d2b98babe8bffd2fd7d`

推荐直接把 Classworks 网页端当前使用的 KV App Token 填入插件设置。也可以填写命名空间和密码，由插件通过 `/apps/auth/token` 获取令牌。插件更新作业时只替换 `homework`，会保留 `attendance` 和服务端返回的其他字段。

## 本地构建

需要 .NET 10 SDK，并在本仓库同级准备 `LanMountainDesktop` 主仓库：

```powershell
./scripts/Initialize-LocalPackageFeed.ps1
./scripts/Test-PluginConsistency.ps1
dotnet restore ./ClassworksPlugin.csproj --configfile ./NuGet.config --force --no-cache
dotnet build ./ClassworksPlugin.csproj -c Release --no-restore
./scripts/Test-PluginConsistency.ps1 -PackagePath ./Classworks4LanDesktop.0.2.0.laapp
```

`LanMountainDesktop.AirAppSdk` 的构建目标会在仓库根目录生成 `.laapp`。

## 发布到市场

GitHub Release 应包含：

- `Classworks4LanDesktop.0.2.0.laapp`
- `market-manifest.json`（嵌套 schema v2）
- `sha256.txt`
- `md5.txt`

Release tag 必须是 `v0.2.0`，包内 `airapp.json` 的 `id/version/apiVersion`、资产名和 tag 必须一致。CI 和手动发布工作流会执行这些校验。

## 安全说明

App Token 与命名空间密码只保存在宿主的插件作用域设置存储中；升级到 `0.2.0` 时会一次性导入旧数据目录中的 `classworks.settings.json`。凭据不会写入插件仓库。发布日志、Issue 和截图中请勿包含真实令牌。
