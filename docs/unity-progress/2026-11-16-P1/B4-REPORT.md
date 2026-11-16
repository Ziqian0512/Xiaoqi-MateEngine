# B4 · 设置页（D1–D4）交付报告

> DeepSeek · 分支 `xiaoqi/b4-deepseek` · 2026-11-16
> **本线全部未编译**：所有结论都是「按代码应当如此」，不是跑过的结果。

## 一、改了哪些文件

| 文件 | 类型 | 作用 |
|---|---|---|
| `Assets/Xiaoqi/UI2/XqSettingsPanel.cs` | 新建 | 面板本体：玻璃面板 + 左分类栏 6 项 + 右内容区 6 页；`Open`/`Close`/`IsOpen`；Esc 关闭、点面板外空白关闭；`SetUiBlocksMovement` 挡拖动；主题 class 切换；尺寸按屏高 70%（≥600）换算 |
| `Assets/Xiaoqi/UI2/XqSettingsRows.cs` | 新建 | 行模板：`Toggle`（点整行切换）、`SliderRow`（拖动即时回调 / 松手提交 / 双击回默认）、`Dropdown`（自绘玻璃选项列表）、`Button`、`Text`、`Dots`、`Note`（整行灰字）、`InputRow`（输入框 / 密码框 / 右侧按钮）、`StatusRow`（灯 + 文字 + 按钮） |
| `Assets/Xiaoqi/UI2/XqSettingsApply.cs` | 新建 | 设置值 → 宿主的全部连接：`SetWindowSit`、`SetLookAtMouse`、`SetLookStrength`、`SetDancing`/`GetDancing`、`SetIdleSleep`、`SetPatDouble`、`ApplyVolume`/`SetMuted`/`SetVolume`/`CommitVolume`、`ApplyAll(cfg)`、`ApplyOnStart()` |
| `Assets/Xiaoqi/Resources/XqSettings.uss` | 新建 | 设置页专用样式（类名统一 `xq-st-` 前缀）。纯色/边框/圆角，不引图片。玻璃配方照 G59-P1 §4.2：白 78%、描边 1px 白 80%、顶部高光复用 `.xq-hi`、阴影复用 `.xq-shadow` |
| `Assets/Xiaoqi/Runtime/XiaoqiConfig.cs` | 修改 | 只加字段 + `LoadOrCreate` 补缺：`settings_v2`/`theme`/`look_at_mouse`/`look_strength`/`sit_offset`/`idle_sleep_min`/`pat_double`/`muted`/`volume`/`dance_on_music`/`window_sit` |
| `Assets/Xiaoqi/Runtime/XiaoqiSettings.cs` | 修改 | 只在 `Open()` 开头加 5 行：`settings_v2` 或 `-settings-v2` 时转去新面板。其余一行未动 |
| `Assets/Xiaoqi/Pet/PetPat.cs` | 修改 | 只加 `public static bool Enabled = true;` 与 `Update()` 首行 `if (!Enabled) return;` |
| `docs/unity-progress/2026-11-16-P1/ISSUES.md` | 追加 | 第 3–8 行（坐姿高度无字段 / 两项无 cfg 字段 / ApplyAll 停舞 / 画质 4 档 / fake-stage 连接状态 / 版本无提交号） |
| `docs/unity-progress/2026-11-16-P1/B4-REPORT.md` | 新建 | 本文件 |

`XiaoqiSystems.cs`、`XqPetHud.cs`、`XqTodayCard.cs`、`xq.uss`、`XqMenu.cs`、`Host/*`、`XiaoqiTray.cs` 与任何 ME 原文件均未改动。

## 二、六个页面的行与默认值

| 页 | 行 | 控件 | 默认 |
|---|---|---|---|
| 外观 | 大小 / 画质 / 帧率 / 主题色 / 模型 | 滑条 / 下拉 / 下拉 / 三色点 / 灰字+按钮 | 现值 / 现值 / 现值 / 粉 / 现值 |
| 行为 | 坐在窗口上 / 坐姿高度微调 / 看向鼠标 / 看向的力度 / 放音乐时自己跳 / 多久不动就睡着 / 双击摸头 | 开关 / 滑条（灰掉）/ 开关 / 滑条 / 开关 / 下拉 / 开关 | 开 / 0 / 开 / 100% / 关 / 关 / 开 |
| 显示 | 始终置顶 / 全屏或游戏时躲开 / 身边小胶囊 / 她说话的气泡 / 她外出时 | 开关 / 开关 / 下拉 / 开关 / 下拉 | 开 / 开 / 淡出 / 开 / G55 现值 |
| 声音 | 静音 / 总音量 | 开关 / 滑条 | 关 / 100% |
| 连接 | 连接状态 / 服务器地址 / 令牌 / 情感线 / 开机自启 | 灯+文字+按钮 / 输入框 / 密码框+粘贴 / 开关 / 开关 | — / 现值 / 现值 / 关 / 关 |
| 关于 | 版本 / ME 署名 / 模型授权 / 打开日志目录 | 文字 / 按钮 / 文字 / 按钮 | — / — / — / — |

「她在哪块屏」按任务书整行不画。

## 三、`TODO(编译员)` 总表（没有 Unity 没法确认的 API）

全部 7 处，都在 `Assets/Xiaoqi/UI2/` 两个文件里，行号对应当前分支 HEAD。

