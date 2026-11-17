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

---

## 第十次（自动化）（`int-all @ 468a93a2` + `XqBenchPanel` 编译修正，G74 W1 原生工作台与回归）

- **被验提交**：`origin/xiaoqi/int-all` @ `468a93a2`（含 `efe867fc` #18/#20 与 `468a93a2` 原生工作台 `XqBenchPanel`）；`xiaoqi/p2-agy4` @ `56fd7b5`（含 1 处纯编译修正，未改任何业务逻辑）。
- **`.meta` 核对**：`Assets/Xiaoqi/UI2/XqBenchPanel.cs.meta`（`guid: a3d96f1b52c84e07b1f84c2d6e905a31`）已带在 `468a93a2` 中，无 GUID 冲突、无遗漏 `.meta`。
- **最小编译修正（记入 `ISSUES.md` `#21`）**：
  - 原 `Assets/Xiaoqi/UI2/XqBenchPanel.cs:268` 编译报错 `error CS1729: 'Rotate' does not contain a constructor that takes 1 arguments`（因 `Assets/MATE ENGINE - Shaders/kage-master/kage-master/kage-BRP/Assets/Scripts/Rotate.cs` 在全局命名空间定义了 `class Rotate : MonoBehaviour` 遮蔽 `UnityEngine.UIElements.Rotate`）。
  - 修正：`Assets/Xiaoqi/UI2/XqBenchPanel.cs:268` 改为 `new UnityEngine.UIElements.Rotate(new Angle(18f, AngleUnit.Degree))`（`xiaoqi/p2-agy4 @ 56fd7b5`）。
- **编译**：**过**（0 compile errors，76 warnings，耗时 69s，`XiaoqiBuild.Win64`，见 `evidence/2026-11-17-B1-r10/build-summary-468a93a.md`）。
- **证据文件**：`evidence/2026-11-17-B1-r10/`（`build-summary-468a93a.md`、`bench-mode-468a93a-Player.log`、`close-test-468a93a-Player.log`、`close-menu-test-468a93a-Player.log`、`smoke30s-468a93a-Player.log`、`ACCEPT-TONIGHT.md`）。

### 1. 进工作台取证（`-xq-bench -xq-mode bench -settings-v2`）

- **关键日志（`bench-mode-468a93a-Player.log`，0 业务 `Exception`）**：
```text
XQ_UI2 on theme=pink class=xq-theme-pink scale=1.00 bodyH=363 fontPx=16.0 scaleMode=ConstantPixelSize clearColor=False clearDepth=False hook=GetOnOpaquePixel
XQ_BENCH built k=1.260 panelH=756
XQ_WATCH rect=-5,0,1927,1119
XQ_MODE Pet -> Bench
XQ_BENCH zoom cur=0.33 new=0.88 z=0.82 ortho
XQ_BENCH open k=1.260 glass=False
XQ_CLOSE eyes view=(0.11,0.82) target=(0.50,0.57)
XQ_CLOSE day fail: 502
XQ_MODE Bench -> Close
XQ_BENCH close
XQ_CLOSE eyes view=(0.50,0.57) target=(0.50,0.57)
XQ_CLOSE exit by=esc
XQ_MODE Close -> Pet
XQ_CLOSE anim exit
XQ_CLOSE close
XQ_CLOSE eyes view=(0.51,0.57) target=(0.50,0.57)
XQ_CLOSE anim done
```
- **日志已确认**：窗口铺满显示器（`XQ_WATCH rect=-5,0,1927,1119` 覆盖 1920×1080）、她站在屏幕左侧（`XQ_CLOSE eyes view=(0.11,0.82)`）、工作台构建 `k=1.260 panelH=756` 并顺利打开、按 `Esc` 两级回退 `Bench -> Close`（`XQ_BENCH close`）→ `Close -> Pet`（`XQ_CLOSE exit by=esc`）全程 **0** 条 `Exception`。
- **截图与视觉/物理项**：隔离桌面 `CopyFromScreen` 报 `The handle is invalid`，`PrintWindow` 仅得全白空白帧无 UI 渲染画面，故截图「**无法取得**」；**4:3 白板+左药丸栏是否完整、她是否没被面板盖住、各行是否溢出、镜头里大小位置对不对、动效是否顺、透明处穿透、拖动手感** 一律标为「**待晚上主人验收**」。

### 2. 回归与离线兜底

- `-xq-close-test`（`close-test-468a93a-Player.log`）：3 轮切入 `1.03s / 1.01s / 1.02s`，`XQ_TEST close drift win=0,0 size=0,0 bodyCx=0 bodyBottom=0 bodyH=0`，整段无 `body outside / fix window / center`，0 业务 `Exception`。
- `-xq-close-test -xq-menu-test -xq-menu-debug`（`close-menu-test-468a93a-Player.log`）：`XQ_MENU2 test-open mode=Close`、`XQ_MENU2 place close-face side=R l1=1202,194 limit=1916x1080`、`XQ_TEST close drift win=0,0 size=0,0 bodyCx=0 bodyBottom=0 bodyH=0`，无退化。
- `30s` 冒烟（`smoke30s-468a93a-Player.log`，199 行）：`XQ_BENCH built k=1.260 panelH=756`，0 业务 `Exception`。
- **离线兜底与钮点击**：无本地服务时命中 `XQ_CLOSE day fail: 502` 无崩溃/无异常；近景点右下「工作台」钮、工作台点 `×`、右下「本地服务没开」提示条可见性与点「重试」出 `XQ_BENCH retry` 因隔离桌面无法点击/截屏，均标「**待晚上主人验收**」。

