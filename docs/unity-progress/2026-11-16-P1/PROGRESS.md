# P1 桌宠 · PROGRESS

> 规则：每步一行。状态：未开始 / 进行中 / **待验收**（代码在、证据没齐）/ 待 WB 复核 / 完成 / 卡住 / 打回。
> 「WB 意见」栏**只有 WB 能写**。执行方只写「状态」「提交」「做了什么 / 没做什么」「证据」。
> 现行分工与批次：主库 `docs/WB-G60-takeover-p1.md`。方案：`docs/WB-G59-P1-pet.md`。
> 主线分支：`xiaoqi/p1`。**不要往 `cursor/...` 分支推。**

起点：`a73dd06e`（含 P1-1、P1-2a-d、P1-3a-c 代码，B0 时核对，**未编、未跑、无证据**）。

| 步骤 | 内容 | 状态 | 提交 | 做了什么 / 没做什么 | 证据 | WB 意见 |
|---|---|---|---|---|---|---|
| P1-0 | 摸底、基线截图、FACTS、PROGRESS | 进行中 | B0 | B0 已建 PROGRESS / FACTS / ISSUES、补 PATCHES；FACTS 里「待 B1 补」的项与基线截图没做 | — | |
| P1-1 | UI Toolkit 基础与点击穿透 | **待验收** | `ede6400a` `4297c2b7` `92a76d0a` `7b54109a` | `UI2/` 目录、PanelSettings、`xq.uss`、思源黑体、阴影 9 宫格、穿透样品 | 无 | |
| P1-2a | 跳舞 | **待验收** | `f2ced130` | `xqHoldDance`；`CheckForSound` 开头 return；拖拽清除 | 无 | |
| P1-2b | 睡姿 | **待验收** | `42c71572` | `xqHold` / `XqSetSleeping`；拖醒 | 无 | |
| P1-2c | 撤「自己走」 | **待验收** | `d8ef7bfc` | 菜单项已撤；`LocomotionEnabled` 保留（B1 核对） | 无 | |
| P1-2d | 置顶、全屏躲开 | **待验收** | `b4cb5fdd` `a73dd06e` | `XqFullscreenHide`；`topmost` / `hide_on_fullscreen` 默认 true；副屏已处理 | 无 | |
| P1-3a | 气泡、胶囊、精力环 | **待验收** | `d80f4704` | `UI2/XqPetHud.cs` | 无 | |
| P1-3b | 入口方钮、三个入口 | **待验收** | `ed7e9887` | 近景 / 工作台灰掉（B1 核对） | 无 | |
| P1-3c | 玻璃今天卡 | **待验收** | `ad51f40c` | `UI2/XqTodayCard.cs`；旧 uGUI 卡保留（B1 核对未删） | 无 | |
| P1-4a | 映射草案 | **主人已确认 11-16 13:05** | — | 批次 B2。留着 / 不躲边 / 困了自己睡 / 接受降级 / 要打字气泡 | — | |
| P1-4b | 停轮换 + IdleDirector | 已写，未编 | 见下（B3） | `IdleDirector` + `XqPoseBlend`（骨骼姿势淡入，不滑过中间编号）；`IDLE_SWITCH_TIME=1e9`；池默认前 10 槽 | 无 | |
| P1-4c | BodyDirector + brain.log + 悬停 | 已写，未编 | 见下（B3） | `BodyDirector` / `BrainLog` / `XqHoverReact`；验收用 `-xq-brain-fast` | 无 | |
| P1-4d | 接服务器意图 | 代码已写 `[未编]` | `xiaoqi/p1` | 批次 B7：`XqStageBody` + PetPresence 不再被每次 poll 冲掉；见 INTENT-MAP-DRAFT §8 | — | |
| P1-5a | 设置页壳 + 行为页 | 已写，未编（DeepSeek 分支 `xiaoqi/b4-deepseek`）| D1/D2 | 6 页框架 + 行为/外观/显示；WB 审查通过 | 7 处 `TODO(编译员)` 见 B4-REPORT | |
| P1-5b | 其余 3 页 + 入口 | 已写，未编（同上，D3/D4 `863d577d`）| D3/D4 | 声音/连接/关于 + `XiaoqiSettings.Open()` 入口（默认关，`settings_v2` 或 `-settings-v2`）+ `ApplyOnStart`；启动接线在 `xiaoqi/int-d1`（`XiaoqiSystems.ApplySettingsOnStart`，仅新页启用时），随 b4 合并进 p1 一起带 | 无 | |
| P1-6 | 托盘与三个状态变体 | 已写，未编（分支 `xiaoqi/wb-b6`）| 见分支 | 托盘 6 条（隐藏/静音/安静/设置/退出）；手动隐藏 `XqFullscreenHide.ManualHidden`；外出文案「她出门了 · 约 HH:MM 回来」且「路上」也算外出。睡着/安静两种在 P1-3a 已有 | 无 | |
| P1-7 | 验收包与回归 | 未开始 | — | 批次 B8 | — | |

## 批次状态（G60）

| 批次 | 内容 | 谁 | 状态 |
|---|---|---|---|
| B0 | 补账 | WB | 完成 |
| B1 | 编译验收 | 编译员（Grok 远程 Inori02）| **进行中**：编译 + 冒烟已过（`a73dd06`，见 evidence/a73dd06-inori02/）；截图全黑无效；人工验收 8 项未做 |
| B2 | P1-4a 映射草案 | WB | 主人已确认 |
| B3 | P1-4b/c | WB | 代码已写 `[未编]`，等编译（`xiaoqi/int-d1`）|
| B4 | P1-5a | DeepSeek | 代码完成未编，WB 审查通过 |
| B5 | P1-5b | DeepSeek | 代码完成未编，WB 审查通过 |
| B6 | P1-6 | 同上 | 等 B4 |
| B7 | P1-4d | 同上 | 代码已写 `[未编]`（在 `xiaoqi/p1`），等编译 |
| B8 | P1-7 | 编译员 | 等 B3–B7 |

## B1 记录（WB 11-16 12:30）

Grok 在 Inori02（Windows，1920×1080 / 100%）用 `tools/xq-verify.ps1 -NoGit` 对 `a73dd06` 跑：

| 项 | 结果 | 依据 |
|---|---|---|
| 编译 | **通过**，Unity exit 0，59 秒，error CS 0，warning CS 0 | `Build Finished, Result: Success.` |
| 代码与 03:55 的包是否一致 | 是（`Assembly-CSharp.dll` 749056 字节、时间未变）| Grok 报 |
| 启动 | 4 个场景（base / nap / theme-blue / settings-v2）都活过 20 秒 | summary.md |
| 日志里的 2 条「异常」 | 都是 `Curl error 28`（连服务器超时），不是崩溃 | player-*.log |
| nap | 有 `XQ_NAP start`、`XQ_SLEEP hold` | xq-lines-nap.txt（只说明睡姿保持的代码跑到了，**没看到姿势**）|
| 主题 | `theme=blue class=xq-theme-blue` | xq-lines-theme-blue.txt |
| 截图 | **4 张全黑，无效**（疑似锁屏 / 熄屏）| smoke-*.png 未收 |
| 人工验收 8 项 | **未做**（需要有人在屏幕前）| — |

结论（WB）：**能编、能启动、没有崩溃**；P1-1~3c 的功能是否对，**仍然没有证据**，步骤状态保持「待验收」。
