提交：26c1bd2（xiaoqi/p2-agy2，基于 int-all f0d7ac3e）   基于：xiaoqi/int-all f0d7ac3e
编译：通过（0 个错误，76 条警告），耗时 66s（f0d7ac3e 基线）；修正 R-1/R-3 后再编通过（0 个错误，59 条警告，耗时 65s）
编译错误清单：无
你修了哪些：未改动 P1/B1 逻辑（严格遵守 G66 只修纯编译错误，B1 问题全记入 ISSUES.md）；P2 R-1/R-3 修改在 xiaoqi/p2-agy2：a9af38d（新增 .meta）、d8a98c6（XqCloseCamera.cs / XqModeConfig.cs）、26c1bd2（XqClosePanel.Anim.cs / XqClosePanel.Controls.cs / XqTestHooks.cs）
运行：能启动（`E:\dsh-pet-projects\Xiaoqi-ME-p2\Build\Xiaoqi\Xiaoqi.exe`，`evidence/2026-11-17-B1/B1-user-Player.log`、`evidence/2026-11-17-B1/B1-user-brain.log`）
窗口实际尺寸、位置（100% 和 175%）：主屏 DISPLAY1 1920×1080（100% 缩放）；副屏 DISPLAY2 -2560,-683 2560×1440（175% 缩放，逻辑 1463×823）；用户实测桌宠窗 W=2557 H=1440 / 0..1920（见 B1-user-Player.log）

---

## 构建自动改动文件（未提交 prefab / ProjectSettings，按 G66 §2.2 贴出 diff stat）

```text
 .../Windows/addressables_content_state.bin         | Bin 5761 -> 5762 bytes
 Assets/Xiaoqi/Resources/XiaoqiSystems.prefab       |  52 +++++++++++++++++++++
 ProjectSettings/ProjectSettings.asset              |   8 ++--
 Thry/presets_known_materials.txt                   |  27 +----------
 UserSettings/EditorUserSettings.asset              |   6 +--
```

---

## B1 人工八项验收表（由主人在 INORI02 屏幕前逐项实测并答复）

启动参数：`-settings-v2 -fake-stage walk_in -fake-day .../sample.json`（及各自验证项）

| # | 项 | 结果 | 依据 / 主人原话与现场证据 |
|---|---|---|---|
| 1 | P1-1 点击穿透 | **通过** | 主人实测答复：「1，没啥问题」。 |
| 2 | P1-1 主题切换（含 .xq-body 白底与 UI 缩放定位复看） | **没通过** | 主人实测答复：「2 问题很大，点击菜单的时候，如果人物很小，那菜单就飞上去了，如果人物很大，菜单就在下面切大部分都看不到。」附图 `evidence/2026-11-17-B1/B1-shot1-small-menu-flyup.jpg`（变小时菜单飞上方与二级脱节）、`B1-shot2-large-settings-clipped.jpg`（人物大时设置页沉到屏幕底端被裁，仅露「设置/外观/坐在窗口上」）。蓝/紫主题换色未能在问题界面下完成独立确认。 |
| 3 | P1-2a 跳舞 | **无法验证** | 构建日志明确 `XQ_HOST dances=0`（无外部舞蹈资源文件）；主人实测：「3 舞蹈1有响应，其他不行」。按 G66 §3/§5 计入「无法验证：无舞蹈资源（仅舞 1 有响应）」。 |
| 4 | P1-2b 睡姿 | **没通过** | 主人实测答复：「4 坐在窗口上的时候菜单完全错误，而且点什么基本都是错了没反应」。结合 `B1-shot3-sit-menu-no-gate.jpg` 与日志 `click l2/p1 row='一直' picked=null`，坐窗口时右键菜单飞到左上方且命中错位，无法正常点出「睡一会 / 叫醒」与先下窗再睡联动。 |
| 5 | P1-2c 菜单（顶部无三段开关、贴内侧、二级外展、坐窗可开） | **没通过** | 主人实测答复：「5 点击没啥反应，而且窗户乱飘，一二级按钮对应都是错误的，这里有个修法就是理论上当他不在自主行动的时候，菜单没必要一直晃动，应该是她自己在走动，可以理解为他这个单位的长方体在移动的时候，菜单才跟着一动，不然一直晃，过于敏感。」实测日志印证 `p0=946,222` 与 `p1=946,866` Y 轴反向、`blank-click` 与行点击错位；顶部确已无三段开关（见图 1/3），但位置飘飞、随待机晃动且命中错乱。 |
| 6 | P1-2d 全屏躲开 | **通过** | 主人实测答复：「6 生效」（另有 `R4-Player.log` 实证 `XQ_FS hide` / `XQ_FS show`）。 |
| 7 | P1-3 桌宠界面（右下常显方钮、三药丸、100%/175% P1 标准图叠图） | **没通过** | 主人实测答复：「7 按钮直接没有看到」。见 `evidence/2026-11-17-B1/B1-shot3-sit-menu-no-gate.jpg`：右下角看不见入口方钮；日志 `XQ_HUD gate shown=True rect(panel)=1041,78 40x40 scale=1.31` 显示 `y=78` 跑到了屏幕上部而非脚边 82%。因方钮不可见且界面异常，未执行三药丸与 P1 标准图合格叠图。 |
| 8 | 旧功能不退化 + 设置页/托盘 | **没通过** | 主人实测答复：「8 基本都有各种问题」。结合截图与日志：`XqSettingsPanel` 沉在底部被裁、近景点空白关设置页会误触 `XQ_TODAY open`、桌宠/坐窗下菜单飞偏与点击命中失灵、`B1-user-Player.log` 记录多处 `picked=null` 与 `drag_end` 误触。 |

---

## 没通过的详细现象与日志定位（已同步记入 `ISSUES.md` #4~#8）

1. **方钮 / 菜单 / 设置页整体纵坐标错位（高缩放或透明全屏下跑飞 / 沉底被裁）**：
   - `B1-user-Player.log`：`XQ_HUD gate shown=True ... rect(panel)=1041,78 40x40 scale=1.31`，`_gate` 理论应在她脚边 82%，实际画到了面板顶部 `y=78`；
   - `B1-user-Player.log`：`XQ_MENU2 blank-click none p0=946,222 p1=946,866 H=1440 picked=null mouse=1253,294 l1=566,341 244x291`，可见 `RuntimePanelUtils.ScreenToPanel`（`p0`）与手动公式 `(Screen.height - m.y)/sc`（`p1`）在 Y 轴方向相反，导致点击位置与渲染位置不重合；
   - `XqSettingsPanel.Place()` 同源把面板压到底部被裁出屏幕（见 `B1-shot2-large-settings-clipped.jpg` 与 R-5 `R5-settings-open.png`）。
2. **菜单在待机期间跟随身体包围盒抖动**：
   - 坐窗与待机动作期间每帧按瞬时 `ScreenRect` 重算位置导致菜单漂移、点不准。主人建议仅在单位整框自主移动时刷新菜单坐标，待机微动锁死菜单。
3. **无外部舞蹈资源**：
   - `XQ_HOST dances=0`，仅舞 1 有响应，其余舞项无对应动作/音频资源。

---

