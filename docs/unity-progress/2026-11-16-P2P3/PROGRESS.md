# P2 / P3 进度（代码先行，全部 [未编]）

> 11-16 主人授权：P2/P3 在没有 Unity 的情况下先写代码，回家看实机再改。关卡状态**不变**（`WB-GATE.md` 里 P2/P3 仍「未开始」）。
> 代码在分支 `xiaoqi/p2`（从 `xiaoqi/p1` 切，WB 写）。不合 `xiaoqi/p1`。每项状态只有三种：**写完[未编] / 编过 / 实机过**。

## P2 近景

| 步 | 内容 | 谁 | 状态 | 文件 |
|---|---|---|---|---|
| P2-0 | 服务器 `POST /api/body/event` | WB | **双端已测**（WB 本机 19 项 + 真 HTTP 5 项；Grok 另机 19 项全过 EXIT=0），未部署 | 主库 `server/E1c-body-event` `fa70cb2b` |
| P2-1 | `XqMode` 三态 + `XqEventSink` + 入口 | WB | 写完[未编] | `Assets/Xiaoqi/Mode/XqMode.cs`、`XqEventSink.cs`、`XqModeConfig.cs`；入口：右键「让她…」子菜单末尾 3 项（`XqMenu.cs`），热键 Ctrl+Alt+1/2/3（默认关，`-xq-hotkeys` 或 `xq_mode.json`），`Esc` |
| P2-2 | 近景镜头 | WB | 写完[未编] | `XqCloseCamera.cs`（包 ME `AvatarBigScreenHandler`，不改它）|
| P2-3 | 近景界面 | DeepSeek | 未开始 | 分支 `xiaoqi/p2-deepseek`；订阅 `XqMode.OnCloseReady / OnCloseLeaving` 出现和收起 |
| P2-4/5 | 延展动画、数据接入 | DeepSeek | 未开始 | |

## P3 工作台

| 步 | 内容 | 谁 | 状态 | 文件 |
|---|---|---|---|---|
| P3-0 | 网页 `bench.html` | DeepSeek | **浏览器已验证**（Grok 重截 `8d6afa74`，证据 b1-grok `c1df05be` `evidence/P3-0-v2/` 27 文件：feed 取最新一窗、断线显示连接不上、首次失败空态均过）；未部署；待改小瑕疵：「连接不上」出现时页签左移 56px，改 `visibility:hidden` 预留位置 | 主库 `server/E3-bench-page` `8d6afa74` |
| P3-1 | Edge 窗口打开/摆放/关闭 | WB | 写完[未编] | `XqBenchWindow.cs` |
| P3-2a | 摸底 | WB | **完成**（纸面）| `FACTS-P3-2a.md` |
| P3-2b | 她坐上去并跟随 | WB | 写完[未编] | `XqBenchWindow.cs` + ME 的 `AvatarWindowHandler.cs`（`XqSitOn` 等，`// XQ:`）|
| P3-3 | 两个方钮与退出 | — | 未开始（Esc 回近景已在 `XqMode`；方钮待 P2-3 的 UI 风格定了再做）| |

## 改动 ME 文件清单（本阶段）

1. `Assets/MATE ENGINE - Scripts/AvatarHandlers/AvatarWindowHandler.cs`：新增 `XqSnappedHwnd` / `XqSitOn` / `XqSitOff` 与字段 `_xqFixedFraction`（约 36 行）；改 2 行（`_postSettleRecalib` 回算加条件、`ClearSnapAndHide` 清标志）。

## 改动小祈文件

- `Assets/Xiaoqi/UI/XqMenu.cs`：「让她…」子菜单末尾加 3 项。
- `Assets/Xiaoqi/Pet/XqStageBody.cs`：自己睡着的条件加「当前在桌宠态」。

## 已知缺口（不挡编译，等实机/后续）

- **近景期间禁止跳舞**：ME 的跳舞由音乐触发，近景里没拦（TODO，待实机看会不会发生）。
- **全屏躲开 × 工作台**：别的程序全屏时，网页窗口应一起藏。现在只有她藏，Edge 窗口不动（TODO，`XqFullscreenHide` 要加回调）。
- **方钮 10/11、近景退出方式的竖栏 ♥ / 入口方钮**：属于 UI，等 DeepSeek 的近景界面。
- **`bench_pet_size`**：默认 0（不改大小），实机标定。
- **近景「进入前位置和大小恢复」**：靠 ME 自己（`originalWindowRect`）；退出后若有偏差，实机再看。
- 令牌在 Edge 命令行里（进程列表可见），同机其他程序能读到；单机桌宠场景接受，日志里已隐藏。

## 回家要看的（实机清单）

见同目录 `P2P3-SCREEN-CHECKLIST.md`。
