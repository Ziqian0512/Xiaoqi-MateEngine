# P3-2a 摸底：ME 坐窗口怎么回事

> WB 读 ME 源码写，2026-11-16。**只读源码，没有 Unity，没跑过**；标「待实机」的是读代码推不出来的。
> 源文件：`Assets/MATE ENGINE - Scripts/AvatarHandlers/AvatarWindowHandler.cs`（下称 AWH，行号为改动前）。

## 7 条答案

| # | 问题 | 答案 | 代码位置 |
|---|---|---|---|
| 1 | 有没有「坐到某个 hwnd」的入口？ | **没有。** 唯一公开方法是 `ForceExitWindowSitting()`。坐的触发只有一条路：用户拖她 ≥ `minDragHoldSecondsToSit`（1 秒）且拖过阈值 → `TrySnap()`，用探针点碰窗口上沿 ±`ScaledProbeRadiusI()` 才吸附。最小适配点 = 新增一个公开方法，照抄 `TrySnap` 吸附成功后的那段（跳过探针和拖拽条件）。**已做：`XqSitOn(hwnd, fraction)` / `XqSitOff()` / `XqSnappedHwnd`，约 30 行，全部 `// XQ:` 标注，不改 ME 原有逻辑**（仅两处各改 1 行：`_postSettleRecalib` 里回算 `snapFraction` 加 `!_xqFixedFraction`；`ClearSnapAndHide` 清标志）| AWH:412 `TrySnap`、:446 `snappedHWND = win.hwnd` |
| 2 | 窗口被拖动，她自动跟随？延迟？ | **自动跟随。** 不拖她时每帧走 `FollowSnapped(false)`：读 `GetWindowRect(snappedHWND)` → `PinToTarget` 用 `MoveWindow` 移她的窗口。另有 `SmoothDamp`（`snapSmoothingTime=0.12s`），但**目标窗口位置一变就取消平滑**（`CancelSnapSmoothingIfTargetMoved`），所以拖窗口时是同帧跟，不拖尾。延迟 ≈ 0–1 帧（待实机录屏确认）| AWH:242、:548、:559、:480 |
| 3 | 水平位置能不能指定？ | **能。** 水平位置 = `窗口左 + snapFraction × 窗口宽`，`snapFraction` 是私有字段。默认由「落点相对窗口宽度的比例」定。`XqSitOn(hwnd, fraction)` 直接给它赋值（49% 就传 0.49），并用 `_xqFixedFraction` 防止落座后 1 帧的回算把它改掉 | AWH:563 `desiredPX`、:459 |
| 4 | 最小化 / 最大化 / 关闭时 ME 的行为？ | **最大化 / 全屏**：每帧检查 `IsWindowMaximized` / `IsWindowFullscreen`，命中就 `ClearSnapAndHide()` 下窗；**最小化 / cloaked**：`IsIconic`/`IsCloaked` 同样下窗；**窗口关闭**：`GetWindowRect` 失败 → `ClearSnapAndHide()`。下窗后是站桌面（`isWindowSit=false`），**不会自己坐回去**。→ `XqBenchWindow` 每 0.25 秒检查，窗口还在、非最小化、非最大化、她没在被拖、距上次 1.5 秒以上，就再 `XqSitOn`——这就是「还原后坐回去」| AWH:225–235、:350–365 |
| 5 | ME 对窗口有没有过滤？Edge `--app` 能过吗？ | 有。`IsSitEligibleWindow` 排除：非顶层、最小化、**无标题**、cloaked、宽 < 200 或高 < 60、类名 Progman/WorkerW/DV2ControlHost/MsgrIMEWindowClass、标题以 `#` 开头或含 `Desktop`；另有同进程窗口、「实质透明」窗口、疑似 UniWindow 吉祥物窗口。Edge `--app` 窗口类名 `Chrome_WidgetWin_1`、有标题、不透明、非同进程 → **读代码应能通过，待实机**。**注意：`XqSitOn` 直接指定 hwnd，不走探针，但每帧的下窗检查仍然适用；它不依赖过滤规则，所以即使过滤把 Edge 排除了（比如标题为空的那一瞬间），我们也能坐**。但 ME 的遮挡四边形（让网页盖住她的腿）用的是 `cachedWindows`，Edge 不在里面时腿不会被正确遮挡 → 待实机看 | AWH:330–345 `IsSitEligibleWindow`、:367 `UpdateCachedWindows` |
| 6 | 175% 缩放下坐标是物理像素还是 DIP？ | ME 全程只用 Win32 同一套（`GetWindowRect` / `MoveWindow` / `GetCursorPos`），**在 Unity 进程 DPI 感知方式下自洽**，不做缩放换算（AWH 内无 DPI 相关代码；只有 `MonitorHelper.GetDpiForMonitor` 用于设置页）。因此 **我们也只用 Win32 同一套**：`XqBenchWindow` 不信 Edge 的 `--window-position/size`（在 175% 下是否 DIP 没法读代码确定），而是窗口出来后用 `SetWindowPos` 按「Unity 看到的像素」摆，日志 `XQ_BENCH placed before=… after=… want=…` 会记下 Edge 自己摆的位置和最终位置，**回家 100% 与 175% 各看一次这行**，就能得出换算。若 Unity 不是 per-monitor DPI 感知，则 Win32 坐标可能是虚拟化后的值——那样 ME 现有的所有坐窗口行为也会错，所以按现状信 ME | AWH:559–575；`MonitorHelper.cs:63–86` |
| 7 | 坐姿高度字段？ | `AvatarWindowHandler.seatOffsetPx`（`[-256,256]` 像素，臀线相对窗口上沿的偏移，AWH:36）+ `windowSitYOffset`（`[-0.05,0.05]`，AWH:37）。**P1 设置页「坐姿高度微调」应写这两个之一**，由 DeepSeek 的设置页实现确认（`XqSettingsApply`）。`totalWindowSitAnimations=4`（坐姿动画 4 种，随机）。工作台标准图要求臀线贴窗口上沿 0–8 px：`seatOffsetPx` 默认 0 即贴边，待实机叠图微调 | AWH:36–37、:564 |

## 方案（P3-2b 草案，已按它写好代码）

选「**用 ME 的坐窗口 + 最小适配**」，不自己重写跟随：

1. ME 加 3 个公开成员（见第 1 条）。优点：跟随、平滑、遮挡、最大化下窗全是 ME 现成的；缺点：改了 1 个 ME 文件（G59-P3 §4.3 允许，≤3 个）。
2. 备选「`XqBenchWindow` 自己每帧读 `GetWindowRect` 移她的窗口」：不动 ME，但要自己做遮挡和坐姿动画切换，没必要。
3. 她被拖离后坐回去：松手 1.5 秒后自动再坐。**这是主观取舍**（用户可能想把她拖走）——想改成「拖走就算了」只要删 `XqBenchWindow.Update` 里那段，待主人看实机后定。

## 待实机确认（回家看）

- Edge `--app` 窗口能不能被 ME 当作可坐窗口（`XqSitOn` 后她是否真的坐下、腿被网页盖住的样子）。
- 拖窗口时她的跟随延迟（录屏）。
- 100% / 175% 下 `XQ_BENCH placed` 日志里 before / after 与 want 的差。
- `bench_pet_size`（进入工作台时她的大小）要实机标定到「头顶到臀线 ≈ 屏高 29%」，现在默认 0 = 不动。
- 最小化 → 还原后是否 1.5 秒内坐回；最大化时她是否站任务栏且不抖。