## 旧 Xiaoqi.zip（11-16 03:55）是从哪个提交编的

不知道（非本次接手构建，未见对应产物清单）。

---

## 第二次实测（`int-all @ 701e6174`，WB-G67 复测）

- **被验提交**：`origin/xiaoqi/int-all` @ `701e6174`（含 `b7e7ba88` `XqPanelXf` / 菜单锁外框 / 设置页去重）；`xiaoqi/p2-agy3` @ `118532b`（仅新增 `XqPanelXf.cs.meta`）。
- **编译**：**过**（0 个错误，76 条警告，耗时 69s，见 `evidence/2026-11-17-B1-r2/build-summary-701e617.md`）。未改动任何定位逻辑文件。
- **证据文件**：`evidence/2026-11-17-B1-r2/B1-round2-Player.log`、`B1-round2-brain.log`、`build-summary-701e617.md`。

### B1 `#2 #4 #5 #7 #8` 复测结果表（按主人屏幕前实测逐项记录）

| # | 项 | 结果 | 主人原话与现场/日志依据 |
|---|---|---|---|
| 2 | P1-1 主题切换（及大小/上下定位复看） | **没通过** | 主人原话：「1 是，都是还是会有轻微抖动，而且还有个问题就是她本地待机好像也会抖动，这导致放大或者近景的情况下会抽搐连带着菜单。」且「6 无法打开设置无法查看」（设置页点完秒关，无法验证主题下设置页；另放大/近景下待机抽搐连带菜单仍未稳）。 |
| 4 | P1-2b 睡姿（含坐窗/拖动边界） | **没通过** | 主人原话：「2 严重问题，当拖动超过屏幕中间以上，他会闪现回屏幕下面中间，就是在两点间疯狂闪动，而且无法拖动到边缘，会被直接拽回中间。」拖动被强拽回下方中间致无法正常拖到上方窗口/边缘完成坐窗与睡姿复测。 |
| 5 | P1-2c 菜单（不飞/锁外框/命中） | **没通过** | 主人原话：「3 前面回答过了，是好了点，但还是会有过于敏感的抖动，其实我怀疑这部分还是xiaoqi本身的轻微抖动放大到了菜单上。」菜单位置已不再上下镜像飞天、日志行点击命中 `XQ_MENU2 click` 均有行名（不再 `picked=null`），但开菜单待机时仍出现敏感轻微抖动（日志中 `scale` 在 `1.24 / 1.25 / 1.29 / 1.32` 间随 `bodyH` 波动）。 |
| 7 | P1-3 桌宠界面（方钮与三药丸） | **通过** | 主人原话：「4 可以点击」。日志实证 `XQ_HUD gate shown=True rect(panel)=1109,1047 40x40 scale=1.25`（已落入面板下半部脚边），`XQ_HUD gate-click open=True`、`XQ_HUD entry-click view=Close`。（注：未补 100%/175% `P1_桌宠_纯净.png` 叠图。） |
| 8 | 旧功能不退化 + 设置页/托盘 | **没通过** | 主人原话：「5 设置页直接无法打开，点击后直接菜单全部隐藏」「6 无法打开设置无法查看」「要慎重的是小祈抖动这里，我感觉本身自带抖动以及动作是可以理解的，但关于放大之后怎么处理以及菜单怎么跟随需要思考」。日志确认点击菜单「设置」后同次/瞬时触发 `XQ_SET2 open` → `XQ_SET2 close`，并记录退出时 `AvatarWindowHandler.SetOtherQuadsActive` 空指针 `NullReferenceException`。 |

### WB 指定三类关键日志摘录（`evidence/2026-11-17-B1-r2/B1-round2-Player.log`）

1. **`XQ_PANEL`**：
```text
XQ_PANEL ScreenToPanel flipsY=False origin=0.0,0.0 H=1440
```
2. **`XQ_MENU2 place` / `click`**：
```text
XQ_MENU2 place body(screen)=1159..1415 y=6..459 H=1440 tl(panel)=918,777 l1=662,791 scale=1.26
XQ_MENU2 place body(screen)=1164..1420 y=39..492 H=1440 tl(panel)=882,718 l1=626,732 scale=1.32
XQ_MENU2 place body(screen)=1167..1414 y=13..472 H=1440 tl(panel)=944,783 l1=1156,798 scale=1.24
XQ_MENU2 place body(screen)=1181..1434 y=-16..451 H=1440 tl(panel)=915,766 l1=1124,768 scale=1.29
XQ_MENU2 click l1/p1 row='设置' picked=VisualElement.xq-mrow.xq-mrow-set
XQ_MENU2 click l1/p1 row='让她…' picked=VisualElement.xq-mrow
XQ_MENU2 click l2/p1 row='跳舞' picked=VisualElement.xq-mrow
XQ_MENU2 click l2/p1 row='舞 1' picked=Label.unity-text-element.unity-label.xq-mchip
```
3. **`XQ_HUD gate`（与设置页秒关日志）**：
```text
XQ_HUD gate shown=True near=False menuOpen=False rect(panel)=1369,1337 40x40 scale=1.33
XQ_HUD gate shown=True near=False menuOpen=False rect(panel)=1096,1052 40x40 scale=1.25
XQ_HUD gate shown=True near=True menuOpen=False rect(panel)=1109,1047 40x40 scale=1.25
XQ_HUD gate-click open=True
XQ_HUD entry-click view=Close
XQ_SET2 open
XQ_SET2 close
```
4. **退出时异常堆栈（`Player.log` 末尾）**：
```text
NullReferenceException: Object reference not set to an instance of an object.
  at UnityEngine.GameObject.get_activeSelf () ...
  at AvatarWindowHandler.SetOtherQuadsActive (System.Int32 activeCount) ... AvatarWindowHandler.cs:858
```

---

## 第三次实测（`int-all @ a5fe7f0c`，WB-G68 复测）

- **被验提交**：`origin/xiaoqi/int-all` @ `a5fe7f0c`（含 `c222ee9`）；`xiaoqi/p2-agy4` @ `c6938c3`（仅补生成 `Assets/StreamingAssets/Xiaoqi/fake_day.meta`，未改任何代码）。
- **编译**：**过**（0 个错误，0 条警告，耗时 41s，`XiaoqiBuild.Win64`，见 `evidence/2026-11-17-B1-r3/build-summary-a5fe7f0.md`）。
- **验证边界说明**：主人在公司无法操作家里物理桌面（`WinSta0\Default`）；AGY 终端运行在 Antigravity 隔离桌面中，实测 `CopyFromScreen` 报错 `The handle is invalid`，且 `SendInput` 无法驱动物理桌面窗口。经主人确认先推进项目（方案 1）：已完成**编译**、**`-xq-close-test` 自动化回归**、`Xiaoqi.exe` 隔离启停日志检查；需物理桌面键鼠交互与视觉观感确认的项按实标为「**无法验证**」。
- **证据文件**：`evidence/2026-11-17-B1-r3/build-summary-a5fe7f0.md`、`evidence/2026-11-17-B1-r3/R1-regression-close-test-Player.log`、`evidence/2026-11-17-B1-r3/B1-r3-quit-Player.log`。

### G68 §3 六项与 R-5 复测结果表

