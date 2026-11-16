# G65 / G66 · P2 近景第二轮验收报告（R-1 ~ R-5）

- **执行人**：AGY（编译与自动化）+ 主人（INORI02 物理桌面运行桌面脚本）
- **日期**：2026-11-17
- **被验对象**：`origin/xiaoqi/int-all` @ `f0d7ac3e`（已含 `p1` + `p2` + `int-d1` + `p2-agy` + ⚙ 接 `XiaoqiSettings.Open()`）
- **代码修改分支**：`origin/xiaoqi/p2-agy2` @ `26c1bd2`（仅改 `XqCloseCamera.cs`、`XqModeConfig.cs`、`XqClosePanel.Anim.cs`、`XqClosePanel.Controls.cs`、`XqTestHooks.cs` + 新增 `.meta`，未改 `XqMode.cs` / `XqGlassMenu.cs` / `MeHost.cs` / ME 原文件 / `WB-GATE.md`）
- **证据目录**：`docs/unity-progress/2026-11-17-P2-round2/`（追加提交到 `xiaoqi/p2-accept-agy`，未改动既有 `2026-11-17-P2-accept-agy/`）

---

## 1. 编译与工作区检查（G66 §2.1 ~ §2.2）

- **基线 `int-all` @ `f0d7ac3e` 编译**：`XiaoqiBuild.Win64` 通过，**0 error**、76 warnings、耗时 66s（`logs/build-summary-f0d7ac3.md`）。
- **`xiaoqi/p2-agy2` @ `26c1bd2` 编译**：`XiaoqiBuild.Win64` 通过，**0 error**、59 warnings、耗时 65s。
- **构建产物自动改动（按 G66 §2.2 未提交 prefab / ProjectSettings / 场景，保留 diff stat 供复核）**：
```text
 .../Windows/addressables_content_state.bin         | Bin 5761 -> 5762 bytes
 Assets/Xiaoqi/Resources/XiaoqiSystems.prefab       |  52 +++++++++++++++++++++
 ProjectSettings/ProjectSettings.asset              |   8 ++--
 Thry/presets_known_materials.txt                   |  27 +----------
 UserSettings/EditorUserSettings.asset              |   6 +--
```
（另有 `Assets/AddressableAssetsData/link.xml.meta`、`LiberationSans SDF - Fallback.asset`、`XqPanelSettings.asset`、`ProjectSettings/lilToonSetting.json` 被构建写入；新增 34 个 `.meta` 已提交在 `a9af38d`。）

---

## 2. R-1 ~ R-5 验收总表

