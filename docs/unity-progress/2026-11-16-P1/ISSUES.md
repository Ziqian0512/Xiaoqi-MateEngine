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