### 3. 重打晚上验收包

- **路径**：`E:\dsh-pet-projects\Xiaoqi-Tonight\`（主程序 `Xiaoqi\Xiaoqi.exe` 已覆盖为 `56fd7b5` 编译产物；`1-启动验收(pink).cmd`、`2-主题blue.cmd`、`3-主题violet.cmd` **三个全部带 `-xq-bench`**；[ACCEPT-TONIGHT.md] 已更新为工作台原生版清单，物理项均标「**待晚上主人验收**」）。

---

## 第十一次（自动化）（`int-all @ bd024f74`，G74 W1b 工作台缩回桌宠大小+Poll 挡 HUD+禁右键）

- **被验提交**：`origin/xiaoqi/int-all` @ `bd024f74`（已合入 `56fd7b5`，含 `XqBenchPanel` 重写、`XqCloseCamera` `_benchFrac`、`XqModeConfig.bench_pet_frac=0.34`、`XqGlassMenu` 排除 `Bench`、`XqUi2Sample` `Poll` + 整体挡 HUD）；`xiaoqi/p2-agy4` @ `bd024f7`（0 修改，0 新增 `.meta`）。
- **编译**：**过**（0 compile errors，59 warnings，耗时 69s，`XiaoqiBuild.Win64`，见 `evidence/2026-11-17-B1-r11/build-summary-bd024f7.md`）。`Screen.mainWindowDisplayInfo` 在 Unity `6000.2.6f2` 下直接编译通过，无需改成 `Display.main` 或 `Screen.currentResolution`。
- **证据文件**：`evidence/2026-11-17-B1-r11/`（`build-summary-bd024f7.md`、`bench-mode-bd024f7-Player.log`、`close-test-bd024f7-Player.log`、`close-menu-test-bd024f7-Player.log`、`smoke30s-bd024f7-Player.log`、`ACCEPT-TONIGHT.md`）。

### 1. `-xq-bench -xq-mode bench` 1080p 取证与 `Esc` 切换

- **关键日志（`bench-mode-bd024f7-Player.log`，全程 0 条 `Exception`）**：
```text
XQ_UI2 on theme=pink class=xq-theme-pink scale=1.00 bodyH=363 fontPx=16.0 scaleMode=ConstantPixelSize clearColor=False clearDepth=False hook=GetOnOpaquePixel
XQ_BENCH built k=1.260 panelPx=756 monitor=1920x1080
XQ_WATCH rect=-9,-80,1927,1039
XQ_MODE Pet -> Bench
XQ_BENCH zoom cur=0.31 new=0.34 z=1.98 ortho
XQ_BENCH open k=1.260 glass=False edge=0 pose=1
XQ_CLOSE eyes view=(0.68,0.79) target=(0.68,0.95) bench frac=0.35/0.34
XQ_CLOSE day fail: 502
XQ_MODE Bench -> Close
XQ_BENCH close
XQ_CLOSE eyes view=(0.50,0.57) target=(0.50,0.57)
XQ_CLOSE exit by=esc
XQ_MODE Close -> Pet
XQ_CLOSE anim exit
XQ_CLOSE close
XQ_CLOSE eyes view=(0.51,0.57) target=(0.50,0.57)
XQ_CLOSE anim done
```
- **日志核对**：
  - 她身高比例已落到目标：`XQ_BENCH zoom cur=0.31 new=0.34 z=1.98 ortho`、`XQ_CLOSE eyes view=(0.68,0.79) target=(0.68,0.95) bench frac=0.35/0.34`（屏高 ~35%，目标 34% / 1080p 约 378px）；
  - 处于板上沿（`edge=0 pose=1`，未出现在板下方）；
  - 通过 `Esc` 完成 `Pet -> Bench -> Close -> Pet` 来回切换，0 `Exception`。
- **1080p 截图**：**无法取得**（隔离桌面 `CopyFromScreen` 报 `The handle is invalid`，`PrintWindow` 仅导出空白白板无 UI 渲染画面，不以空图冒充）。4:3 白板+左侧药丸栏视觉完整性、位置美观度、动效顺不顺、拖动手感一律标「**待晚上主人验收**」。

### 2. 自动化交互与回归

- **点击交互（`×` / 玻璃开关 / 侧栏 2 号 / 工作台右键她）**：**待晚上主人验收**（隔离桌面无法向物理桌面注入 `SendInput` 鼠标点击与命中 `worldBound` 坐标，且无对应点击测试钩子；源码核对 `XqGlassMenu.cs:322` 已改为仅 `XqMode.Current == XqView.Pet` 才开桌宠右键菜单、`XqUi2Sample.cs` 已在 `XqBenchPanel.IsShown` 时跳过 `_hud?.HandlePointer`）。
- **`-xq-close-test`**（`close-test-bd024f7-Player.log`）：3 轮切入 `1.03s / 1.01s / 1.02s`，`XQ_TEST close drift win=0,0 size=0,0 bodyCx=0 bodyBottom=0 bodyH=0`，无 `body outside / fix window / center`，0 业务 `Exception`。
- **`-xq-close-test -xq-menu-test -xq-menu-debug`**（`close-menu-test-bd024f7-Player.log`）：`XQ_MENU2 test-open mode=Close`、`XQ_MENU2 place close-face side=R l1=1202,194 limit=1920x1080`、drift 全 0，无退化。
- **`30s` 冒烟**（`smoke30s-bd024f7-Player.log`，199 行）：`XQ_BENCH built k=1.500 panelPx=900 monitor=2560x1440`，0 业务 `Exception`。

### 3. 晚上验收包更新

- **路径**：`E:\dsh-pet-projects\Xiaoqi-Tonight\`（`Xiaoqi\Xiaoqi.exe` 已覆盖为 `bd024f74` 构建，`1-启动验收(pink).cmd`、`2-主题blue.cmd`、`3-主题violet.cmd` **三个全部带 `-xq-bench`**，[ACCEPT-TONIGHT.md] 已更新为 `bd024f74` 清单，物理项均标「**待晚上主人验收**」）。


---

## 第十二次（自动化 + World Space 探针）（`int-all @ 82367953`，G74 W1c；另开探针分支 `xiaoqi/p3-ws-probe @ cd2bd26`）

- **被验提交**：`origin/xiaoqi/int-all` @ `82367953`（G74 W1c：`XqBenchPanel` 屏高 80% / 侧栏空白与顶部 98px 整板拖动、`XqCloseCamera.SetTilt` / `ResetTilt`、`XqMode` 接入 `ApplyTilt`）；`xiaoqi/p2-agy4` 同步停在 `8236795`（0 修改）。
- **编译**：**过**（0 compile errors，59 warnings，耗时 69s，`XiaoqiBuild.Win64`，见 `evidence/2026-11-17-B1-r12/build-summary-8236795.md`）。`Screen.mainWindowDisplayInfo` 在 Unity `6000.2.6f2` 下直接编译通过；`XqCloseCamera.TickTilt` 在 `LateUpdate` `[DefaultExecutionOrder(200)]` 写 `a.transform.rotation` 无编译冲突（实机动作下是否与动画机打架留待物理桌面验收）。
- **证据目录**：`evidence/2026-11-17-B1-r12/`（`build-summary-8236795.md`、`bench-mode-8236795-Player.log`、`bench-r2-8236795-Player.log`、`close-test-8236795-Player.log`、`close-menu-test-8236795-Player.log`、`smoke30s-8236795-Player.log`、`ws-probe-Player.log`、`ACCEPT-TONIGHT.md`）。

### 1. `-xq-bench -xq-mode bench` 1080p 与 `Esc` 切换

- **关键日志（`bench-mode-8236795-Player.log`，0 条业务 `Exception`）**：
```text
XQ_UI2 on theme=pink class=xq-theme-pink scale=1.00 bodyH=363 fontPx=16.0 scaleMode=ConstantPixelSize clearColor=False clearDepth=False hook=GetOnOpaquePixel
XQ_BENCH built k=1.440 panelPx=864 monitor=1920x1080
XQ_MODE Pet -> Bench
XQ_BENCH zoom cur=0.32 new=0.34 z=2.06 ortho
XQ_BENCH open k=1.440 glass=False edge=0 pose=1
XQ_CLOSE eyes view=(0.74,0.79) target=(0.74,0.96) bench frac=0.34/0.34
XQ_CLOSE day fail: 502
XQ_MODE Bench -> Close
XQ_BENCH close
XQ_CLOSE eyes view=(0.50,0.57) target=(0.50,0.57)
XQ_CLOSE exit by=esc
XQ_MODE Close -> Pet
XQ_CLOSE anim exit
XQ_CLOSE close
XQ_CLOSE eyes view=(0.51,0.57) target=(0.50,0.57)
XQ_CLOSE anim done
```
- **核对结论**：
  - `panelPx=864 monitor=1920x1080`（精确等于屏高 `80%`）；
  - `XQ_BENCH zoom cur=0.32 new=0.34 z=2.06 ortho` 与 `XQ_CLOSE eyes ... bench frac=0.34/0.34`（身高比例收敛到目标 `0.34`）；
  - `Esc` 驱动 `Bench -> Close -> Pet` 正常退出、0 业务 `Exception`。
- **1080p 截图**：**无法取得**（隔离桌面 `CopyFromScreen` 报 `The handle is invalid`，`PrintWindow(..., 2)` 仅输出空白白板帧，已读图确认；板高 80%、贴左/右沿歪头转身、贴上沿只露头视觉观感均标「**待晚上主人验收**」）。

### 2. 自动化拖动 / 控件点击 / 5 轮切换 / 回归

- **`SendInput` 拖动与 5 轮热键切换**：隔离桌面 `SendInput` 返回 `0`（`bench-r2-8236795` 实测鼠拖与 `Ctrl+Alt+1/2/3` 均被桌面隔离拦截），故整板拖动（侧栏空白 / 顶部 98px）、`XQ_BENCH board drag start/end`、边缘不出屏、点 `×`/玻璃开关/重试/侧栏 2 号、右键她无 `XQ_MENU2 open`、近景↔工作台↔桌宠 5 轮连切及退出工作台朝向/旋转还原，全部实机物理交互项一律标「**待晚上主人验收**」。
- **`-xq-close-test`**（`close-test-8236795-Player.log`）：3 轮 `1.04s` 级切入，`XQ_TEST close drift win=0,0 size=0,0 bodyCx=0 bodyBottom=0 bodyH=0`，无 `body outside / fix window / center`，0 业务 `Exception`。
- **`-xq-close-test -xq-menu-test -xq-menu-debug`**（`close-menu-test-8236795-Player.log`）：`XQ_MENU2 test-open mode=Close`、`XQ_MENU2 place close-face side=R l1=1202,194 limit=1920x1080`、drift 全 0，无退化。
- **`30s` 冒烟**（`smoke30s-8236795-Player.log`）：0 业务 `Exception`。

### 3. 晚上验收包更新

- **路径**：`E:\dsh-pet-projects\Xiaoqi-Tonight\`（已由 `8236795` 构建覆盖 `Xiaoqi\Xiaoqi.exe`，`1-启动验收(pink).cmd`、`2-主题blue.cmd`、`3-主题violet.cmd` **三个均带 `-xq-bench`**，[ACCEPT-TONIGHT.md] 已更新为 `82367953` 清单，物理项均标「**待晚上主人验收**」；经校验 `Xiaoqi-Tonight` 不含任何 `p3-ws-probe` 代码）。

### 4. World Space `PanelSettings` 探针（独立分支 `xiaoqi/p3-ws-probe @ cd2bd26`，未合入 `int-all`）

- **改动范围**：仅新增 `Assets/Xiaoqi/WsProbe/XqWsProbeRunner.cs` 与 `Assets/Xiaoqi/WsProbe/Resources/XqWsPanelSettings.asset`（`-xq-ws-probe` 启动参数触发），未改正式逻辑，编译 0 error（58s）。
- **(a) ME 透明窗口下显示**：**可行（PASS）**。`UIDocument` + `UIRenderer`（`m_RenderMode: 1`，`m_RuntimeWorldShader: 9102`）在 ME 的 `Alpha/Opacity` + `SolidColor(0,0,0,0)` 透明窗口下正常绘制实色像素（`btnA=(754,326) rgba=(1.00,0.20,0.55,a=1.00)`），面板外背景保持完全透明（`outside=(40,540) rgba=(0.00,0.00,0.00,a=0.00)`）。（注：Unity `6000.2.6f2` 中 `PanelSettings.renderMode`、`colliderUpdateMode`、`pixelsPerUnit` 与 `UIDocument.isWorldSpace` 为 `internal` 属性，需在 `.asset` 中序列化配置；`UIDocument.worldSpaceSizeMode / worldSpaceSize` 为公开属性。）
- **(b) 3D 人物在板前/板后深度遮挡**：**可行（PASS）**。先采样基准人物像素 `(970,173)` 为 `rgba=(0.86,0.89,0.97,a=1.00)`；板摆在人物前（`z=2.75 < depth=3.146`）同像素变成亮橙板色 `rgba=(0.96,0.42,0.08,a=1.00)`（板遮挡人物）；板摆在人物后（`z=3.55 > depth=3.146`）同像素恢复人物色 `rgba=(0.87,0.89,0.98,a=1.00)` 且身旁粉钮 `btnA=(754,326)` 仍正常显示（`a=1.00`）。Z-buffer 原生前后遮挡成立。
- **(c) 鼠标命中（3 个点击点前后日志）**：**可行（PASS，须走 `EventSystem` + `PanelInputConfiguration` + `WorldDocumentRaycaster` 管线）**；`BaseRuntimePanel.Pick()` 在 World Space 下返回 `null`、`visualTree.worldBound` 为 `(0,0,0,0)`，旧版 `worldBound + Pick` 手动命中路线不适用：
```text
XQ_WSPROBE click#1 BEFORE tag=BtnA screenPt=(754,693) cntA=0 cntB=0 cntBoard=0
XQ_WSPROBE cb PointerDown target=ws-btnA local=(99.76, 70.29, 0.00)
XQ_WSPROBE cb Click target=ws-btnA cntA=1
XQ_WSPROBE click#1 AFTER tag=BtnA phys=[XqWsBoard dist=2.736] es=[XqWsPanelSettings@WorldDocumentRaycaster:d=2.74:wp=(-0.42, -0.17, -0.52); ...] picked=null cntA=1 cntB=0 cntBoard=1
XQ_WSPROBE click#2 BEFORE tag=BtnB screenPt=(1132,83) cntA=1 cntB=0 cntBoard=1
XQ_WSPROBE cb PointerDown target=ws-btnB local=(119.76, 80.29, 0.00)
XQ_WSPROBE cb Click target=ws-btnB cntB=1
XQ_WSPROBE click#2 AFTER tag=BtnB phys=[XqWsBoard dist=2.736] es=[XqWsPanelSettings@WorldDocumentRaycaster:d=2.74:wp=(0.35, -0.66, -0.52); ...] picked=null cntA=1 cntB=1 cntBoard=2
XQ_WSPROBE click#3 BEFORE tag=Outside screenPt=(40,540) cntA=1 cntB=1 cntBoard=2
XQ_WSPROBE click#3 AFTER tag=Outside phys=[Shadow dist=5.280] es=[EventSystem@WorldDocumentRaycaster:d=5.28:wp=(-1.87, 0.27, 2.02); ...] picked=null cntA=1 cntB=1 cntBoard=2
```
- **ISSUES**：本轮 `82367953` 0 编译错误，未改代码，无新增编译 ISSUES（前序仅 `#21`）。

