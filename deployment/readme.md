# ZoneTray

FancyZones 窗口吸附功能，独立后台驻留版（无布局编辑器）。

## 使用

- 双击 `ZoneTray.exe` 启动；托盘出现图标，FancyZones 核心引擎作为子进程运行。
- 左键托盘图标，或右键 → 窗口行为与布局设置：打开设置面板。
- 右键 → 开机自启：勾选/取消开机自启。
- 右键 → 退出：停止 FancyZones 与托盘。

## 分区布局

在设置面板的"分区布局"下拉框选择预设模板（左右各半、三等分列/行、2×2 网格、主区+侧列、焦点布局），
点击"应用并重启"后生效。分区数据写入：

`%LOCALAPPDATA%\Microsoft\PowerToys\FancyZones\default-layouts.json`

本版本不含可视化布局编辑器，仅提供上述固定模板。需要完全自定义分区时，可手动编辑该 JSON 文件
（格式：`monitor-configuration` 为 `horizontal`/`vertical`，`type` 支持 `columns`/`rows`/`grid`/
`priority-grid`/`focus`，`zone-count` 为区域数）。

## 窗口行为设置

配置保存在 `%LOCALAPPDATA%\Microsoft\PowerToys\FancyZones\settings.json`。在面板中修改并点击"应用并重启"后生效。

### 包含的 8 项窗口行为

1. 屏幕分辨率或工作区变化时，使窗口保持在各自的区域内
2. 区域布局变化时，窗口匹配新的大小/位置
3. 新窗口移动到上一个已知区域
4. 新窗口移动到当前活动的监视器（实验性）
5. 取消贴靠时还原窗口的原始大小
6. 让被拖动的窗口透明
7. 允许子窗口对齐
8. 贴靠窗口时禁用圆角

## 开机自启

右键托盘图标 → 勾选"开机自启"即可开启；再次点击取消。菜单打开时会显示当前状态。

（可选）也可运行 `Add-ToAutostart.ps1` 手动添加启动项。