| # | 文件 | 行 | API | 用途 | 说明 |
|---|---|---|---|---|---|
| 1 | `XqSettingsPanel.cs` | 59 | `IVisualElementScheduler`：`VisualElement.schedule.Execute(Action).Every(ms)` | 面板开着时做 16ms Tick（Esc 关闭、点外关闭、挡拖动、连接状态刷新），避免另建 MonoBehaviour | UI Toolkit 标准 API，本仓库无先例 |
| 2 | `XqSettingsPanel.cs` | 446 | `GUIUtility.systemCopyBuffer` | 令牌行「粘贴」读剪贴板 | Unity 标准 API，本仓库无先例 |
| 3 | `XqSettingsRows.cs` | 119 | `VisualElement.CapturePointer` / `ReleasePointer` | 滑条拖动时把指针锁在轨道上，移出轨道仍能继续拖 | UI Toolkit 标准 API，本仓库无先例 |
| 4 | `XqSettingsRows.cs` | 159 | `Length.Percent(float)` | 滑条填充宽度（0–100%） | UI Toolkit 标准写法，本仓库无先例 |
| 5 | `XqSettingsRows.cs` | 313 | `TextField` / `isPasswordField` / `SetValueWithoutNotify` | 服务器地址与令牌输入框 | 标准 API，本仓库无先例 |
| 6 | `XqSettingsRows.cs` | 319 | `KeyDownEvent` + `KeyCode.Return` / `KeyCode.KeypadEnter` | 输入框回车提交 | 标准 API，本仓库无先例 |
| 7 | `XqSettingsRows.cs` | 326 | `FocusOutEvent` | 输入框失焦提交 | 标准 API，本仓库无先例 |

补充说明（不是 TODO，但编译时可能需要注意）：

- `XqSettings.uss` 里用了 `--xq-r-panel` / `--xq-r-pill` / `--xq-accent` / `--xq-ink` / `--xq-ink-soft` / `--xq-accent-ink` 六个变量，都来自现有 `xq.uss` 的主题 class，已带 fallback 值。
- `XqSettingsRows.Dots()` 用 `d.tooltip` 存色点名称，只是给编译员看的，不影响功能。

## 四、`ApplyOnStart()` 谁接、什么时候接、前提是什么

- **函数**：`Xiaoqi.UI2.XqSettingsApply.ApplyOnStart()`（无参数、无返回值、不抛异常）。
- **谁接**：WB 在 `XiaoqiSystems.cs` 里接。**本分支不动 `XiaoqiSystems.cs`**。
- **什么时候接**：模型加载完成之后调一次。太早调的话 `AvatarMouseTracking` / `SaveLoadHandler` / `AvatarSleepController` 还没出来，只会打 `XQ_SET2 miss ...` 就跳过。
- **前提对象**：`XiaoqiRuntime.Config` 已赋值（否则打 `XQ_SET2 miss cfg`）；上述三个宿主组件已随模型加载出来；`PetHost.I` 已挂上。
- **重复调用是安全的**：`SetDancing` / `WindowSitEnabled` 都只在与现值不同时才写，不会每次启动都触发一遍宿主 `ApplyAll` 把正在跳的舞停掉。但**仍建议只调一次**。
- **另一个调用点**：`XqSettingsPanel` 第一次 `Open()` 时也会调一次 `ApplyAll(cfg)`（§4 D4 原文要求），与启动那次不冲突。

`ApplyOnStart()` 会重新应用这 7 项：`look_at_mouse`、`look_strength`、`idle_sleep_min`、`pat_double`、`muted`+`volume`（→ `AudioListener.volume`）、`dance_on_music`、`window_sit`。

## 五、没做的 / 没接的

| 项 | 原因 |
|---|---|
| 坐姿高度微调连宿主 | ME 里找不到对应字段（ISSUES 第 2、3 行）。整行 `SetEnabled(false)` + 右侧「暂未接入」，`cfg.sit_offset` 只存盘 |
| 画质第四档（最高） | G59-P1 §6 只列 低/中/高。ME 的 `graphicsQualityLevel` 允许 0–3，现值 ≥3 时显示「高」但不写回宿主（ISSUES 第 6 行） |
| 「她在哪块屏」 | 任务书标「不做」，整行不画 |
| 「版本 / 提交号」的提交号 | 只有 `Application.version`，提交号等 B8 由编译脚本写入（ISSUES 第 8 行） |
| 托盘「调参（F9）」 | 不在 B4 范围 |
| 窗口撑大 | 任务书明确由 WB 另做 |
| `settings_v2` 默认值翻转 | 任务书明确本轮不做，默认仍为 `false`；可用 `-settings-v2` 命令行参数临时打开 |
| 其它 5 个 ME 面板的 bug | 不在 §2 文件范围，见到也不改 |
| 所有 `.meta` 文件 | 任务书要求不写，由 Unity 自己生成、编译员提交 |

## 六、已知副作用（编译后要验的）

1. `SetDancing` 真正切换时仍会停一次正在跳的舞 —— 宿主 `ApplyAllSettingsToAllAvatars()` 会把 `isDancing` 置 false，属 ME 自身行为（ISSUES 第 5 行）。
2. 「大小 / 画质 / 帧率 / 始终置顶」在提交时调 `PetHost.I.SaveSettings()`，而 `Bind` 里还会 `XiaoqiConfig.Save(cfg)`，同一动作写两次盘（一份宿主存档、一份 `xiaoqi_config.json`）。功能无影响，只多一次 IO。
3. `-fake-stage` 下连接状态会在 `3×poll_sec+5` 秒后显示「连不上」（ISSUES 第 7 行），真服务器不会。