| 编号 | 对应十项 | 结论 | 量化 / 实测依据 | 证据文件（本目录下） |
|---|---|---|---|---|
| **R-1** | #4 取景（+ #2 漂移不回升） | **通过** | 3 轮 `-xq-close-test`：`entered in=1.03s / 1.01s / 1.02s`，进入稳定后 `XQ_CLOSE eyes view=(0.50,0.55..0.56)`、退出前 `view=(0.50,0.57)`（目标 `(0.50,0.57)`，误差 ≤ 2%），3 轮后 `drift win=0,0 size=0,0 bodyCx=0 bodyBottom=0 bodyH=-1`。100%（`1904×1041`）`view=(0.50,0.56)`，175%（`1447×784`）`view=(0.50,0.55..0.57)`；两张真标准图叠图 `std/P2_近景_纯净.png` 双眼精准对齐 43% 横线与 50% 竖线 | `logs/R1-A-close-test-Player.log`、`logs/R1-100-Player.log`、`logs/R1-175-Player.log`、`shots/P2-2-close-100.png`、`shots/P2-2-close-175.png`、`shots/P2-2-overlay-std-100.png`、`shots/P2-2-overlay-std-175.png` |
| **R-2** | #7 穿透证据 | **通过** | 记事本置于近景卡片间透明区下方：真 `SendInput` 点透明区后，前景窗切为 `无标题 - Notepad` 并成功打字 `R2_OK`；再点左侧竖栏 `今` 后前景切回 `小祈`，日志触发 `XQ_CLOSE focus on` / `off`，后续 `SHOULD_NOT_TYPE_IN_NP` 未落入记事本 | `logs/DESKTOP-RUN.log`、`logs/R2-passthrough-Player.log`、`shots/R2-before.png`、`shots/R2-after-transparent-click.png`、`shots/R2-after-card-click.png` |
| **R-3** | #8 四种退出方式 | **通过（附实测 bodyH 说明）** | 均在 `16.vrm` 正常加载环境实测：① Esc：`XQ_CLOSE exit by=esc` → `Close -> Pet`，`drift win=0,0 size=0,0 bodyCx=-1 bodyBottom=-2 bodyH=4`；② ♥：`XQ_CLOSE rail pet` → `XQ_CLOSE exit by=rail` → `Close -> Pet`，`drift win=0,0 size=0,0 bodyCx=0 bodyBottom=2 bodyH=-5`；③ 桌宠钮：`XQ_CLOSE btn pet` → `XQ_CLOSE exit by=btn` → `Close -> Pet`，`drift win=0,0 size=0,0 bodyCx=0 bodyBottom=-1 bodyH=3`；④ `Ctrl+Alt+1`（`-xq-hotkeys`）：`XQ_MODE Close -> Pet`（前面无 `rail/btn/esc` 日志 = 热键），`drift win=0,0 size=0,0 bodyCx=0 bodyBottom=-1 bodyH=1`。注：Esc 与 ♥ 退出后 `win/size/bodyCx/bodyBottom` 均在 `0~2px`，`bodyH` 因 `Settle()` 接回待机微幅帧差为 `4px / -5px`（如实写） | `logs/R3-esc-Player.log`、`logs/R3-rail-Player.log`、`logs/R3-btn-Player.log`、`logs/R3-hotkey-Player.log` |
| **R-4** | #10 近景右键 / 拖 / 全屏 | **通过（现象如实记）** | ① 右键她：弹出 `XqGlassMenu`（`XQ_MENU2 open side=L`），点空白关闭（`XQ_MENU2 close by=blank`）后仍在近景，无 Exception（菜单背景较透部分压左下精力卡）；② 拖她：近景下窗口未移动未被拖走、不崩无 Exception，鼠标经过触发 `XQ_BRAIN src=local evt=hover`；③ 全屏窗口：遮挡同屏全屏窗时触发 `XQ_FS hide` 隐藏，关闭全屏窗后触发 `XQ_FS show` 恢复近景、无 Exception | `logs/R4-Player.log`、`shots/R4-1-rclick.png`、`shots/R4-1-rclick-after.png`、`shots/R4-2-drag.png`、`shots/R4-3-fullscreen.png`、`shots/R4-3-fullscreen-after.png` |
| **R-5** | P2-5 ⚙ 打开设置页 | **半（开关成立但存在集成问题）** | 点竖栏 `设` 确实打 `XQ_CLOSE settings open` 并打开设置页（`XQ_SET2 open`），关闭（`XQ_SET2 close`）后仍在近景。**暴露 2 个问题（记入 `ISSUES.md`）**：① `-settings-v2` 下打开的 `XqSettingsPanel` 严重偏下、底部被裁到屏外（仅露顶部「设置 / 外观 / 坐在窗口上」）；② 点击顶部关闭设置页时同时误触近景下的 `XqPetHud` 弹出「今日日程」（`XQ_TODAY open` + 502），且日志里 `XQ_CLOSE settings open` 触发了两次 | `logs/R5-Player.log`、`shots/R5-settings-open.png`、`shots/R5-settings-closed.png` |

---

## 3. R-1 / R-3 代码改动摘要（`xiaoqi/p2-agy2`）

