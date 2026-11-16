<div align="center">

# ZoneTray

把 PowerToys 的 FancyZones 拆出来单独跑：八个窗口吸附开关，一个托盘图标，没有编辑器。

![C#](https://img.shields.io/badge/C%23-9.0-239120?logo=csharp&logoColor=white)
![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D4?logo=windows&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-3DA639?logo=opensourceinitiative&logoColor=white)

</div>

装 PowerToys 只为用 FancyZones 有点多余。这个项目只留窗口吸附那一套：从 PowerToys 0.99.1 里取出微软签名的核心引擎，配一个自己写的托盘宿主，双击就常驻后台。分区布局用内置模板切换，所以不需要那个 .NET 编辑器，整个发布包压到 43 MB。

> 只想用起来：去 Releases 下 `ZoneTray.zip` → 解压到任意目录 → 双击 `ZoneTray.exe`。完事。

## 你可能关心的事

| 你可能担心 | 实际做法 |
|---|---|
| 得先装整个 PowerToys 吗 | 不用。发布包里带了从 0.99.1 提取的三个官方签名文件，运行时不依赖 PowerToys |
| 没有编辑器怎么划分区 | 设置面板里有六种预设模板，选中点"应用并重启"就换。想完全自定义就手改 JSON |
| 会不会一直弹窗口 | 不会。只有一个托盘图标，`MainWindowHandle` 恒为 0 |
| 更新会不会丢设置 | 配置和布局都存在 `%LOCALAPPDATA%`，跟程序目录无关，覆盖 exe 就行 |
| 和已装的 PowerToys 冲突吗 | 会。两者共用同一个配置路径和窗口钩子，别同时跑 |
| 能开机自启吗 | 右键托盘图标勾"开机自启"，再点一次取消 |

## 怎么用

双击 `ZoneTray.exe`，托盘出现一个蓝色分区图标，FancyZones 引擎作为子进程跟着起来。

- 左键图标，或右键 → 窗口行为与布局设置：打开设置面板
- 右键 → 开机自启：切换自启状态，菜单打开时会显示当前是开是关
- 右键 → 退出：停掉引擎和托盘

### 八项窗口行为

这些就是 PowerToys 设置里"窗口行为"那一页的开关，键名和官方 `Settings.cpp` 对得上：

| 开关 | 默认值 |
|---|---|
| 屏幕分辨率或工作区变化时，窗口保持在各自区域内 | 开 |
| 区域布局变化时，窗口匹配新的大小/位置 | 关 |
| 新窗口移动到上一个已知区域 | 关 |
| 新窗口移动到当前活动的监视器（实验性） | 关 |
| 取消贴靠时还原窗口原始大小 | 关 |
| 让被拖动的窗口透明 | 关 |
| 允许子窗口对齐 | 关 |
| 贴靠窗口时禁用圆角 | 关 |

改完要点"应用并重启"，PowerToys 这套设置本来就是重启才生效，这里只是把重启按钮做进了面板。

### 六种分区模板

| 模板 | 效果 |
|---|---|
| 左右各半（2 列） | 最常见的左右分屏 |
| 三等分列 / 三等分行 | 横向或纵向三开 |
| 2×2 网格 | 四个象限 |
| 主区 + 右侧两列 | 左边大区，右边两条 |
| 焦点 + 两侧 | 中间主区，左右辅助 |

模板写进 `default-layouts.json`。之所以不用管 `applied-layouts.json`，是因为官方解析器会忽略里面 uuid 全零的默认布局，每次启动都重新按 `default-layouts.json` 应用。想手动微调，直接改这个文件，字段含义见 [使用说明](build/docs/readme.md)。

## 自己构建

需要 Windows、[.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) 和 PowerShell。二进制来源二选一：本机装了 PowerToys 0.99.1，或者保留仓库里已有的 `deployment/src` 三个文件。

```powershell
git clone https://github.com/kelisidina725/ZoneTray.git
cd ZoneTray\build
powershell -ExecutionPolicy Bypass -File .\GenerateDeployment.ps1
```

脚本会提取核心文件、发布托盘程序、复制文档，最后在仓库根目录生成 `ZoneTray.zip`。找不到 PowerToys 安装目录时会自动复用 `deployment/src` 里已有的文件，所以卸掉 PowerToys 也能重新打包。

只想编译托盘程序：

```powershell
dotnet build src\ZoneTray\ZoneTray.csproj -c Release
```

仓库里带三个测试项目，覆盖配置读写、模板写入和图标加载。比如跑配置测试：

```powershell
dotnet run --project tests\SettingsStoreTest -c Release
```

## 目录里有什么

| 路径 | 作用 |
|---|---|
| `src/ZoneTray/` | 托盘宿主全部源码，C# / WinForms |
| `src/ZoneTray/Resources/ZoneTray.ico` | 托盘和 exe 图标，九种尺寸 |
| `build/GenerateDeployment.ps1` | 打包脚本，生成发布包和 zip |
| `build/docs/` | 发布包内文档的源文件 |
| `tools/IconGenerator/` | 图标生成工具 |
| `tests/` | 配置、模板、图标三个测试项目 |
| `deployment/` | 构建产物，不进 git |

`src`、`tests`、`tools`、`build` 会提交。二进制和构建产物靠 `.gitignore` 挡在外面。

## 数据在哪

| 内容 | 路径 |
|---|---|
| 八项开关 | `%LOCALAPPDATA%\Microsoft\PowerToys\FancyZones\settings.json` |
| 默认布局 | 同目录的 `default-layouts.json` |
| 开机自启 | `%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\ZoneTray.lnk` |

写配置时只动那八个键，其他配置原样保留。配置损坏会回退到默认值，不会卡住面板。

## 出问题先看这里

| 现象 | 怎么办 |
|---|---|
| 托盘图标没出现 | 看任务管理器有没有 `ZoneTray` 进程；没有就手动跑一次 exe |
| 拖窗口没反应 | 检查 FancyZones 引擎进程在不在；确认没同时开 PowerToys |
| 改了设置没生效 | 要点"应用并重启"，光勾选不算 |
| 高权限窗口吸附失败 | 用管理员权限运行 ZoneTray，普通权限管不到提权进程 |
| SmartScreen 拦住 | 托盘 exe 未签名，首次运行会有提示，选择仍要运行 |
| 换模板后布局不对 | 手动改 `default-layouts.json` 的 `zone-count`，或删掉文件让程序重建 |

## 免责声明

非官方工具，与 Microsoft Corporation 没有隶属或背书关系。PowerToys 和 FancyZones 是微软的商标，其官方二进制按 MIT License 使用，见 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)。

这个程序会安装全局钩子并改动其他窗口的位置，请自行判断适用场景。因使用本工具造成的窗口状态异常、数据丢失或其他损失，作者不承担责任。

## 协议

[MIT](LICENSE)