| 项 | 结果 | 实测依据与原因 |
|---|---|---|
| 设置页（主菜单点设置停住、点内外关闭） | **无法验证** | 隔离桌面无法向物理桌面注入 `SendInput` 且 `CopyFromScreen` 报 `The handle is invalid`，待物理桌面实测。 |
| 拖动（屏幕边缘 / 跨双屏不闪回） | **无法验证**（另发现回归） | 物理桌面拖动无法模拟；但 `-xq-close-test` 自动化回归已实锤 `MeHost.EnsureOnScreen` 在 `Close` 态下把副屏窗口强拉回主屏（见下方日志与 `ISSUES.md` #11）。 |
| 菜单抖（开菜单待机 10s + 放大缩小） | **无法验证** | 需物理桌面交互与肉眼观察待机微抖/缩放观感。 |
| 坐窗睡（坐窗口右键 + 睡一会联动） | **无法验证** | 需物理桌面其他窗口配合与交互点击。 |
| 主题（`-xq-theme blue` / `violet`） | **无法验证** | 需物理桌面肉眼确认配色观感；默认 `pink` 启动日志为 `XQ_UI2 on theme=pink class=xq-theme-pink scale=1.33 bodyH=484 fontPx=21.3`。 |
| 退出空指针（`SetOtherQuadsActive`） | **无法验证** | 隔离桌面报 `Failed to add system tray icon`，`CloseMainWindow` 未触发托盘退出且未在物理桌面坐窗后退出；但 `-xq-close-test` 通过 `Application.Quit()` 正常退出（ExitCode 0）日志中**未再出现** `NullReferenceException` / `SetOtherQuadsActive` 异常，只读源码见 `AvatarWindowHandler.cs:857` 已补 `if (otherQuadGOs[i] == null) continue;`。 |
| R-5（近景 `⚙` 开设置页脚本） | **无法验证** | 依赖物理桌面 `SendInput` 与 `CopyFromScreen` 全屏截图，隔离桌面下 `CopyFromScreen` 报 `The handle is invalid`。 |

### `-xq-close-test` 发现的新阻断回归（交 WB 修 `MeHost.cs`）

`MeHost.EnsureOnScreen()` 改为检查身体中心点 `pt = (wr.left + bodyR.center.x, wr.top + Screen.height - bodyR.center.y)` 是否命中 `MonitorFromPoint(pt)`。在进入 `Close` 态放大人物或退出过渡帧时，`bodyR` 会大幅超出屏幕矩形，导致 `pt` 落到显示器外（`pt=-1325,5792`、`1055,1452`、`1082,12658`、`767,7184`），`MonitorFromPoint(pt)` 返回 `Zero`，反复触发 `XQ_WATCH body outside` → `XQ_HOST fix window` → `MoveToCenter()`（`XQ_HOST center -327,-447 2574x1479`），直接把窗口从副屏（`-2567,-683`）强行拽回主屏（`-327,-447`），造成 `XQ_TEST close drift win=2240,236`。

关键日志摘录（`evidence/2026-11-17-B1-r3/R1-regression-close-test-Player.log`）：
```text
XQ_PANEL ScreenToPanel flipsY=False origin=0.0,0.0 H=1440
XQ_UI2 on theme=pink class=xq-theme-pink scale=1.33 bodyH=484 fontPx=21.3 scaleMode=ConstantPixelSize clearColor=False clearDepth=False hook=GetOnOpaquePixel
XQ_TEST close before win=-2567,-683 2574x1479 body=1172..1427 y=6..462 h=455 mode=Pet
XQ_MODE Pet -> Close
XQ_TEST close round=1 enter accepted=True
XQ_WATCH body outside pt=-1325,5792
XQ_HOST fix window inside=False tiny=False rect=-2567,-683
XQ_HOST resize 2574x1479
XQ_HOST center -327,-447 2574x1479
XQ_TEST close round=1 entered in=1.03s win=-327,-447 2574x1479 body=-82..2722 y=-6047..-1809 h=4238 mode=Close
XQ_CLOSE eyes view=(0.50,0.57) target=(0.50,0.57)
XQ_WATCH body outside pt=1055,1452
XQ_HOST fix window inside=False tiny=False rect=-327,-447
XQ_HOST center -327,-447 2574x1479
...
XQ_WATCH body outside pt=1082,12658
XQ_HOST fix window inside=False tiny=False rect=-327,-447
XQ_HOST center -327,-447 2574x1479
XQ_TEST close round=3 exited accepted=True win=-327,-447 2574x1479 body=1171..1427 y=7..461 h=455 mode=Pet
XQ_TEST close drift win=2240,236 size=0,0 bodyCx=0 bodyBottom=0 bodyH=-1
```

---

## 第四次（自动化部分）（`int-all @ 628221fc`，WB-G69）

- **被验提交**：`origin/xiaoqi/int-all` @ `628221fc`（含 `17c96fda` 窗口保护修正与 `628221fc` 全屏躲开联动工作台）；`xiaoqi/p2-agy4` @ `628221f`（0 新增 `.meta` 需提交，未改任何逻辑文件）。
- **编译**：**过**（0 个错误，49 条警告，耗时 61s，`XiaoqiBuild.Win64`，见 `evidence/2026-11-17-B1-r4/build-summary-628221f.md`）。
- **P3-3（DeepSeek `xiaoqi/p3-deepseek`）**：**未到**（查询 `origin` 尚未出现 `xiaoqi/p3-deepseek` 分支）。
- **证据文件**：`evidence/2026-11-17-B1-r4/`（`build-summary-628221f.md`、`close-test-628221f-Player.log`、`smoke30s-628221f-Player.log`、`nap-test-628221f-Player.log`、`sit-test-628221f-Player.log`、`theme-blue-628221f-Player.log`、`theme-violet-628221f-Player.log`、`ACCEPT-TONIGHT.md`）。

### 1. `-xq-close-test` 回归：**通过**

- `XQ_PANEL ScreenToPanel flipsY=False origin=0.0,0.0 H=1440`（仅出现 1 次）。
- 3 轮切入耗时：`1.02s`、`1.01s`、`1.01s`（全部 ≤ 1.3s）。
- `XQ_CLOSE eyes`：3 轮稳定 `(0.50,0.57)` / `(0.50,0.55)` / `(0.50,0.57)`，退出前均 `(0.50,0.57)`（目标 `(0.50,0.57)`）。
- 最终漂移：`XQ_TEST close drift win=0,0 size=0,0 bodyCx=0 bodyBottom=0 bodyH=-1`。
- 整段日志 **0** 条 `XQ_WATCH body outside`、**0** 条 `XQ_HOST fix window`、**0** 条 `XQ_HOST center`（`17c96fda` 确已消除 G68 的副屏拉回漂移回归）。

### 2. 自动冒烟：**通过**