---

## 第十三次（自动化）（`int-all @ 8dbbd5e2`，G74 W2 右侧置底立绘 + 叠卡 + 视差 + 直切工作台路径与 `diag`）

- **被验提交**：`origin/xiaoqi/int-all` @ `8dbbd5e2`（G74 W2：`UI2/XqBenchPanel.cs`、`Mode/XqMode.cs`、`Mode/XqModeConfig.cs`）；`xiaoqi/p2-agy4` 同步至 `8dbbd5e`（0 修改；方案 B 作废，`p3-ws-probe` 未合入）。
- **编译**：**过**（0 compile errors，76 warnings，耗时 69s，`XiaoqiBuild.Win64`，见 `evidence/2026-11-17-B1-r13/build-summary-8dbbd5e.md`）。`TickVisuals` 与 `XqMode.Transition` `if/else` 中新增 `yield` 均直接编译通过。
- **证据目录**：`evidence/2026-11-17-B1-r13/`（`build-summary-8dbbd5e.md`、`w2-direct-bench-8dbbd5e-Player.log`、`w2-close-to-bench-8dbbd5e-Player.log`、`w2-bench-esc-8dbbd5e-Player.log`、`w2-close-test-8dbbd5e-Player.log`、`w2-close-menu-test-8dbbd5e-Player.log`、`w2-smoke30s-8dbbd5e-Player.log`、`ACCEPT-TONIGHT.md`）。

