# P1 · 问题单

> 一步试两种做法都不行、或遇到方案外的事，就写在这里然后停。WB 看了再决定。
> 格式：编号 / 谁写的 / 哪一步 / 现象 / 试过什么 / 你的建议 / WB 回复（只有 WB 写）。

| # | 谁 | 步骤 | 现象 | 试过 | 建议 | WB 回复 |
|---|---|---|---|---|---|---|
| 1 | WB | B1 | `XqFullscreenHide.SelfWindow()` 先按标题「小祈」找，找不到退化成任意 `UnityWndClass` 窗口，可能隐藏错窗口 | 只读代码，没跑 | B1 验证：标题是否确为「小祈」；有别的 Unity 窗口时是否误隐藏 | 待验证 |
| 2 | WB | B4 | 「坐姿高度微调」对应的 ME 字段还没找到 | 读过 AvatarWindowHandler 一带，未找到 | B4 开始时先查；找不到这一行灰掉，不要自己造 | 待查 |
| 3 | Grok | R2-2 | 2026-11-16 13:04 无法送达：Inori02 离线，int-d1@bcbfa2da 的 bundle 传不过去；未编译 | 未编译 | Inori02 重新上线后再跑 R2-2/R2-3 | |
| 4 | AGY+主人 | B1·#2/#8·R-5 | `XqSettingsPanel.Place()` 打开后严重偏下、底部大多数内容被裁到屏幕外面（仅露出顶部「设置/外观/坐在窗口上」，见 `B1-shot2` 与 `R5-settings-open.png`） | 已在物理桌面与 -settings-v2 复现，按 G66 未改代码 | 核查 `XqSettingsPanel.Place()` 的 `ScreenToPanel` Y 轴方向与高缩放（`PanelSettings.scale`）换算 | |
| 5 | AGY+主人 | B1·#2/#4/#5 | 人物变小或坐在窗口上时，`XqGlassMenu` 飞到屏幕上方脱离人物且与二级菜单错位，一二级行点击大多无反应或错位（日志 `p0.y=222` 与 `p1.y=866` 反向、`click l2/p1 picked=null`），且菜单随待机小幅度动作不断抖动（见 `B1-shot1`/`B1-shot3`） | 按 G66 规矩未改 `XqGlassMenu.cs` | 统一 `ScreenToPanel` 与屏幕坐标系 Y 轴；按主人建议，仅在单位整框自主移动时更新菜单，待机小幅度动作期间锁定菜单位置不跟着晃 | |
| 6 | AGY+主人 | B1·#7 | 桌宠态右下角看不到入口方钮（`B1-shot3` 右下角无钮），日志 `XQ_HUD gate shown=True rect(panel)=1041,78 40x40 scale=1.31` 显示 `y=78` 画到了屏幕上方 | 按 G66 规矩未改 `XqPetHud.cs` | 修复 `XqPetHud.PlaceBox` 传入 `ScreenToPanel` 时的 Y 轴方向，确保位于脚边 82% | |
| 7 | AGY | P2·R-5 | 近景态下点顶部关闭设置页时同时误触发了 `XqPetHud` 的「今日日程」弹窗（`XQ_TODAY open` + 502），且 `R5-Player.log` 记录 `XQ_CLOSE settings open` 触发了两次 | 按 G66 未改 `XqPetHud.cs` | 近景态屏蔽 `XqPetHud` 单击开今天卡，并排查 `XqClosePanel` 竖栏 `设` 双重触发 | |
| 8 | AGY+主人 | B1·#3 | 跳舞仅「舞 1」有动作响应，其余舞项无反应；日志 `XQ_HOST dances=0` | 已核对构建与日志 | 若需验收完整跳舞，需补齐 `StreamingAssets/CustomDances` 舞蹈资源（G66 §5 暂不算阻塞） | |
| 9 | AGY+主人 | G67 B1·#8 / R-5 | 点击「设置」（或近景 ⚙）打开设置页后 `XQ_SET2 open` 紧接着就输出 `XQ_SET2 close`，设置页直接打不开/菜单全部隐藏 | 复测 `701e6174`，按 G67 未改 `XqSettingsPanel.cs` | `XqSettingsPanel.Tick()` 在打开当帧 `Input.GetMouseButtonDown(0)` 仍为真且鼠标在页外（点的是菜单或 ⚙），立即触发 `Close()`；应跳过打开当帧或等左键抬起后再判定点页外关闭 | |
| 10 | 主人+AGY | G67 B1·#2/#4/#5 | ①拖动超过屏幕中间以上时闪回下方中间两点间疯狂闪动、无法拖到边缘；②菜单不再飞天，但放大/近景下人物待机微抖连带 `PanelSettings.scale`（1.24~1.32）与菜单轻微抖动；③退出时报 `AvatarWindowHandler.SetOtherQuadsActive` 空指针 | 复测 `701e6174`，未改代码 | ①排查拖动过屏幕中线以上被拉回底部的窗口钳位逻辑；②菜单打开期间同步锁定 `PanelSettings.scale`/面板坐标，并对放大/近景待机抖动做平滑/降敏评估 | |
| 11 | AGY | G68 `-xq-close-test` | `MeHost.EnsureOnScreen()` 改按身体中心 `pt = (wr.left + bodyR.center.x, wr.top + Screen.height - bodyR.center.y)` 调 `MonitorFromPoint(pt)`：进入 `Close` 态及退出过渡帧时人物 `bodyR` 大幅超屏（如 `pt=-1325,5792` / `1055,1452` / `1082,12658`），被误判出屏并反复触发 `XQ_WATCH body outside` → `XQ_HOST fix window` → `XQ_HOST center -327,-447`，把副屏窗口（`-2567,-683`）强行拉回主屏（`-327,-447`），造成 `XQ_TEST close drift win=2240,236` | 复测 `a5fe7f0c`，按 G68 未改 `MeHost.cs` | `Close` 态及模式切换过渡帧禁止按超屏 `bodyR.center` 触发 `MoveToCenter()`，改为仅在 `Pet` 态或按窗口自身矩形与任意显示器无交集时再保底拉回；另桌面 6 项+R-5 因主人在外且隔离桌面禁截屏暂无法验证 | |
| 12 | AGY | `98d0bb3b` `-xq-close-test -xq-menu-debug` | 近景菜单自动化取证未触发：`XqTestHooks.cs` 在 `-xq-close-test` / `-xq-menu-debug` 下无自动打开右键菜单的钩子（`98d0bb3b` 仅修改了 `XqGlassMenu.cs`），且 AGY 隔离桌面无法用 `SendInput` / `PostMessage` 命中物理桌面 `Input.GetMouseButtonDown(1) && onHer` 并截屏，致 `XQ_MENU2 place close-face side=R\|L l1=…` 日志未产生、近景菜单图无法取得 | **已关闭**（`707e68e9` 已补 `-xq-menu-test` 钩子并复测取到 `XQ_MENU2 test-open mode=Close` 与 `XQ_MENU2 place close-face side=R l1=607,97 limit=960x540`，drift 全 0；实机视觉不压脸留作清单观察项 b 待晚上主人验收） | — | 已关闭 |
| 13 | 主人+AGY | `bdb993c1` 实机 #1 | 点击菜单/设置页仍透传到底层本体与 HUD：每次点菜单/面板会同时打印 `XQ_HUD down` + `XQ_BRAIN evt=drag_end`（偶发 `XQ_TODAY open`），导致小祈动作变化并反过来触发菜单重排 | 实机日志 `real-user-Player-prev.log` / `real-user-Player.log` 复现 | 玻璃菜单或设置页打开并命中 UI 区域期间，彻底拦截底层 `PetHost` 点击/拖拽响应与 `XqPetHud` 按下判定（仅靠 `RegisterMenuBlocker` 或时机晚于 `XQ_HUD down` 时仍漏过） | |
| 14 | 主人+AGY | `bdb993c1` 实机 #1/#3/#8 | UI 跟随人物缩放导致过大遮挡，且设置页未跟随主题与菜单互斥：①`PanelSettings.scale`（`0.81~2.00`）随人物缩放致设置页遮人裁半（`real-shot1`/`3`）、近景左竖栏巨型化（`real-shot2`）；②`-xq-theme blue` 下设置页仍带 `xq-theme-pink`（日志 `picked=VisualElement.xq-st-panel.xq-theme-pink`），且画风独立于玻璃小卡；③开右键菜单未互斥关掉设置页导致叠层误点 | 实机截图与日志确认 | 按主人建议：①一二级菜单/设置页/今天卡/近景栏改用固定舒适尺寸区间（仅随显示器 DPI，不随人物体形缩放倍增）；②设置页挂载并同步当前主题 class，缩小/改玻璃风格并禁止遮人；③打开菜单或设置页时互斥关闭另一方 | |
| 15 | 主人 | `bdb993c1` 实机 #3 | 桌宠态人在屏幕左侧时，「今天」与「设置」二级弹窗仍出现在错误位置（今天默认偏左、设置大而偏） | 主人实机反馈 | 固定「今天卡/设置页」按显示器可用区 + 人物/菜单安全区居中或贴空侧展开，不压人物 | |
| 16 | 主人 | `bdb993c1` 实机 #1 | 近景模式下做动作时人物本体仍偶发抽搐抖动；且近景下仍可右键呼出桌宠同款菜单很违和 | 主人实机反馈 | ①排查近景相机与动画切态下的身体微颤平滑；②按主人设计，近景模式下禁用桌宠右键弹窗，把近景可用功能整合进左侧栏或底部 Dock 栏 | |
| 17 | 主人 | `bdb993c1` 实机 #4 | 坐在窗口上点「睡一会」会先掉下窗口再睡 | 主人实机反馈 | 坐窗状态点「睡一会」保持坐窗前置状态不调 `ExitWindowSit()`，直接在当前所坐窗口顶边切睡姿（醒来后也保持坐窗/正常动作） | |
