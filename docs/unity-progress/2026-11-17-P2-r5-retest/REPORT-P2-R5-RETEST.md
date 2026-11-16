# G67 · P2 近景回归与 R-5 复测报告

- **执行人**：AGY（编译、近景回归、R-5 桌面脚本复测）
- **日期**：2026-11-17
- **被验对象**：`origin/xiaoqi/int-all` @ `701e6174`（含 `b7e7ba88` `XqPanelXf` / 设置页与菜单修复）
- **`.meta` 提交分支**：`origin/xiaoqi/p2-agy3` @ `118532b`（仅新增 `Assets/Xiaoqi/UI2/XqPanelXf.cs.meta`）
- **证据目录**：`docs/unity-progress/2026-11-17-P2-r5-retest/`

---

## 1. 编译与近景回归（G67 §6）

- **编译**：`XiaoqiBuild.Win64` 通过，**0 error**、76 warnings、耗时 69s（`logs/build-summary-701e617.md`）。未提交 prefab / ProjectSettings / 场景改动。
- **`-xq-close-test` 近景回归**（`logs/R1-regression-close-test-Player.log`）：
  - `XQ_PANEL ScreenToPanel flipsY=False origin=0.0,0.0 H=1440`（首帧探出不翻 Y，只出现一次）。
  - 3 轮进入：`1.04s`、`1.01s`、`1.06s`（≤ 1.3s）；`XQ_CLOSE eyes` 进入稳定后 `view=(0.50,0.57)/(0.50,0.55)/(0.50,0.56)`，退出前三轮均为 `view=(0.50,0.57)`（目标 `(0.50,0.57)`，误差 ≤ 2%）。
  - 最终漂移：`XQ_TEST close drift win=0,0 size=0,0 bodyCx=0 bodyBottom=0 bodyH=-1`（通过）。

---

## 2. R-5（近景 ⚙ 打开设置页）复测结果：**没通过**

- **已改善部分**：
  - 点击竖栏 `设` 后，日志里 `XQ_CLOSE settings open` **只输出 1 次**（去重修复生效）。
  - 之后点屏幕空白时 **不再误触** `XQ_TODAY open`（「今日日程」不再弹出）。
- **未通过现象**：
  - `XQ_SET2 open` 之后在极短时间内（同一次点击 / 下一帧）立刻打印 **`XQ_SET2 close`**，导致设置页在屏幕上无法保留可见——`shots/R5-v2-settings-open.png`、`shots/R5-v2-settings-inside.png`、`shots/R5-v2-settings-closed.png` 中均未见设置面板，且主人在 B1 中同样反馈「设置页直接无法打开，点击后直接菜单全部隐藏」。
- **关键日志（`logs/R5-v2-Player.log`）**：
```text
XQ_PANEL ScreenToPanel flipsY=False origin=0.0,0.0 H=1440
XQ_HUD gate shown=True near=False menuOpen=False rect(panel)=1367,1337 40x40 scale=1.33
XQ_CLOSE eyes view=(0.50,0.56) target=(0.50,0.57)
XQ_CLOSE settings open
XQ_SET2 doc=XqUi2Document
XQ_SET2 page 行为
XQ_SET2 volume=1.00
XQ_SET2 open
XQ_SET2 close
```
- **原因判断**：`XqSettingsPanel.Tick()` 新增 `if (!over && Input.GetMouseButtonDown(0)) Close();`，而点击 `⚙`（或右键菜单「设置」）打开设置页的同一帧，`Input.GetMouseButtonDown(0)` 仍为 `true` 且鼠标在居中的 `_panel` 之外（`!over`），导致打开当帧/次帧即自关。按 G67 规矩本次未改 `XqSettingsPanel.cs`，已记入 `ISSUES.md` #9 交 WB 修复。