### 1. 直切 / 近景切工作台复现与 `XQ_BENCH diag`

- **(a) 桌宠直接 `Set(Bench)`（`-xq-bench -xq-mode bench`）**（`w2-direct-bench-8dbbd5e-Player.log`，0 业务 `Exception`）：
```text
XQ_BENCH built k=1.332 panelPx=799 monitor=1920x1080
XQ_MODE Pet -> Bench
XQ_BENCH open k=1.332 glass=False edge=0 pose=1
XQ_BENCH parallax src=mouse
XQ_CLOSE eyes view=(0.76,0.56) target=(0.76,0.56) bench frac=0.79/0.78
XQ_BENCH diag host.disp=Flex vis=True parent=XqUi2Document-container stage=76,139,1176x801 op=1.00 panelSize=1920x1080 screen=1920x1080 pxS=-1.00
```
  - 另在多屏切换跑（`w2-bench-esc-8dbbd5e-Player.log`）中还观测到一段值得记录：`Warm()` 阶段窗口在 `2560x1440` 屏先构建 `XQ_BENCH built k=1.776 panelPx=1066 monitor=2560x1440`，进工作台铺满到 `1920x1080` 屏时 `diag` 打印：`XQ_BENCH diag host.disp=Flex vis=True parent=XqUi2Document-container stage=101,185,1569x1067 op=1.00 panelSize=1920x1080 screen=1920x1080 pxS=-1.00`（`185+1067=1252 > 1080`，因 `EnsureBuilt()` 重建阈值看的是 `MonitorDims()` 的 `px` 而未重核屏尺寸改变；未按单屏复现，已如实贴出两端 `diag` 备查，未改动任何逻辑）。
  - 接 `Esc` 验证（`w2-bench-esc-8dbbd5e-Player.log`）：`XQ_MODE Bench -> Close` → `XQ_BENCH close` → `XQ_CLOSE exit by=esc` → `XQ_MODE Close -> Pet`，均 0 `Exception`。
