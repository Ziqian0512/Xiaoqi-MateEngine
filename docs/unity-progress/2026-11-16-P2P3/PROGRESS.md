# P2 / P3 进度（代码先行，全部 [未编]）

> 11-16 主人授权：P2/P3 在没有 Unity 的情况下先写代码，回家看实机再改。关卡状态**不变**（`WB-GATE.md` 里 P2/P3 仍「未开始」）。
> 代码在分支 `xiaoqi/p2`（从 `xiaoqi/p1` 切，WB 写）。不合 `xiaoqi/p1`。每项状态只有三种：**写完[未编] / 编过 / 实机过**。

## P2 近景

| 步 | 内容 | 谁 | 状态 | 文件 |
|---|---|---|---|---|
| P2-0 | 服务器 `POST /api/body/event` | WB | **双端已测**（WB 本机 19 项 + 真 HTTP 5 项；Grok 另机 19 项全过 EXIT=0），未部署 | 主库 `server/E1c-body-event` `fa70cb2b` |
| P2-1 | `XqMode` 三态 + `XqEventSink` + 入口 | WB | 写完[未编] | `Assets/Xiaoqi/Mode/XqMode.cs`、`XqEventSink.cs`、`XqModeConfig.cs`；入口：右键「让她…」子菜单末尾 3 项（`XqMenu.cs`），热键 Ctrl+Alt+1/2/3（默认关，`-xq-hotkeys` 或 `xq_mode.json`），`Esc` |
| P2-2 | 近景镜头 | WB | 写完[未编] | `XqCloseCamera.cs`（包 ME `AvatarBigScreenHandler`，不改它）|
| P2-3 | 近景界面 | DeepSeek | 已合入 `xiaoqi/p2` | 分支 `xiaoqi/p2-deepseek`；订阅 `XqMode.OnCloseReady / OnCloseLeaving` 出现和收起 |
| P2-4/5 | 延展动画、数据接入 | DeepSeek | 已合入 `xiaoqi/p2` | |
| D-1 | 近景取景调参（眼睛 43%、居中，±5%）| AGY（新 Agent）| **实机过[已编]** | `Assets/Xiaoqi/Mode/XqCloseCamera.cs`、`XqModeConfig.cs`（分支 `xiaoqi/p2-agy`）|
| D-2 | 近景界面的位置/脸区/占位与卡面裁剪修复 | AGY（新 Agent）| **实机过[已编]** | `UI2/XqCloseLayout.cs`、`XqClosePanel*.cs`、`XqClose.uss`、`xq.uss` |
| D-3 | 方钮点击与常显（方案 B）+ 近景隐藏桌宠胶囊 | AGY（新 Agent）| **实机过[已编]** | `UI2/XqPetHud.cs` |
| D-4 | 近景数据接入/断线显示「还没有」| AGY（新 Agent）| **实机过[已编]** | `UI2/XqCloseData.cs`、`XqCloseDay.cs`、`XqClosePanel.Bind.cs` |

## P3 工作台

| 步 | 内容 | 谁 | 状态 | 文件 |
|---|---|---|---|---|
| P3-0 | 网页 `bench.html` | DeepSeek | **浏览器已验证**（Grok 重截 `8d6afa74`，证据 b1-grok `c1df05be` `evidence/P3-0-v2/` 27 文件：feed 取最新一窗、断线显示连接不上、首次失败空态均过）；未部署；待改小瑕疵：「连接不上」出现时页签左移 56px，改 `visibility:hidden` 预留位置 | 主库 `server/E3-bench-page` `8d6afa74` |
| P3-1 | Edge 窗口打开/摆放/关闭 | WB | 写完[未编] | `XqBenchWindow.cs` |
| P3-2a | 摸底 | WB | **完成**（纸面）| `FACTS-P3-2a.md` |
| P3-2b | 她坐上去并跟随 | WB | 写完[未编] | `XqBenchWindow.cs` + ME 的 `AvatarWindowHandler.cs`（`XqSitOn` 等，`// XQ:`）|
| P3-3 | 两个方钮与退出 | DeepSeek + WB | 写完[未编]（WB 复核改 2 处）| `UI2/XqBenchChrome.cs`、`Resources/XqBenchChrome.uss`；DPI 按显示器取(`8aa152f4`) |

## 改动 ME 文件清单（本阶段）

1. `Assets/MATE ENGINE - Scripts/AvatarHandlers/AvatarWindowHandler.cs`：新增 `XqSnappedHwnd` / `XqSitOn` / `XqSitOff` 与字段 `_xqFixedFraction`（约 36 行）；改 2 行（`_postSettleRecalib` 回算加条件、`ClearSnapAndHide` 清标志）。

## 改动小祈文件

- `Assets/Xiaoqi/UI/XqMenu.cs`：「让她…」子菜单末尾加 3 项。
- `Assets/Xiaoqi/Pet/XqStageBody.cs`：自己睡着的条件加「当前在桌宠态」。

## 已知缺口（不挡编译，等实机/后续）

- **近景期间禁止跳舞**：ME 的跳舞由音乐触发，近景里没拦（TODO，待实机看会不会发生）。
- ~~全屏躲开 × 工作台~~：已做（`628221fc`，`XqFullscreenHide` 一起藏/显 Edge 窗口），[未编→已编 0102fd20]，待实机。
- **方钮 10/11、近景退出方式的竖栏 ♥ / 入口方钮**：属于 UI，等 DeepSeek 的近景界面。
- **`bench_pet_size`**：默认 0（不改大小），实机标定。
- **近景「进入前位置和大小恢复」**：靠 ME 自己（`originalWindowRect`）；退出后若有偏差，实机再看。
- 令牌在 Edge 命令行里（进程列表可见），同机其他程序能读到；单机桌宠场景接受，日志里已隐藏。

## 回家要看的（实机清单）

见同目录 `P2P3-SCREEN-CHECKLIST.md`。

## 集成分支与状态（WB 11-17 13:00）

- 集成分支 `xiaoqi/int-all` = p1 + p2 + int-d1(设置页/托盘) + p2-agy(近景调参) + p3-deepseek(工作台方钮)。**以它的最新提交为准编译验收。**
- 最近提交：`8aa152f4`（方钮 DPI、稳定外框死区封顶+0.25s 持续、菜单锁位容差封顶，[未编]）；`0102fd20` 已被 AGY 编过（0 error）。
- 位置换算统一走 `UI2/XqPanelXf`（这台机器 `ScreenToPanel` 不翻 Y）。**凡 UI 定位/Pick 不得直接调 `ScreenToPanel`。**
- 窗口保护 `MeHost.EnsureOnScreen`：窗口中心不在屏里时，仅桌宠态+非切换+外框正常才看身体中心；近景/工作台不动窗口。
- 主屏 100%，副屏 175%（主人 11-17 确认）。

## 验收状态

| 范围 | 已过 | 待实机（晚上，主人） |
|---|---|---|
| P1 | 点击穿透、全屏躲开、方钮常显（主人实测）| 设置页、拖动不闪回、菜单抖、坐窗睡、主题观感、退出无空指针 |
| P2 | 十项 #1–#10（WB 复核认可）| R-5 近景里开设置页 |
| P3 | P3-0 网页（浏览器）| 工作台窗口、坐标题栏、方钮（含 175% 副屏大小）、全屏联动；P3-4 稳定性 |
| 服务器 | 代码完成 | E1c、E3 未部署 |

`WB-GATE.md`：P1/P2/P3 均未写通过。