| 用例 | 结果 | 关键日志（`evidence/2026-11-17-B1-r4/`） |
|---|---|---|
| 启动 30 秒无 Exception | **通过** | `smoke30s-628221f-Player.log`（198 行）：仅 `OperationException : Failed to initialize localization, could not preload string tables.`，无业务/运行时 `Exception` 或 `NullReferenceException`；`XQ_PANEL flipsY=False` 出现 1 次。 |
| `-xq-nap-test` | **通过** | `nap-test-628221f-Player.log`：`XQ_TEST nap start` → `XQ_TEST nap asleep=True` → `XQ_TEST nap woke asleep=False`（0 异常）。 |
| `-xq-sit-test`（目标窗 `Untitled - Notepad`） | **通过** | `sit-test-628221f-Player.log`：`XQ_TEST sit ok=True sitting=True` → 1/3/5s `snapped=True sitting=True` → `XQ_TEST sit exit sitting=False`（0 异常）。 |
| `-xq-theme blue` / `violet` | **通过** | 分别打印 `XQ_UI2 theme=blue class=xq-theme-blue` 与 `XQ_UI2 theme=violet class=xq-theme-violet`（`scale=1.33 bodyH=484 fontPx=21.3`），各含 1 次 `XQ_PANEL flipsY=False`（0 异常）。 |

### 3. 物理桌面六项（均**待晚上主人验收**）

| 序号 | 项 | 状态 | 晚上验收方式 |
|---|---|---|---|
| 1 | 设置页（主菜单/托盘/近景 `⚙`、点内外关、无今天卡） | **待晚上主人验收** | 见 `ACCEPT-TONIGHT.md` #1 |
| 2 | 拖她（上沿/左右边缘不闪回、跨双屏、无 `fix window`） | **待晚上主人验收** | 见 `ACCEPT-TONIGHT.md` #2 |
| 3 | 菜单抖（开菜单待机 10s 不抖、放大/缩小各开一次） | **待晚上主人验收** | 见 `ACCEPT-TONIGHT.md` #3 |
| 4 | 坐窗睡（坐窗顶边、右键、先下窗再睡、叫醒/拖醒） | **待晚上主人验收** | 见 `ACCEPT-TONIGHT.md` #4 |
| 5 | 主题（`-xq-theme blue \| violet` 各元素观感） | **待晚上主人验收** | 见 `ACCEPT-TONIGHT.md` #5 |
| 6 | 坐窗口后托盘退出（无 `SetOtherQuadsActive` 空指针） | **待晚上主人验收** | 见 `ACCEPT-TONIGHT.md` #6 |

### 4. 晚上验收包位置与说明

- **固定可运行路径**：`E:\dsh-pet-projects\Xiaoqi-Tonight\Xiaoqi\Xiaoqi.exe`
- **操作清单与一键脚本**：`E:\dsh-pet-projects\Xiaoqi-Tonight\ACCEPT-TONIGHT.md`（配套 `1-启动验收(pink).cmd`、`2-主题blue.cmd`、`3-主题violet.cmd`），清单副本同步存于 `evidence/2026-11-17-B1-r4/ACCEPT-TONIGHT.md`。

---

## 第五次（自动化）（`int-all @ 0102fd20`，WB-G70）

- **被验提交**：`origin/xiaoqi/int-all` @ `0102fd20`（含 `3d53968` P3-3 工作台方钮、`70c5a6a`、`0102fd2` WB 复核修复）；`xiaoqi/p2-agy4` @ `c04093d`（仅补生成 `XqBenchChrome.cs.meta`、`XqBenchChrome.uss.meta`，未改任何代码逻辑）。
- **编译**：**过**（0 个错误，76 条警告，耗时 63s，`XiaoqiBuild.Win64`，见 `evidence/2026-11-17-B1-r5/build-summary-0102fd2.md`）。
- **证据文件**：`evidence/2026-11-17-B1-r5/`（`build-summary-0102fd2.md`、`close-test-0102fd2-Player.log`、`bench-chrome-0102fd2-Player.log`、`ACCEPT-TONIGHT.md`）。

### 1. `-xq-close-test` 回归：**通过**

- `XQ_PANEL ScreenToPanel flipsY=False origin=0.0,0.0 H=1440`（仅出现 1 次）。
- 3 轮切入耗时：`1.03s`、`1.01s`、`1.01s`（全部 ≤ 1.3s）。
- `XQ_CLOSE eyes`：3 轮稳定 `(0.50,0.57)` / `(0.50,0.55)` / `(0.50,0.55)`，退出前均 `(0.50,0.57)`（目标 `(0.50,0.57)`）。
- 最终漂移：`XQ_TEST close drift win=0,0 size=0,0 bodyCx=0 bodyBottom=0 bodyH=-1`。
- 整段日志 **0** 条 `XQ_WATCH body outside`、**0** 条 `XQ_HOST fix window`、**0** 条 `XQ_HOST center`；仅 Unity Localization 预加载常规日志，0 业务异常。

### 2. `-xq-bench-chrome-always` 方钮日志与截图说明

- **方钮日志**：**有**（`evidence/2026-11-17-B1-r5/bench-chrome-0102fd2-Player.log`，0 运行时异常）：
```text
XQ_UI2 theme=pink class=xq-theme-pink
XQ_PANEL ScreenToPanel flipsY=False origin=0.0,0.0 H=1440
XQ_BENCH chrome doc=XqUi2Document
XQ_BENCH chrome boot always=True
XQ_BENCH chrome shown side=R rect=-173,-251 40x88
XQ_UI2 on theme=pink class=xq-theme-pink scale=1.33 bodyH=484 fontPx=21.3 scaleMode=ConstantPixelSize clearColor=False clearDepth=False hook=GetOnOpaquePixel
```
- **日志观察供 WB 参考（按 G70 未改 `XqBenchChrome.cs`）**：
  - 进程窗口位于副屏 `DISPLAY2`（2560×1440，175% 缩放），但 `XQ_BENCH chrome shown side=R rect=-173,-251 40x88` 打印的按钮宽高为 **`40x88`**（`BtnDip=40`, `40+8+40=88`），即 `GetDpiForWindow(UnityHwnd())` 在该环境下返回 96 DPI（`k=1.0`），未按 175% 放大到 `70x154`（`ProbeHwnd()` 在真打开 `XqBenchWindow` 时取 Edge HWND 或仍取 `UnityHwnd` 需晚上在实机核对）。
  - 随后将窗口 `SetWindowPos` 迁至主屏 `1920x1080` 后，因两侧均为 `side=R` 满足 `if (side == _side) return;` 未再次打印第二条 `shown` 日志。
- **方钮截图（100%/175%）**：**无**（隔离桌面 `CopyFromScreen` 报 `The handle is invalid`；尝试 `PrintWindow(..., 2)` 对 D3D11 透明窗仅导出空黑/空白色板，不含 UI 画面，故不以无效黑/白图冒充截图）。

### 3. 物理桌面八项（均**待晚上主人验收**）