- **(b) 近景 → 点工作台**（`w2-close-to-bench-8dbbd5e-Player.log`）：近景打开正常（`XQ_CLOSE open`、`XQ_CLOSE ok btn-bench rect=0.919,0.896 0.042x0.074`），但隔离桌面无法通过 `PostMessage(WM_LBUTTONDOWN)` / `SendInput` 命中 `btn-bench`（未触发 `XQ_CLOSE btn bench`），此路是否出板标「**待晚上主人验收**」。

### 2. 1080p 截图、视差、拖板与回归

- **1080p 截图（含右侧 78% / 置底不歪 / 左板 3 层叠卡 / 光标最左最右视差对比）**：**无法取得**（隔离桌面 `CopyFromScreen` 报 `The handle is invalid`，`PrintWindow(..., 2)` 仅输出空白白板帧；`SendInput mouse left=0 right=0` 无法移动真实光标或拖动）。
  - 实测已有 `XQ_BENCH parallax src=mouse` 日志出现，`XQ_CLOSE eyes view=(0.76,0.56) target=(0.76,0.56) bench frac=0.79/0.78` 对位落点命中目标。
  - **全部物理视觉与鼠标交互项**（两条入口出板视觉、右侧 78% 脚切屏外不歪、左板下 3 层叠卡、最左/最右光标视差画面差、拖药丸栏空白/顶部 98px 整板动且她不动、点 `×`/玻璃开关/重试/侧栏 2 号、右键禁菜单、桌宠↔近景↔工作台 5 轮连切与退出站姿还原、手感质感）均标「**待晚上主人验收**」。
