# P1 · 问题单

> 一步试两种做法都不行、或遇到方案外的事，就写在这里然后停。WB 看了再决定。
> 格式：编号 / 谁写的 / 哪一步 / 现象 / 试过什么 / 你的建议 / WB 回复（只有 WB 写）。

| # | 谁 | 步骤 | 现象 | 试过 | 建议 | WB 回复 |
|---|---|---|---|---|---|---|
| 1 | WB | B1 | `XqFullscreenHide.SelfWindow()` 先按标题「小祈」找，找不到退化成任意 `UnityWndClass` 窗口，可能隐藏错窗口 | 只读代码，没跑 | B1 验证：标题是否确为「小祈」；有别的 Unity 窗口时是否误隐藏 | 待验证 |
| 2 | WB | B4 | 「坐姿高度微调」对应的 ME 字段还没找到 | 读过 AvatarWindowHandler 一带，未找到 | B4 开始时先查；找不到这一行灰掉，不要自己造 | 待查 |
| 3 | DeepSeek | B4·D1 | 复查确认 ME 里仍无「坐姿高度」字段：`AvatarBigScreenHandler.YOffset` 是近景镜头，`AvatarWindowHandler` 只有 `minDragHoldSecondsToSit` | 只读代码，没跑 | 已按第 2 条建议处理：「坐姿高度微调」整行 `SetEnabled(false)` + 右侧小字「暂未接入」，`cfg.sit_offset` 只存盘不连宿主。等 ME 侧给出真实字段再接 | 待 WB 确认 |
| 4 | DeepSeek | B4·D1 | 「放音乐时自己跳」「坐在窗口上」的初始显示读的是宿主现值，没有对应的 `cfg` 字段，重启后会被 `XiaoqiDefaults` 复位（前者强制关、后者强制开），用户的选择记不住 | 只读代码，没跑 | D4 加 `dance_on_music`（默认 false）/ `window_sit`（默认 true）两个 cfg 字段，由 `ApplyOnStart()` 在启动时回灌 | 按 §7.1 执行 |
| 5 | DeepSeek | B4·D2补 | `SaveLoadHandler.ApplyAllSettingsToAllAvatars()` 在把 data 写回组件时会把 `isDancing` 置 false，一次「放音乐时自己跳」的开关就会停掉正在跳的舞；「坐在窗口上」「始终置顶」「大小」「画质」的 setter 与 `SaveSettings()` 也都会触发它 | 只读代码，没跑 | 已在 `SetDancing` 加「值没变就直接返回」，避免重复点=重复 ApplyAll。但真正切换时仍会停舞一次，属 ME 自身行为，本版不改 ME 文件 | 待 WB 确认 |
| 6 | DeepSeek | B4·D2补 | ME 的 `graphicsQualityLevel` 允许 0–3，而 G59-P1 §6 的「画质」只列 低/中/高 三档 | 只读代码，没跑 | 面板只画三档：现值 ≥3 时显示为「高」，但不写回宿主（保持原值），等编译后验证显示是否可接受 | 待验证 |
| 7 | DeepSeek | B4·D3 | 用 `-fake-stage` 启动时 `XiaoqiLink` 只推一次 stage，之后不再有事件 | 只读代码，没跑 | 连接页判定是「3 × poll_sec + 5 秒内收到过」，所以 fake-stage 下会先显示「已连接」，过期后变「连不上」。真服务器每次轮询都会推，不会这样。B8 若要用 fake 做验收，看这一行别误判 | 待 WB 知悉 |
| 8 | DeepSeek | B4·D3 | 关于页「版本 / 提交号」目前只有 `Application.version`，没有独立提交号来源 | 只读代码，没跑 | 提交号 B8 由编译脚本写入（`XiaoqiBuild` 一带），本次 D3 不做。读不到版本时显示「开发版」 | 待 B8 补 |