| 序号 | 项 | 状态 | 晚上验收方式 |
|---|---|---|---|
| 1 | 设置页（主菜单/托盘/近景 `⚙`、点内外关、无今天卡） | **待晚上主人验收** | 见 `ACCEPT-TONIGHT.md` #1 |
| 2 | 拖她（上沿/左右边缘不闪回、跨双屏、无 `fix window`） | **待晚上主人验收** | 见 `ACCEPT-TONIGHT.md` #2 |
| 3 | 菜单抖（开菜单待机 10s 不抖、放大/缩小各开一次） | **待晚上主人验收** | 见 `ACCEPT-TONIGHT.md` #3 |
| 4 | 坐窗睡（坐窗顶边、右键、先下窗再睡、叫醒/拖醒） | **待晚上主人验收** | 见 `ACCEPT-TONIGHT.md` #4 |
| 5 | 主题（`-xq-theme blue \| violet` 各元素观感） | **待晚上主人验收** | 见 `ACCEPT-TONIGHT.md` #5 |
| 6 | 坐窗口后托盘退出（无 `SetOtherQuadsActive` 空指针） | **待晚上主人验收** | 见 `ACCEPT-TONIGHT.md` #6 |
| 7 | 工作台（右键/热键打开、右外侧双方钮、拖动跟随、点「近景」返回） | **待晚上主人验收** | 见 `ACCEPT-TONIGHT.md` #7 |
| 8 | 工作台全屏联动（他程序全屏时人与网页同藏同回、`XQ_FS hide/show`） | **待晚上主人验收** | 见 `ACCEPT-TONIGHT.md` #8 |

### 4. 重打晚上验收包（已覆盖更新）

- **固定可运行路径**：`E:\dsh-pet-projects\Xiaoqi-Tonight\Xiaoqi\Xiaoqi.exe`（已覆盖为 `0102fd20` 编译产物）
- **操作清单与脚本**：`E:\dsh-pet-projects\Xiaoqi-Tonight\ACCEPT-TONIGHT.md`（已补全第 7、8 项；`1-启动验收(pink).cmd` 已带上 `-settings-v2 -xq-hotkeys` 便于 `Ctrl+Alt+3`；副本同步存于 `evidence/2026-11-17-B1-r5/ACCEPT-TONIGHT.md`）。

---

## 第六次（自动化）（`int-all @ 98d0bb3b`，近景菜单取证与全冒烟回归）

- **被验提交**：`origin/xiaoqi/int-all` @ `98d0bb3b`（含 `8aa152f4` DPI/稳定外框、`6f758018` docs、`98d0bb3b` 近景菜单按脸区摆位）；`xiaoqi/p2-agy4` @ `98d0bb3`（0 新增 `.meta` 需补，未改任何源码逻辑）。
- **编译**：**过**（0 个错误，49 条警告，耗时 62s，`XiaoqiBuild.Win64`，见 `evidence/2026-11-17-B1-r6/build-summary-98d0bb3.md`）。
- **证据文件**：`evidence/2026-11-17-B1-r6/`（`build-summary-98d0bb3.md`、`close-test-98d0bb3-Player.log`、`smoke30s-98d0bb3-Player.log`、`nap-test-98d0bb3-Player.log`、`sit-test-98d0bb3-Player.log`、`theme-blue-98d0bb3-Player.log`、`theme-violet-98d0bb3-Player.log`、`theme-pink-98d0bb3-Player.log`、`close-face-menu-98d0bb3-Player.log`、`bench-chrome-98d0bb3-Player.log`、`ACCEPT-TONIGHT.md`）。

### 1. `-xq-close-test` 回归（对照 G68 基线）：**通过**

- `XQ_PANEL ScreenToPanel flipsY=False origin=0.0,0.0 H=1041`（仅出现 1 次）。
- 3 轮切入耗时：`1.02s`、`1.01s`、`1.01s`（全部 ≤ 1.3s）。
- `XQ_CLOSE eyes`：3 轮稳定 `(0.50,0.57)` / `(0.50,0.55)` / `(0.50,0.57)`，退出前均 `(0.50,0.57)`（目标 `(0.50,0.57)`）。
- 最终漂移：`XQ_TEST close drift win=0,0 size=0,0 bodyCx=0 bodyBottom=0 bodyH=0`（全 0）。
- 整段日志 **0** 条 `XQ_WATCH body outside`、**0** 条 `XQ_HOST fix window`、**0** 条 `XQ_HOST center`。

### 2. 全套自动冒烟：**通过**

| 用例 | 结果 | 关键日志（`evidence/2026-11-17-B1-r6/`） |
|---|---|---|
| 启动 30 秒无 Exception | **通过** | `smoke30s-98d0bb3-Player.log`（198 行）：仅 `OperationException : Failed to initialize localization, could not preload string tables.`，0 业务异常；`XQ_PANEL flipsY=False` 出现 1 次。 |
| `-xq-nap-test` | **通过** | `nap-test-98d0bb3-Player.log`：`XQ_TEST nap start` → `XQ_TEST nap asleep=True` → `XQ_TEST nap woke asleep=False`（0 异常）。 |
| `-xq-sit-test`（目标窗 `Untitled - Notepad`） | **通过** | `sit-test-98d0bb3-Player.log`：`XQ_TEST sit ok=True sitting=True` → 1/3/5s `snapped=True sitting=True` → `XQ_TEST sit exit sitting=False`（0 异常）。 |
| `-xq-theme blue \| violet \| pink` | **通过** | 分别打印 `XQ_UI2 theme=blue class=xq-theme-blue`、`XQ_UI2 theme=violet class=xq-theme-violet`、`XQ_UI2 theme=pink class=xq-theme-pink`（`scale=1.00 bodyH=350 fontPx=16.0`），各含 1 次 `XQ_PANEL`（0 异常）。 |
| `-xq-bench-chrome-always`（`8aa152f`） | 已录日志 | 本次窗口落于主屏 `H=1080`（100% DPI）：`XQ_BENCH chrome shown side=R rect=1794,326 40x88`；副屏 175% 下尺寸**待晚上主人验收**。 |

### 3. 近景菜单取证（`-xq-close-test -xq-menu-debug`）：**未触发（记入 `ISSUES.md` #12）**

- **`XQ_MENU2 place close-face side=R|L l1=…` 日志**：**无（未产生）**（见 `close-face-menu-98d0bb3-Player.log`）。
- **近景菜单打开截图**：**无法取得**。
- **原因与定位（按要求不改 `XqGlassMenu.cs` / `XqTestHooks.cs`）**：
  1. `98d0bb3b` 仅修改了 `XqGlassMenu.cs`，未在 `XqTestHooks.cs` 中添加 `-xq-close-test`（或 `-xq-menu-debug`）自动打开右键菜单的钩子；
  2. `XqGlassMenu` 仍只在检测到真实鼠标右键 `Input.GetMouseButtonDown(1) && onHer` 时打开；在 AGY 隔离桌面下 `SendInput` 与 `PostMessage(WM_RBUTTONDOWN/UP)` 无法驱动物理桌面鼠标点击与 `GetOnOpaquePixel` 命中，且隔离桌面无法截得透明 D3D11 UI 画面。该项与菜单是否压住脸区（屏宽 38–62%、屏高 18–65%）均标为「**待晚上主人验收**」。

### 4. 物理桌面验收项与晚上验收包（均**待晚上主人验收**）