- **自动化回归（均 0 业务 `Exception`）**：
  - `-xq-close-test`（`w2-close-test-8dbbd5e-Player.log`）：3 轮 `1.01s~1.03s` 切入，`XQ_TEST close drift win=0,0 size=0,0 bodyCx=0 bodyBottom=0 bodyH=0`。
  - `-xq-close-test -xq-menu-test -xq-menu-debug`（`w2-close-menu-test-8dbbd5e-Player.log`）：`XQ_MENU2 test-open mode=Close`、`XQ_MENU2 place close-face side=R l1=1202,194 limit=1920x1080`、drift 全 0，无退化。
  - `30s` 冒烟（`w2-smoke30s-8dbbd5e-Player.log`）：0 业务 `Exception`。

### 3. 晚上验收包更新 & ISSUES

- **晚上包**：`E:\dsh-pet-projects\Xiaoqi-Tonight\`（已覆写为 `8dbbd5e` 纯净构建，三个 `.cmd` 均带 `-xq-bench`，[ACCEPT-TONIGHT.md] 已更新为 W2 验收清单）。
- **ISSUES**：本轮 `8dbbd5e2` 0 编译 error，未修改正式代码，**无新增 ISSUES**（前序仅 `#21`）。

---

## 第十四次（自动化 + 内嵌截帧 + AGY 改良对比）（`int-all @ 10530de5` W3 vs `xiaoqi/agy-w3-alt @ 590e5e4`）

- **被验提交**：`origin/xiaoqi/int-all` @ `10530de5`（G74 W3；禁止 World Space，`p3-ws-probe` 未合入）；`xiaoqi/p2-agy4` 同步停在 `10530de`（0 修改）。
- **编译**：**过**（0 compile errors，59 warnings，耗时 67s，`XiaoqiBuild.Win64`，见 `evidence/2026-11-17-B1-r14/build-summary-10530de.md`）。`XqBenchPanel.Poll`、`XqMode.Transition` `path`、`XqGlassMenu` 均直接编译通过。
- **证据目录**：`evidence/2026-11-17-B1-r14/`（全套 `.log`、`build-summary-10530de.md`、`ACCEPT-TONIGHT.md`、`w3-02/06/07/08*.png`、`alt-02/06/07/08*.png`）。

### 1. 多屏复现 & 截帧致命发现（**ISSUES #22**）

- **多屏路径（`w3-shot-pet-Player.log`）**：启动先在 `2560x1440` 屏 `Warm()` 构建出 `panelPx=1066`，`Pet -> Bench`（`path=pet-direct ok=True screen=1920x1080`）后立刻触发重建并按当前 `1920x1080` 算准尺寸，两条入口（`pet-direct` 与 `from-close`）均 `ok=True shown=True`，0 业务 `Exception`：
```text
XQ_BENCH built(rebuild=n) k=1.776 panelPx=1066 monitor=2560x1440
XQ_MODE Pet -> Bench
XQ_MODE bench path=pet-direct ok=True screen=1920x1080 target frac=0.78 eye=(0.37,0.87)
XQ_BENCH built(rebuild=y) k=1.332 panelPx=799 monitor=1920x1080
XQ_BENCH open k=1.332 glass=True edge=0 pose=1
XQ_MODE bench shown=True path=pet-direct
XQ_BENCH parallax src=mouse
XQ_CLOSE eyes view=(0.37,0.87) target=(0.37,0.87) bench frac=0.78/0.78
XQ_BENCH diag host.disp=Flex vis=True parent=XqUi2Document-container stage=29,122,1225x862 op=1.00 panelSize=1920x1080 screen=1920x1080 pxS=-0.97 pet=(x:461.49, y:270.98, width:534.43, height:836.95) builtPx=799 k=1.332
XQ_MODE Close -> Bench
XQ_MODE bench path=from-close ok=True screen=1920x1080 target frac=0.78 eye=(0.37,0.87)
XQ_BENCH diag host.disp=Flex vis=True parent=XqUi2Document-container stage=76,139,1177x801 op=1.00 panelSize=1920x1080 screen=1920x1080 pxS=0.00 pet=(x:503.57, y:249.86, width:497.99, height:853.98) builtPx=799 k=1.332
```
- **实机截帧与 `#22` 根因**：通过 `XqShotProbe` 进程内 `ScreenCapture` 抓到 `10530de` 的 1080p 画面（`w3-02_bench_center.png`）：
  - **左栏（`_rail`）** 正常半透（像素 `(120,250) A=107`）且 5 个图标清晰；
  - **右板（`_panel`）** 因 `XqBenchPanel.cs:344` 保留 `_panel.style.overflow = Overflow.Hidden;`，在 `clearDepthStencil=false` 下触发模板缓冲裁剪，导致整块右板变成 **纯白不透明（`(400,200)` / `(600,400)` 为 `R=255,G=255,B=255,A=255`）**，所有标题、6 行内容、滑条、开关与 `×` 钮全部被裁没且背后小祈完全透不出！
