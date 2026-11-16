# P1 · FACTS（事实表）

> 凡标「待 B1 补」的，要运行才知道。其余是 B0 时 WB 读 `a73dd06e` 代码得出，**没运行验证**。

## 1. 起点
- fork：`Ziqian0512/Xiaoqi-MateEngine`；Unity `6000.2.6f2`（`4a4dcaec6541`）；起点提交 `a73dd06e`（11-16 10:07）。
- 旧分支 `cursor/p1-1-ui-clickthrough-bdd1`、`cursor/ui2-ingame-clickthrough-96f3`：只读。

## 2. `XiaoqiHide` 关了什么（读 `Runtime/XiaoqiHide.cs`）
- 关 GameObject（`SetActive(false)`）：`ChatBot AI`、`MateEngineAPI/ModLoader`、`MateEngineAPI/FoodMenu`、`= Steam Exklusives`、`= ME Modding`、`= Settings`、`= AI`、`= FOOD SYSTEM`、`Main Menu`、`Chat`、`AI INPUT`、`AI Speech`、`AI Alarm Speech`、`AiSystemPrompt`、`Food`、`Food System`、`FoodMenu`、`ModLoader`、`AlarmMenuCanvas`、`BlendshapeMenuCanvas`、`DancePlayerMenuCanvas`、`MainMenuAlarm`、`MenuPanel`、`Welcome Window`、`Welcome`、`Tutorial`、`Discord`、`Discord RPC`、`DiscordPresence`、Steam 一批、`SystemStart`。`MateEngineAPI/CircleMenu` 跳过不关。
- 关组件（`enabled=false`）：`DiscordPresence`、`AvatarRandomMessages`、`AvatarMinecraftMessages`、`MenuAudioHandler`、`TutorialMenu`、`SystemStartHandler`、`LaunchMateEngineInstances`、`SteamWorkshopAutoLoader`、`SteamWorkshopHandler`、**`PetVoiceReactionHandler`**、**`KeyBindHandler`**（后两个是「没有悬停和触摸反应」的原因）。
- 另有 `StreamingAssets/Xiaoqi/xiaoqi_hide.json` 可追加。
- 旧 F1 圆盘：把所有 `KeyCode` 字段设成 `None`。

## 3. `XiaoqiDefaults`（读 `Runtime/XiaoqiDefaults.cs`，启动时写入 ME 的存档）
`enableDancing=false`、`enableDanceSwitch=false`、`enableRandomMessages=false`、`enableFeedSystem=false`、`enableWindowSitting=true`、**`enableLocomotion=false`**、`selectedLocaleCode="zh"`，其余 ME 功能全关。

## 4. `IPetHost` 现有能力（读 `Host/IPetHost.cs`）
`Ready`、`Avatar`、`Animator`、`AvatarChanged`、`IsDragging`、`ScreenRect()`、`SetFace(FaceFrame)`、`SetMouth`、`LoadModel`、`SetSleeping`、`IsWindowSitting`、`IsChibi`、`AvatarVisible`、`ExitWindowSit`、`MoveToCenter`、`Dances`、`Actions`、`RunAction(id)`、`IsDancing`、`PlayDance(name)`、`StopDance`、`Size`、`Topmost`、`Quality`、`Fps`、`WindowSitEnabled`、`LocomotionEnabled`、`StartWithWindows`、`SaveSettings`、`SuppressHostMenu`、`RegisterMenuBlocker`、`SetUiBlocksMovement`。
**没有：** 悬停回调、看向鼠标的开关、待机轮换控制、坐姿高度。B3 / B4 要加就加在 `MeHost`，并同步 `IPetHost`。

## 5. 改过的 ME 原文件（以 `2c5ea6b8` 为 ME 上游基线）
| 文件 | 改动 | 提交 |
|---|---|---|
| `Assets/MATE ENGINE - System Tray/SystemTray.cs` | 托盘 tooltip 改「小祈」 | E0 |
| `…/AvatarHandlers/AvatarAnimatorController.cs` | G55 一处；P1-2a：`xqHoldDance`、`StartDancing` / `SetDancing` 改 public、`XqSetDanceIndex` | `85da635`、`f2ced130` |
| `…/AvatarHandlers/AvatarSleepController.cs` | P1-2b：`xqHold`、`XqSetSleeping`、`Update` 开头 | `42c71572` |
| `ProjectSettings/ProjectSettings.asset` | companyName / productName → Xiaoqi；清空 Standalone 图标 | G55 |

## 6. 待 B1 补
- 窗口实际尺寸、位置（100% 与 175%）。
- ME 判断「鼠标在不在可交互物上」的位置，UI Toolkit 元素现在接在哪。
- `/api/body/stage`、`/day`、`/feed` 各一份真实 JSON（令牌打码）。
- 旧 `Xiaoqi.zip`（11-16 03:55）是从哪个提交编的。
- `xiaoqi_config.json` 在主人本机已被运行改写；升级覆盖安装时不要覆盖它。

## 7. B1 运行得到的事实（Inori02，`a73dd06`，100%，1920×1080，RTX 5090 D，D3D11）

- **游戏窗口矩形** `XQ_WATCH rect=349,511,1233,1031` → **884 × 520**，位置固定在屏幕左中下。**设置页要 70% 屏高（756）放不下，必须临时撑大窗口**，G59 已写，B4/B5 留意。
- **UI 缩放**：`scale=0.91 bodyH=332 fontPx=14.6`（身高 332 物理像素 / 364 样式稿 px）。穿透钩子 `hook=GetOnOpaquePixel`（ME 的不透明像素判断）。
- **ME 待机动作**：`PET_IDLE`、`PET_IDLE_2…15`、`PET_IDLE_UPDATE2_01…04`、`HUS_IDLE01…09`；舞蹈 `HUS_DANCE_01…04`；另有 `CUSTOM_DANCE`、`FACE_IDLE_1`。B3 的待机池从这里选。
- **`XQ_HOST dances=0`**：正常。这是 ME 自定义舞蹈文件夹（`Xiaoqi/Dances`）的列表，空的；菜单「舞 1–5」走内置 `dance_0…4`（`BuildActions`）。
- `KeyBindHandler`、`LaunchMateEngineInstances` 在场景里找不到（`not found`）；`SteamAPI_Init failed` 是预期（不用 Steam）。
- 这台机器连不上服务器（Curl 28），所以冒烟只用假数据（`-fake-stage` / `-fake-day`）。