- **覆盖路径**：`E:\dsh-pet-projects\Xiaoqi-Tonight\`（主程序 `E:\dsh-pet-projects\Xiaoqi-Tonight\Xiaoqi\Xiaoqi.exe`、清单 `E:\dsh-pet-projects\Xiaoqi-Tonight\ACCEPT-TONIGHT.md`）。
- **清单新增两条观察项（均标「待晚上主人验收」）**：
  - **a)** 副屏 175% 时，工作台方钮大小是否正常（上一版是 `40×88` 偏小）——**待晚上主人验收**。
  - **b)** 近景右键菜单的位置：是否在脸区右侧、不压脸、打开后不跟镜头晃——**待晚上主人验收**。
- 原八项（设置页 / 拖她 / 菜单抖 / 坐窗睡 / 主题 / 坐窗后退出 / 工作台 / 工作台全屏联动）均保持「**待晚上主人验收**」。

---

## 第七次（自动化）（`int-all @ 707e68e9`，`-xq-menu-test` 近景菜单取证与回归）

- **被验提交**：`origin/xiaoqi/int-all` @ `707e68e9`（新增 `-xq-menu-test` 验收钩子）；`xiaoqi/p2-agy4` @ `707e68e`（0 新增 `.meta`，未改任何源码）。
- **编译**：**过**（0 个错误，59 条警告，耗时 62s，`XiaoqiBuild.Win64`，见 `evidence/2026-11-17-B1-r7/build-summary-707e68e.md`）。
- **证据文件**：`evidence/2026-11-17-B1-r7/`（`build-summary-707e68e.md`、`close-menu-test-707e68e-Player.log`、`close-test-707e68e-Player.log`、`smoke30s-707e68e-Player.log`、`ACCEPT-TONIGHT.md`）。

### 1. `-xq-close-test -xq-menu-test -xq-menu-debug` 取证：**通过（三条日志全取到，drift 全 0）**

```text
XQ_MENU2 test-open mode=Close
XQ_MENU2 place close-face side=R l1=607,97 limit=960x540
XQ_TEST close drift win=0,0 size=0,0 bodyCx=0 bodyBottom=0 bodyH=0
```
- 整段日志 **0** 条 `XQ_WATCH body outside`、**0** 条 `XQ_HOST fix window`、**0** 条 `XQ_HOST center`；仅 Unity Localization 预加载常规日志，0 运行时异常。
- 坐标换算核对：`limit=960x540` 下，`rect=602,97 244x291` 处于屏宽 `62.7%–88.1%`、屏高 `18.0%–71.9%`，完全位于脸区（屏宽 `38–62%`、屏高 `18–65%`）右侧不重叠（日志验证）。
- **近景菜单打开截图**：**无法取得**（隔离桌面 `CopyFromScreen` 报 `The handle is invalid`，`PrintWindow(..., 2)` 仅输出空白白色底图无 UI 内容，已核对不冒充截图）。实机不压脸视觉表现仍标「**待晚上主人验收**」。

### 2. 无 `-xq-menu-test` 的 `-xq-close-test` 回归 + 30s 冒烟：**均通过（无回归）**

- `-xq-close-test`（`close-test-707e68e-Player.log`）：3 轮切入 `1.03s / 1.01s / 1.01s`，眼位 `(0.50,0.57)/(0.50,0.55)/(0.50,0.57)`，最终 `XQ_TEST close drift win=0,0 size=0,0 bodyCx=0 bodyBottom=0 bodyH=0`，无 `body outside / fix window / center`。
- 30s 冒烟（`smoke30s-707e68e-Player.log`，199 行）：仅 Localization 预加载常规日志，0 业务异常；`XQ_PANEL flipsY=False` 出现 1 次。

### 3. `ISSUES.md` 与晚上验收包更新

- `ISSUES.md` `#12` 已标记为 **已关闭**（已取到 `-xq-menu-test` 近景菜单摆位日志）。
- **晚上验收包路径**：`E:\dsh-pet-projects\Xiaoqi-Tonight\`（已用 `707e68e9` 覆盖更新 `Xiaoqi\Xiaoqi.exe`，[ACCEPT-TONIGHT.md] 观察项 b 已补充 `XQ_MENU2 test-open mode=Close` 与 `XQ_MENU2 place close-face side=R l1=607,97 limit=960x540`，所有物理桌面项均保持「**待晚上主人验收**」）。

---

## 第八次（自动化）（`int-all @ bdb993c1`，近景二级卡位置回归与晚上包重打）

- **被验提交**：`origin/xiaoqi/int-all` @ `bdb993c1`（修改 `XqGlassMenu.cs` 近景二级卡摆到脸区另一侧）；`xiaoqi/p2-agy4` @ `bdb993c`（0 新增 `.meta`，未改任何代码）。
- **编译**：**过**（0 个错误，49 条警告，耗时 62s，`XiaoqiBuild.Win64`，见 `evidence/2026-11-17-B1-r8/build-summary-bdb993c.md`）。
- **证据文件**：`evidence/2026-11-17-B1-r8/`（`build-summary-bdb993c.md`、`close-test-bdb993c-Player.log`、`smoke30s-bdb993c-Player.log`、`ACCEPT-TONIGHT.md`）。

### 1. `-xq-close-test` 回归 + 30s 冒烟：**均通过**

- `-xq-close-test`（`close-test-bdb993c-Player.log`）：
  - `XQ_PANEL ScreenToPanel flipsY=False origin=0.0,0.0 H=1080`（仅出现 1 次）。
  - 3 轮切入 `1.03s / 1.01s / 1.02s`，眼位 `(0.50,0.57)/(0.50,0.55)/(0.50,0.57)`（退出前均 `(0.50,0.57)`）。
  - 最终漂移：`XQ_TEST close drift win=0,0 size=0,0 bodyCx=0 bodyBottom=0 bodyH=0`。
  - 整段日志 **0** 条 `XQ_WATCH body outside`、**0** 条 `XQ_HOST fix window`、**0** 条 `XQ_HOST center`。
- 30s 冒烟（`smoke30s-bdb993c-Player.log`，198 行）：仅 Localization 预加载常规日志，0 业务异常；`XQ_PANEL flipsY=False` 出现 1 次。

### 2. 晚上验收包更新

- **固定路径**：`E:\dsh-pet-projects\Xiaoqi-Tonight\`（主程序 `E:\dsh-pet-projects\Xiaoqi-Tonight\Xiaoqi\Xiaoqi.exe` 已覆盖为 `bdb993c1` 构建）。
- `E:\dsh-pet-projects\Xiaoqi-Tonight\ACCEPT-TONIGHT.md` 观察项 b 已补一句：「点开二级菜单时，二级卡应出现在脸的另一侧，不压一级卡也不压脸」，所有物理项均标「**待晚上主人验收**」。

---

## 第八次补充：主人回家实机验收结果（`int-all @ bdb993c1`，物理桌面）

- **被验对象**：`E:\dsh-pet-projects\Xiaoqi-Tonight\Xiaoqi\Xiaoqi.exe`（`int-all @ bdb993c1`），由主人在物理桌面亲自实测 `pink`（`real-user-Player-prev.log`）与 `blue`（`real-user-Player.log`）两轮，并附 3 张现场截图（`evidence/2026-11-17-B1-real/`）。

### 实机逐项结果表（按主人 1~8 反馈与现场日志对照）

| 序号 | 项 | 结果 | 主人实测原话与现场截图/日志依据 |
|---|---|---|---|
| 1 | 设置页（及近景交互） | **没通过** | 主人反馈：「1 设置直接遮住人了而且很大很违和，其次每次点击菜单上面的时候，就会类似于点击本体一样，本体会动一下然后导致菜单页页跟着变化，哪怕是两者没有重叠。近景状态下本体还是会抽搐抖动，但有些时候却没有问题，抖动主要是出现在做动作的时候，还有近景下仍能右键呼出桌宠的菜单，很违和，我认为应该像手机平板一样的继承在左边一列或者像mac一样集成在下面一列。不过确实不弹今日日程。」日志实证：点击菜单/面板同时触发 `XQ_HUD down` + `XQ_BRAIN evt=drag_end`（偶发 `XQ_TODAY open`），`real-shot1`/`real-shot3` 显示设置页过大盖住小祈且在近景/高缩放下左侧竖栏（`real-shot2`）与面板均被过度放大，近景确已不再误弹「今日日程」。 |
| 2 | 拖她（边界/跨双屏） | **通过** | 主人反馈：「2 问题解决」。两份真实运行日志均 **0** 条 `XQ_HOST fix window` / `XQ_HOST center` / `XQ_WATCH body outside`。 |
| 3 | 菜单抖/缩放/一二级卡 | **没通过** | 主人反馈：「3 当人在屏幕左边的时候，一级卡显示没问题，但是二级卡特别是今天和设置两个依旧会出现在不正确的地方，设置参考问题反馈1，今天默认出现在左边。滚动放大依旧有问题，我认为设置等一二级卡应该保持一个大小区间，不应该跟着人物比例去走，甚至可以固定一个相对舒适的区间。不然就会出现过大甚至显示一半的问题」。日志实证 `scale` 在 `0.81`~`2.00` 间随人物尺寸剧烈波动（`real-shot3` 右侧裁切），近景/贴边下设置与今天卡落位偏左/过大遮挡。 |
| 4 | 坐窗睡 | **没通过** | 主人反馈：「4 坐在窗口上时点击睡觉会掉下窗口，应该直接在坐的窗口上睡觉，理论上一个逻辑只不过换了动作而已，我理解理论常驻就是坐窗的几个动作以及待机的几个动作，在这几个基础动作的情况下切换不应该改变前置。」目前点击「睡一会」仍触发先下窗再睡，未在窗顶边直接切睡姿。 |
| 5 | 全屏联动（视频全屏） | **通过** | 主人反馈：「5 看着没什么问题」。`real-user-Player-prev.log` 记录到两轮 `XQ_FS hide` → `XQ_FS show`。 |
| 6 | 工作台（第 7 项） | **无法验证（启动脚本漏参数）** | 主人反馈：「6 点击工作台依旧未开放」。核查发现之前发出的 `.cmd` / 命令行漏传 `-xq-bench`（`XqModeConfig.cs:73` 默认 `bench_enabled = false`），致工作台入口灰置；已更新 `Xiaoqi-Tonight` 三个 `.cmd` 补上 `-xq-bench` 待复测（本次亦未能触发观察项 a 副屏 175% 方钮）。 |
| 7 | 坐窗口后退出进程 | **通过** | 主人反馈：「7 退出正常」。`real-user-Player-prev.log` 末尾正常销毁 Avatar 并 `Shutdown`，**0** 条 `SetOtherQuadsActive` 空指针与业务 `Exception`。 |
| 8 | 主题换色（`blue` 等） | **部分通过（设置页未统一）** | 主人反馈：「8 除了设置页颜色都没问题都是统一的，设置页总感觉很独立出来的感觉，完全不是一个画风而且出的问题最多」。日志 `real-user-Player.log` 实证：`-xq-theme blue` 下虽然 `XQ_UI2 theme=blue class=xq-theme-blue`，但点击设置页仍命中 `VisualElement.xq-st-panel.xq-theme-pink`（设置页未随主题切换且白底大窗与玻璃风割裂，见 `real-shot3`）。 |

### 新增/更新问题清单（同步记入 `ISSUES.md` `#13`~`#17`）