- **站姿 20s 固定 / 视差 / 拖板 / 多轮回归（探针日志实证）**：
  - `still0 -> still10 -> still20`：`idle=0.00 speed=0.05 state=2081823275`（20 秒姿势完全静止）；
  - 视差：`parL` `rot=(0.6, 192.0, 357.1) pxS=-1` vs `parR` `rot=(0.5, 169.3, 2.6) pxS=0.89`，左栏+右板刚体同向整体平移倾转，背后小祈真 3D 转身；
  - 拖板：`08_drag_mid`（`rect=1034,217`）、`09_drag_edge`（`rect=1163,207`）板与她同步移动且边缘不越屏；
  - 来回切换（4 轮完成至第 5 轮时 135s 超时收尾）：每轮退出回 `Pet` 均复位 `idle=0.00 speed=1.00 rot=(0.0, 180.0, 0.0)`。
- **纯净回归（`w3-close-test` / `w3-close-menu-test` / `w3-smoke30s`，均 0 业务 `Exception`）**：
  - `-xq-close-test`：`XQ_TEST close drift win=0,0 size=0,0 bodyCx=0 bodyBottom=0 bodyH=0`；
  - `-xq-close-test -xq-menu-test -xq-menu-debug`：`XQ_MENU2 test-open mode=Close`、`XQ_MENU2 place close-face side=R l1=1202,194 limit=1920x1080`，drift 全 0；
  - 物理桌面 `SendInput` 鼠点/拖动/右键与质感体感标「**待晚上主人验收**」。

### 2. AGY 改良对比版（分支 `xiaoqi/agy-w3-alt @ 590e5e4`，构建包 `E:\dsh-pet-projects\xq-builds\w3-agy-alt\Xiaoqi.exe`）

- **对比 `10530de`（`w3-02_bench_center.png`）与 AGY 改版（`alt-02_bench_center.png`，`blue` 主题）**：
  1. **修复 `#22`**：去掉 `_panel.style.overflow = Overflow.Hidden;`，右板立即从纯白死板恢复通透毛玻璃（`(400,200) A=185`、`(600,400) A=175`），`✦ XIAOQI WORKBENCH`、6 行、滑条、开关、`×` 与背后小祈全身清晰透出；
  2. **二次元主题与材质统一**：读取 `XqUi2Sample.CurrentTheme`（`pink/blue/violet`）统一配色，左栏从深灰紫改为同色系浅霜玻璃与高光白边，左栏+右板质地协调不再割裂；
  3. **控件与手感升级**：新增顶部居中拖手条与柔光带、行胶囊悬停高亮、6px 圆角主题色滑条与描边拇指滑块、拖板按速度动态微倾（`±3.5°`）与轻微抬起缩放（`1.015x`）松手回弹。（注：自查 `alt-02` 见 `topGlow` 直角上沿与斜向 `_sweep` 顶角微露，后续用圆角或安全内边距收掉即可。）

### 3. 晚上验收包更新

- `E:\dsh-pet-projects\Xiaoqi-Tonight\`（纯净 `10530de` 构建，三 `.cmd` 均带 `-xq-bench`，[ACCEPT-TONIGHT.md] 已更新为 W3 清单并附对比包路径 `xq-builds\w3-agy-alt\Xiaoqi.exe`）。

---

## 第十五次（自动化 + 内嵌截帧）（`int-all @ 2ac1238b`，G74 W4：#22 修复 + 胸像/全身构图 + 反向平移视差）

- **被验提交**：`origin/xiaoqi/int-all` @ `2ac1238b`（已 cherry-pick `149170a2` + `590e5e44`，`XqShotProbe` 仅在临时分支 `xiaoqi/agy-w4-probe`，未进 `int-all`；禁止 World Space，`p3-ws-probe` 未合入）。`xiaoqi/p2-agy4` 同步停在 `2ac1238`（0 修改）。
- **编译**：**过**（0 compile errors，76 warnings，耗时 68s，`XiaoqiBuild.Win64`，见 `evidence/2026-11-17-B1-r15/build-summary-2ac1238.md`）。
- **证据目录**：`evidence/2026-11-17-B1-r15/`（`w4-shot-pink/blue/violet/mode1-Player.log`、`w4-close-test/close-menu-test/smoke30s-2ac1238-Player.log`、`w4-pink-02/06_parL/06b_parC/07_parR/08_drag.png`、`w4-blue-02.png`、`w4-violet-02.png`、`w4-mode1-02.png`、`build-summary-2ac1238.md`、`ACCEPT-TONIGHT.md`）。

### 1. 渲染（`#22` 回归 + 三主题）与多屏尺寸

