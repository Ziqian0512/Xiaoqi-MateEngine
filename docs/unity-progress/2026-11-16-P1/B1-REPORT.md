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