1. **点击菜单/设置页透传到底层本体与 HUD**：`XQ_HUD down` + `XQ_BRAIN src=local evt=drag_end` 与 `XQ_TODAY open` 仍被触发，致本体抖动/位置变化进而引起 UI 重排（`#13`）。
2. **UI 缩放与尺寸未锁舒适区间 + 设置页过大且未跟随主题色**：`PanelSettings.scale` 随人物缩放跑（`0.81~2.00`）致近景竖栏（`real-shot2`）、设置页（`real-shot1`/`real-shot3`）与一二级卡过大、裁半或遮人；`-xq-theme blue` 下设置页仍带 `xq-theme-pink` 且点击菜单未先关掉设置页导致穿透命中 `xq-st-panel`（`#14`）。
3. **桌宠人在左侧时「今天」卡片默认偏左、设置页定位错乱**：需固定「今天卡/设置页/一二级菜单」独立舒适尺寸与安全边距（`#15`）。
4. **近景交互体验：做动作偶发抽搐 + 近景右键仍呼出桌宠菜单违和**：主人建议近景功能整合进左侧或底部 Dock 栏，取消近景右键桌宠菜单弹窗（`#16`）。
5. **坐窗口状态下点「睡一会」会掉下窗口**：主人要求坐窗口上直接切换睡姿，保持坐窗前置状态不先下窗（`#17`）。
6. **配置提醒**：验收启动命令需带 `-xq-bench` 才会开启工作台入口（已更新 `.cmd`）。

---

## 第九次（自动化）（`int-all @ 1454ba8f`，WB-G73 B1 #13~#17 第二轮修复编译与回归）

- **被验提交**：`origin/xiaoqi/int-all` @ `1454ba8f`（`[未编]`，含 `XqMonitor.cs` + `XqMonitor.cs.meta` 与 #13~#17 修复）；`xiaoqi/p2-agy4` @ `1454ba8`（无新增 `.meta` 需补，未改动任何源码）。
- **`.meta` 与 GUID 核对**：`Assets/Xiaoqi/UI2/XqMonitor.cs.meta` 的 `guid: 7c1e5b2a94d84f3a8b6e0d2f1a3c5e79` 全仓唯一，无冲突。
- **编译**：**过**（0 compile errors，76 warnings，耗时 71s，`XiaoqiBuild.Win64`，见 `evidence/2026-11-17-B1-r9/build-summary-1454ba8.md`）。
- **证据文件**：`evidence/2026-11-17-B1-r9/`（`build-summary-1454ba8.md`、`close-test-1454ba8-Player.log`、`close-menu-test-1454ba8-Player.log`、`smoke30s-1454ba8-Player.log`、`theme-blue-1454ba8-Player.log`、`ACCEPT-TONIGHT.md`）。

### 1. `-xq-close-test`、`-xq-menu-test` 回归与 30s 冒烟：**均通过**

- **`-xq-close-test`**（`close-test-1454ba8-Player.log`）：
  - 3 轮切入耗时 `1.02s`、`1.01s`、`1.01s`；对眼稳定 `view=(0.50,0.55)/(0.50,0.55)/(0.50,0.56)`，因 `1454ba8f` 在 1.6s 后冻结 `TickEyeAlign`，4s 后退出前 `view=(0.53,0.57)`（目标 `(0.50,0.57)`）。
  - 最终漂移：`XQ_TEST close drift win=0,0 size=0,0 bodyCx=0 bodyBottom=0 bodyH=0`（全 0）。
  - 整段日志 **0** 条 `body outside` / `fix window` / `center`。