- **多屏复现（`w4-shot-pink-Player.log`，2560x1440 → 1919x1080）**：
```text
XQ_BENCH built(rebuild=n) k=1.776 panelPx=1066 monitor=2560x1440
XQ_MODE Pet -> Bench
XQ_MODE bench path=pet-direct ok=True screen=1919x1080 target frac=1.50 eye=(0.37,0.72)
XQ_BENCH built(rebuild=y) k=1.332 panelPx=799 monitor=1919x1080
XQ_BENCH open k=1.332 glass=True edge=0 pose=1
XQ_MODE bench shown=True path=pet-direct
XQ_BENCH parallax src=mouse
XQ_CLOSE eyes view=(0.37,0.72) target=(0.37,0.72) bench frac=1.53/1.50
XQ_BENCH diag host.disp=Flex vis=True parent=XqUi2Document-container stage=92,129,1177x801 op=1.00 panelSize=1919x1080 screen=1919x1080 pxS=-0.97 pet=(x:163.56, y:-547.94, width:1099.26, height:1648.05) builtPx=799 k=1.332
```
- **截帧核对**（`w4-pink-02.png`、`w4-blue-02.png`、`w4-violet-02.png`）：
  - 右板不再是纯白不透明（`w4-mode1-02.png` 非人物中心像素 `midRGBA=0.79,0.76,0.77,a=0.64`，`A < 255`），标题、6 行、主题滑条、开关与背后小祈均清晰透出；
  - `pink`/`blue`/`violet` 三主题左栏与右板同色系联动；`topGlow` 顶部圆角贴合面板、无直角或扫光溢出圆角外。

### 2. 构图（默认胸像 + `bench_stage_mode=1` 全身）与固定站姿

- **默认胸像（`bench_stage_mode=0`）**：`XQ_CLOSE eyes view=(0.37,0.72) target=(0.37,0.72) bench frac=1.53/1.50`（准确收敛到 `1.50` 目标），`w4-pink-02.png` 实见头+肩+胸铺在半透工作台后方、头顶略低于板上沿、身体下沿延伸出板框外。
- **全身缩进（`bench_stage_mode=1`，`w4-shot-mode1-Player.log` + `w4-mode1-02.png`）**：`XQ_CLOSE eyes view=(0.37,0.78) target=(0.37,0.78) bench frac=0.69/0.68`，完整全身缩进板高内（约板高 92%，不越板框）。
- **固定站姿**：间隔 5s（`still0` → `still5` → `still10`）日志恒定 `idle=0.00 speed=0.05 state=2081823275`。

### 3. 拖板与视差

- **拖板**：`w4-pink-08_drag.png` 实见板和她同步平移、`09_drag_edge` 边缘钳位 `rect=975,-569` 不出屏（物理 `SendInput` 拖左栏空白/顶部 98px 留待「**待晚上主人验收**」）。
- **视差（`w4-pink-06_parL.png` / `06b_parC.png` / `07_parR.png`）**：
  - `parL`：`stRot=0.00 stScale=1.000 stTx=16.0`，她 `rot=(0.0, 189.0, 0.0)`；
  - `parC`：`stRot=0.00 stScale=1.000 stTx=0.0`，她 `rot=(0.0, 180.5, 0.0)`；
  - `parR`：`stRot=0.00 stScale=1.000 stTx=-16.0`，她 `rot=(0.0, 172.0, 0.0)`；
  - 前景左栏+右板只同步反向平移（不旋转不缩放），她绕竖轴真 3D 转身约 `±9°`，且 `XQ_CLOSE eyes` 未在视差期间每帧刷屏。

### 4. 三路入口、回归与晚上包

- **三路入口**：状态机双路径 `pet-direct` 与 `from-close` 均实跑通过（`ok=True shown=True`）；隔离桌面无法用 `SendInput` 点击桌宠右键「工作台」/ 右下方钮 / 近景「工作台」按钮，三路物理按钮实操标「**待晚上主人验收**」。
- **5 轮切出恢复**（`w4-shot-pink-Player.log` `round1-pet` ~ `round5-pet` + `final`）：每轮退出回 `Pet` 均复位 `idle=0.00 speed=1.00 rot=(0.0, 180.0, 0.0)`，0 业务 `Exception`。
- **纯净回归（0 业务 `Exception`）**：`-xq-close-test`（`drift win=0,0 size=0,0 bodyCx=0 bodyBottom=0 bodyH=0`）、`-xq-close-test -xq-menu-test -xq-menu-debug`（`XQ_MENU2 place close-face side=R l1=1202,194 limit=1919x1080`、drift 全 0）、`30s` 冒烟全过。
- **晚上包**：`E:\dsh-pet-projects\Xiaoqi-Tonight\`（已覆写为纯净 `2ac1238` 构建，三个 `.cmd` 均带 `-xq-bench`，[ACCEPT-TONIGHT.md] 已更新；物理手感/质感/动效均标「**待晚上主人验收**」）。
- **ISSUES**：本轮 `2ac1238b` 0 编译 error，未修改正式代码，无新增 ISSUES（`#22` 已于 `2ac1238b` 回归通过，待 WB 正式闭单）。