- **`d8a98c6`（R-1）**：
  - `Assets/Xiaoqi/Mode/XqModeConfig.cs`：`close_yoffset` 初值标定为 `-0.015f`（原 `0.045f`）。
  - `Assets/Xiaoqi/Mode/XqCloseCamera.cs`：新增 `[DefaultExecutionOrder(200)] EyeAlignRunner`（先于渲染、后于 `FixedPosition` 执行），在进入动画淡入完成（`0.95s`）后每帧取双眼骨骼 `HumanBodyBones.LeftEye / RightEye` 中点（缺省回退 `attachBone` + 头部偏移）经 `Camera.WorldToViewportPoint` 投影，按 `cam.ViewportToWorldPoint(0.50, 0.57, vp.z)` 阻尼（`0.65f`）同步收敛 `h.YOffset` 与相对 `_savedAvatarPos` 的水平偏移 `_xShift`，并在进入稳定后和 `BeginExit()` 退出前各打一次 `XQ_CLOSE eyes view=(0.xx,0.xx) target=(0.50,0.57)`。
- **`26c1bd2`（R-3）**：
  - `Assets/Xiaoqi/UI2/XqClosePanel.Anim.cs`、`XqClosePanel.Controls.cs`：补充 `[DefaultExecutionOrder(-100)] CloseEscWatcher` 在 `XqMode.Update()` 前捕获 Esc 打 `XQ_CLOSE exit by=esc`，并在 ♥ / 「桌宠」点击路径调 `XqMode.Set(Pet)` 前打 `XQ_CLOSE exit by=rail|btn`。
  - `Assets/Xiaoqi/Mode/XqTestHooks.cs`：新增默认关闭的 `-xq-snap-mode` 测试参数，为四种真实键鼠退出记录 `XQ_TEST snap before / close / after / drift`。

---

## 4. 关键日志摘录

### R-1（`-xq-close-test` 3 轮与 100% / 175% 眼位）
```text
XQ_TEST close before win=-2560,-683 1463x823 body=604..903 y=-7..542 h=549 mode=Pet
XQ_TEST close round=1 entered in=1.03s win=-2560,-683 1463x823 ... mode=Close
XQ_CLOSE eyes view=(0.50,0.55) target=(0.50,0.57)
XQ_CLOSE eyes view=(0.50,0.57) target=(0.50,0.57)
XQ_TEST close round=2 entered in=1.01s ...
XQ_CLOSE eyes view=(0.50,0.56) target=(0.50,0.57)
XQ_CLOSE eyes view=(0.50,0.57) target=(0.50,0.57)
XQ_TEST close round=3 entered in=1.02s ...
XQ_CLOSE eyes view=(0.50,0.56) target=(0.50,0.57)
XQ_CLOSE eyes view=(0.50,0.57) target=(0.50,0.57)
XQ_TEST close round=3 exited accepted=True win=-2560,-683 1463x823 body=604..902 y=-7..542 h=549 mode=Pet
XQ_TEST close drift win=0,0 size=0,0 bodyCx=0 bodyBottom=0 bodyH=-1
```

### R-3（四种真实退出 + 热键）
```text
# Esc (R3-esc-Player.log)
XQ_CLOSE exit by=esc
XQ_MODE Close -> Pet
XQ_TEST snap drift win=0,0 size=0,0 bodyCx=-1 bodyBottom=-2 bodyH=4

# ♥ (R3-rail-Player.log)
XQ_CLOSE rail pet
XQ_CLOSE exit by=rail
XQ_MODE Close -> Pet
XQ_TEST snap drift win=0,0 size=0,0 bodyCx=0 bodyBottom=2 bodyH=-5

# 桌宠方钮 (R3-btn-Player.log)
XQ_CLOSE btn pet
XQ_CLOSE exit by=btn
XQ_MODE Close -> Pet
XQ_TEST snap drift win=0,0 size=0,0 bodyCx=0 bodyBottom=-1 bodyH=3

# Ctrl+Alt+1 (R3-hotkey-Player.log，前无 rail/btn/esc 日志)
XQ_CLOSE eyes view=(0.50,0.57) target=(0.50,0.57)
XQ_MODE Close -> Pet
XQ_CLOSE eyes view=(0.50,0.57) target=(0.50,0.57)
XQ_TEST snap drift win=0,0 size=0,0 bodyCx=0 bodyBottom=-1 bodyH=1
```