- **`-xq-close-test -xq-menu-test -xq-menu-debug`**（`close-menu-test-1454ba8-Player.log`）：
  - `XQ_MENU2 test-open mode=Close`
  - `XQ_MENU2 place close-face side=R l1=1201,194 limit=1917x1080`
  - `XQ_TEST close drift win=0,0 size=0,0 bodyCx=0 bodyBottom=0 bodyH=0`
  - 注意：`Close` 态下 `scale` 保持 `1.00`（不再被外框撑到 `2.00`），`limit=1917x1080`。
- **30s 冒烟 + `blue` 主题启动**：
  - `smoke30s-1454ba8-Player.log`（198 行）：仅 Localization 预加载常规日志，0 业务异常。
  - `theme-blue-1454ba8-Player.log`：`XQ_UI2 theme=blue class=xq-theme-blue`、`XQ_UI2 on theme=blue class=xq-theme-blue scale=1.00 bodyH=363 fontPx=16.0`（0 业务异常）。

### 2. 新增日志核对（G73 §4-3）

| 项 | 结果 | 实测日志或说明 |
|---|---|---|
| a) `XQ_UI2 on … scale=`（主屏 1.00 / 副屏 1.75） | 主屏已取到 `1.00`；副屏 1.75 **待主人** | 主屏桌宠态与近景态均稳定：`XQ_UI2 on theme=pink class=xq-theme-pink scale=1.00 bodyH=363 fontPx=16.0`、`XQ_MENU2 place ... scale=1.00`（不再随人物跳到 `2.00`）。 |
| b) 设置页 `XQ_SET2 place side=… wa=… size=… dpi=…` | **待晚上主人验收** | 隔离桌面无自动点击开设置页入口。 |
| c) 坐窗睡 `XQ_SLEEP sit-keep` / `sit-check sleeping=…` / `sit-restore` | **待晚上主人验收** | 需物理桌面坐窗后菜单点「睡一会」触发。 |

### 3. 晚上验收包更新（`E:\dsh-pet-projects\Xiaoqi-Tonight\`）

- **固定路径**：`E:\dsh-pet-projects\Xiaoqi-Tonight\Xiaoqi\Xiaoqi.exe`（已覆盖为 `1454ba8f` 最新构建）。
- **启动 `.cmd` 核对**：`1-启动验收(pink).cmd`、`2-主题blue.cmd`、`3-主题violet.cmd` **三个全部已确认带上 `-xq-bench`**，且 `2-主题blue.cmd` 带 `-xq-theme blue`。
- `ACCEPT-TONIGHT.md` 已按 `WB-G73` §3 更新为 10 项复测清单，物理桌面项全部标为「**待晚上主人验收**」。
- **ISSUES**：无新增纯编译错误（0 error，未新增 Issue）。

---

## 第九次补充：主人实机复测反馈与日志定位（`int-all @ 1454ba8f`）

- **证据文件**：`evidence/2026-11-17-B1-r9-real/`（`real-g73-Player.log`、`real-g73-bench-127-refused.jpg`、`real-g73-close-bench-off.png`）。

### 1. 本次实机已验证生效的修复

- **#13 设置页内点击不透传**：打开设置页并连续点击「外观/行为/显示/声音/连接/关于」及切换 `blue/violet/pink` 主题期间，日志中 **0** 条 `XQ_HUD down` 与 `drag_end`（已生效）。
- **#14 设置页尺寸摆位与主题同源**：日志实证 `XQ_SET2 place side=R wa=205,0 1711x864 size=664x536 dpi=1.00`、`XQ_SET2 set theme=blue` / `violet` / `pink` 均即时生效；开菜单后设置页自动退让不叠层。
- **工作台窗口定位/吸附/外侧方钮（含随窗换边）**：工作台启动后成功坐上标题栏（`XQ_BENCH sit ok=True frac=0.49`），外侧双方钮正常显示且由 `side=R rect=1794,326 40x88` 随窗口切换成 `side=L rect=130,261 40x88`，关窗后恢复 `XQ_BENCH restore pet window`。

### 2. 新暴露问题与主人明确否定项（已记入 `ISSUES.md` `#18`~`#20`）

1. **右键菜单除「设置」外行点击错位 / 点「让她…」弹今日日程（`#18`）**：
   - 主人原话：「点击让他直接弹出今日日程，右边四个菜单除了设置全部都是对照不是上的，要么就是点了没反应」。
   - **日志实锤**：
```text
XQ_MENU2 place body(screen)=769..1168 y=-17..666 H=1080 tl(panel)=769,414 l1=1180,442 scale=1.00
XQ_MENU2 click l1/p0 row='让她…' picked=VisualElement.xq-mrow
XQ_MENU2 click l1/p0 row='今天' picked=VisualElement.xq-mrow.xq-mrow-open
XQ_MENU2 click l1/p0 row='安静一会 · 安静中' picked=VisualElement.xq-mrow
XQ_MENU2 click l1/p0 row='宿舍 · fake walk_in' picked=VisualElement.xq-mrow.xq-mrow-open
```
   - **源码根因（`XqGlassMenu.cs:247-275`）**：`MousePanelPoints` 把未翻 Y 的 `RuntimePanelUtils.ScreenToPanel(panel, m)`（本机 `flipsY=False`）放在 `pts[0]`（`p0`），把正确的翻 Y 坐标放在 `pts[1]`（`p1`）；`Locate()` 先遍历 `pts[0]`。当菜单竖直跨越屏幕中线（如 `l1=1180,442` 高 `291`）时，镜像后的 `p0` 同样落在菜单卡片内，直接导致整卡行号**上下颠倒错命中**（点「让她…」命中「今天」展开/打开「今日日程」，点「安静一会」命中「宿舍 · fake walk_in」），只有最底行「设置」的镜像 `p0` 跑出卡外才落回 `p1` 命中成功。
2. **工作台弹出白底 Edge 网页且 `127.0.0.1` 拒绝连接，样式不符合二次元动漫风格（`#19`）**：
   - 主人原话：「工作台啥也打不开，而且我不希望是这种网业的样式，这和之前我们看的预览以及参考图完全不一样，没有那种二次元动漫风格啊」。
   - **现场实证**：`real-g73-bench-127-refused.jpg` 为标准 Windows Edge 浏览器白框窗口且页面报 `127.0.0.1 refused to connect`（本机未同启 `dspet` 本地后端服务；且产品形态与外观本身被主人否决，需重设非浏览器外观方案）。
3. **近景左下/右下「工作台」按钮仍灰置不可点（`#20`）**：
   - 主人原话：「当切换到近景的时候，工作台显示是无法打开的」。
   - **源码确认（`XqClosePanel.Controls.cs:78-84`）**：`_btnBench` 仍写死 `.AddToClassList("off")` 与悬停「还没开放」，尚未按 `-xq-bench` 接入 `XqMode.Set(XqView.Bench)`。

