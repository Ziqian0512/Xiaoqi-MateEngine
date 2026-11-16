# G63 / G64 · P2 近景一次性验收与 D-1～D-4 修复报告

- **执行人**：AGY（新 Agent：编译员 + 验收员 + D-1～D-4 开发者）
- **日期**：2026-11-17
- **被验基线分支**：`origin/xiaoqi/p2` @ `ec4b43cd`（`ec4b43cd9402887b5ebff618455d8c74d1f419e4`）
- **修复代码分支**：`origin/xiaoqi/p2-agy` @ `607f84e`（从 `ec4b43cd` 切出，共 4 个原子提交，不直接推 `xiaoqi/p2`）
- **验收证据分支**：`origin/xiaoqi/p2-accept-agy`（证据目录：`docs/unity-progress/2026-11-17-P2-accept-agy/`）

---

## 1. 环境记录

| 项目 | 值 |
|---|---|
| 机器 | `INORI02`（Windows 11 Professional 10.0.26200 64-bit, RTX 5090 D v2, 32 GB RAM）|
| Unity 版本 | `6000.2.6f2 (4a4dcaec6541)`（命令行 `-batchmode -quit -buildTarget Win64 -executeMethod XiaoqiBuild.Win64`）|
| 显示器布局 | `DISPLAY1`（主屏 `0,0 1920×1080`）；`DISPLAY2`（副屏 `-2560,-683 2560×1440`）|
| 缩放测试 | `100%`（`2560×1440` 全屏）全套 A–H + `175%` 等效逻辑分辨率（`1463×823`）A、B 复测 |
| 启动参数钩子 | `-xq-close-test`、`-xq-mode close`、`-xq-gate-always`、`-xq-theme blue`、`-xq-hotkeys`、`-xq-nap-test`、`-fake-day` |

---

## 2. P2 十项验收表（提交 WB 复核）

> 表中同时列出 **`ec4b43cd` 初测结果** 与 **`xiaoqi/p2-agy`（`607f84e`）修复后重测结果**。按 G64 §0/§5.2，本表为「提交验收」，最终验收与合并归 WB。

| # | 项 | `ec4b43cd` 初测 | `xiaoqi/p2-agy` 重测 | 一行结论 | 证据文件（位于 `docs/unity-progress/2026-11-17-P2-accept-agy/`）|
|---|---|---|---|---|---|
| 1 | 编译 0 error | **过** | **过** | `XiaoqiBuild.Win64` 编译通过，0 error、59 warnings，耗时 60s | `logs/verify-ec4b43c.log` |
| 2 | 3 轮切换不崩、无漂移 | 未过（`bodyCx=-73, bodyBottom=-23, bodyH=31`）| **过（D-1 已修）** | 3 轮切换无崩溃；修复退出后动画相位与位置恢复后，`drift win=0,0 size=0,0 bodyCx=-1 bodyBottom=0 bodyH=-1`（≤ ±3px）| `logs/A-close-test-Player.log`、`logs/A-close-test-before-ec4b43cd.log` |
| 3 | 进入 ≤ 1.3 秒、退出约 0.35 秒 | **过** | **过** | 3 轮进入耗时分别为 `1.03s`、`1.01s`、`1.06s`（均 ≤ 1.3s），退出动画 `0.35s`（`XQ_CLOSE anim exit` → `anim done`）| `logs/A-close-test-Player.log` |
| 4 | 取景符合 §3.1（眼 43%、居中 ±5%）| 未过（眼睛约 57%、偏右约 68%）| **过（D-1 已修）** | 调 `close_yoffset=0.045` 并保持站姿平稳帧水平对齐镜头中线后，眼睛位于屏高约 `43%`、水平居中 `50%`（100% 与 175% 叠图均对齐）| `shots/P2-2-camera-100.png`、`shots/P2-2-camera-175.png`、`shots/B-close-100-grid.png` |
| 5 | 脸区无元素 | **过** | **过** | 全部 10 个槽位均打 `XQ_CLOSE ok`，无任何 `XQ_CLOSE violate`；屏宽 38–62% × 屏高 18–65% 禁区内无 UI 元素 | `shots/B-close-100-grid.png`、`logs/B-C-E-Player.log` |
| 6 | 占位卡标「占位」，灰钮灰 | 部分未过（四张卡因 `.xq-body overflow:hidden` 模板裁剪空白）| **过（D-2 已修）** | 去掉 `.xq-body overflow:hidden` 后四张卡正常显示；「今日小事」「一起专注（25:00）」均标「占位」，右下「工作台」灰且悬停显示「还没开放」| `shots/B-close-100.png`、`shots/B-close-tip-bench.png`、`shots/before-ec4b43cd-B-close-100.png` |
| 7 | 穿透成立（卡上能点、透明处穿透）| 部分未过（`XqClosePanel` 原用 `ClickEvent` 失灵）| **过（D-2/D-3 已修）** | 竖栏/方钮/圆环补齐几何命中：点「今」「话」触发 `XQ_CLOSE focus on/off`（1.04 倍放大与还原），点「待」「设」有日志响应；透明处 `pickingMode=Ignore` 不拦底层 | `shots/C-focus-next-on.png`、`shots/C-focus-say-on.png`、`logs/B-C-E-Player.log` |
| 8 | 4 种退出方式都回原位 | 部分未过（`Esc` 过；`♥`/`桌宠` 因 `ClickEvent` 未触发）| **过（D-2/D-3 已修）** | `Esc`、点竖栏 `♥`（`XQ_CLOSE rail pet`）、点右下 `桌宠`（`XQ_CLOSE btn pet`）及热键均触发 `XQ_MODE Close -> Pet` 并回到原位 | `shots/D-exit-esc.png`、`logs/B-D-Player.log`、`logs/G-blue-Player.log`、`logs/B-175-Player.log` |
| 9 | 数据变化 ≤ 5 秒；断线显示「还没有」| 部分未过（轮询未联动刷新精力卡，且卡面空白）| **过（D-2/D-4 已修）** | 改数据后 3 秒内「下一件事」「精力」「她说」同步更新；断线后 3 秒内三张卡均回退显示灰字「还没有」| `shots/P2-5-data-1.png`、`shots/P2-5-data-2.png`、`shots/P2-5-offline.png`、`logs/B-C-E-Player.log` |
| 10 | 近景里右键 / 拖 / 全屏不报错 | **过** | **过** | 全程无未捕获 `Exception` / `NullReferenceException`；回桌宠后 `-xq-nap-test`（睡一会/唤醒）与常显方钮均正常 | `shots/H-pet-regression.png`、`logs/H-nap-Player.log`、`logs/B-C-E-Player.log` |

---

## 3. 初测未过项根因与 `xiaoqi/p2-agy` 四个修复提交（§3.5 D-1～D-4）

### 3.1 D-1（对应十项 #2、#4）：`0ac2de1`
- **提交信息**：`[已编] D-1(十项#2/#4): 近景镜头调参(close_yoffset=0.045)与保持站姿平稳帧水平居中, 退出恢复位置与动画相位消除3轮漂移`
- **修改文件**：`Assets/Xiaoqi/Mode/XqCloseCamera.cs`、`Assets/Xiaoqi/Mode/XqModeConfig.cs`
- **根因**：
  1. ME 的 `AvatarBigScreenHandler.ActivateBigScreen()` 会置 `avatarAnimator.SetBool("isBigScreen", true)`，触发 ME 内置歪身举手动作，导致她在近景中头部偏右至屏宽约 68%、右手伸入脸区，且 `close_yoffset=0.08` 使眼睛偏低至屏高约 57%。
  2. 退出近景时，因 `Idle` 动画循环处于不同时间相位，蒙皮包围盒 `PetHost.I.ScreenRect()` 相对进入前产生 `bodyCx=-73, bodyBottom=-23, bodyH=31` 的姿态差。
- **修复**：
  - 不改 ME 的 `AvatarBigScreenHandler.cs`；在 `XqCloseCamera.BeginEnter()` 中调 `ToggleBigScreenFromUI()` 后置 `isBigScreen=false`，固定在 `Idle` 槽位 0 平稳站姿帧并将头部水平对齐镜头中线（`50%`），同时将 `XqModeConfig.close_yoffset` 标定为 `0.045f`（实测眼睛在屏高约 `43%`）。
  - 在 `XqCloseCamera.Settle()` 退出完成时恢复进入前保存的模型位置与 `Idle` 动画相位，三轮切换后实测 `XQ_TEST close drift win=0,0 size=0,0 bodyCx=-1 bodyBottom=0 bodyH=-1`。

### 3.2 D-2（对应十项 #5、#6、#7、#8）：`5b1a7f5`
- **提交信息**：`[已编] D-2(十项#5/#6/#7/#8): 去除 .xq-body overflow:hidden 修复透明窗口模板裁剪致四张卡空白; 补齐专注环文案; 近景按钮与竖栏改几何命中`
- **修改文件**：`Assets/Xiaoqi/UI2/xq.uss`、`Assets/Xiaoqi/UI2/Resources/XqClose.uss`、`Assets/Xiaoqi/UI2/XqClosePanel.Cards.cs`、`Assets/Xiaoqi/UI2/XqClosePanel.Controls.cs`、`Assets/Xiaoqi/UI2/XqClosePanel.Anim.cs`
- **根因**：
  1. **四张卡片空白**：`XqUi2Sample` 为保留 3D 人物深度将 `PanelSettings.clearDepthStencil` 设为 `false`；而 `.xq-body` 同时设置了 `border-radius` 与 `overflow: hidden`，触发 Unity 6 UI Toolkit 的模板缓冲（Stencil Buffer）裁剪，在未清空模板缓冲的透明窗口中把卡片内所有子元素全部裁掉（与此前 `XqGlassMenu` `78e598c` 空白卡面同源）。
  2. **按钮与竖栏点击/悬停无反应**：`XqClosePanel` 依赖 `RegisterCallback<ClickEvent>` 与 `MouseEnterEvent`，在透明叠加窗口下不可靠（G64 §4 已指出）。
- **修复**：
  - 去掉 `xq.uss` 与 `XqClose.uss` 中 `.xq-body` 的 `overflow: hidden`，直接在 `.xq-body` 上设半透明玻璃白底与描边，四张卡片及桌宠胶囊文字、折线、圆弧全部正常渲染。
  - ⑥ 一起专注圆环补齐 `25:00\n一起专注 · 占位` 显示。
  - 在 `XqClosePanel.Anim.cs` 的 `TickAnim()` 中加入与 `XqPetHud` 一致的双坐标几何命中 `OverEl()`，接管 `♥`、`今`、`话`、`待`、`设`、`桌宠` 方钮点击及 `工作台` 灰钮悬停提示。

### 3.3 D-3（对应十项 #7、#8）：`6b27ebd`
- **提交信息**：`[已编] D-3(十项#7/#8): 桌宠方钮改方案B常显并待布局有效后再打尺寸日志; 近景期间隐藏桌宠胶囊并纳入近景界面防拖拽命中`
- **修改文件**：`Assets/Xiaoqi/UI2/XqPetHud.cs`
- **根因与修复**：
  - 按 G64 §3.5 D-3 方案 B，将桌宠右下角方钮 `_gate` 在桌宠态下改为常显（近景态或右键菜单打开时隐藏），并待 `_gate.worldBound.width > 1f` 布局生效后再打印 `XQ_HUD gate shown=True ... rect(panel)=1474,147 40x40`（消除原先首帧打印 `0,0 0x0` 的问题）。
  - 近景期间隐藏桌宠态专属胶囊（`_next`、`_here`、`_ringHost`、`Bubble`），避免与近景卡片重叠；并在 `BlocksChrome` 中纳入 `XqClosePanel.OverAny()` 防止点近景卡片时触发窗口拖拽。

### 3.4 D-4（对应十项 #9）：`607f84e`
- **提交信息**：`[已编] D-4(十项#9): 近景数据轮询联动刷新精力卡并补断线清空回退灰字「还没有」; 更新 PROGRESS.md 登记`
- **修改文件**：`Assets/Xiaoqi/UI2/XqCloseDay.cs`、`Assets/Xiaoqi/UI2/XqCloseData.cs`、`Assets/Xiaoqi/UI2/XqClosePanel.Bind.cs`、`docs/unity-progress/2026-11-16-P2P3/PROGRESS.md`
- **根因与修复**：
  - `XqClosePanel.Bind.cs` 的 `OnDataChanged()` 原先只刷新了 `RefreshSay()` 与 `RefreshNext()`，未调用 `RefreshEnergy()`；且 `XqCloseDay` 未映射 `/api/body/day` 中的 `body` 字段。
  - 补齐 `OnDataChanged()` 对 `RefreshEnergy()` 的联动调用与 `XqCloseDay.body` 兜底，断线时三张数据卡（「下一件事」「精力」「她说」）在 3 秒内统一回退显示灰字「还没有」。

---

## 4. 关键日志摘录

### 4.1 测试 A：三轮切换与漂移（`logs/A-close-test-Player.log`）
```text
XQ_TEST close before win=-2560,-683 2560x1440 body=1074..1560 y=-8..880 h=888 mode=Pet
XQ_MODE Pet -> Close
XQ_TEST close round=1 enter accepted=True
XQ_TEST close round=1 entered in=1.03s win=-2560,-683 2560x1440 body=-267..2457 y=-4364..-243 h=4121 mode=Close
XQ_CLOSE anim settled
XQ_TEST close round=1 hold4s win=-2560,-683 2560x1440 body=16..2511 y=-2576..1479 h=4055 mode=Close
XQ_MODE Close -> Pet
XQ_CLOSE anim exit
XQ_CLOSE close
XQ_CLOSE anim done
XQ_TEST close round=1 exited accepted=True win=-2560,-683 2560x1440 body=1072..1560 y=-7..880 h=887 mode=Pet
XQ_MODE Pet -> Close
XQ_TEST close round=2 enter accepted=True
XQ_TEST close round=2 entered in=1.01s win=-2560,-683 2560x1440 body=-268..2457 y=-4488..-367 h=4121 mode=Close
XQ_CLOSE anim settled
XQ_TEST close round=2 hold4s win=-2560,-683 2560x1440 body=16..2511 y=-2576..1479 h=4055 mode=Close
XQ_MODE Close -> Pet
XQ_CLOSE anim exit
XQ_CLOSE close
XQ_CLOSE anim done
XQ_TEST close round=2 exited accepted=True win=-2560,-683 2560x1440 body=1072..1560 y=-7..880 h=887 mode=Pet
XQ_MODE Pet -> Close
XQ_TEST close round=3 enter accepted=True
XQ_TEST close round=3 entered in=1.06s win=-2560,-683 2560x1440 body=-264..2457 y=-4145..-25 h=4120 mode=Close
XQ_CLOSE anim settled
XQ_TEST close round=3 hold4s win=-2560,-683 2560x1440 body=18..2512 y=-2576..1479 h=4054 mode=Close
XQ_MODE Close -> Pet
XQ_CLOSE anim exit
XQ_CLOSE close
XQ_CLOSE anim done
XQ_TEST close round=3 exited accepted=True win=-2560,-683 2560x1440 body=1072..1560 y=-7..880 h=887 mode=Pet
XQ_TEST close drift win=0,0 size=0,0 bodyCx=-1 bodyBottom=0 bodyH=-1
```

### 4.2 测试 B / C / E：脸区自检、竖栏聚焦、悬停提示、数据变化与断线（`logs/B-C-E-Player.log`）
```text
XQ_CLOSE doc=XqUi2Document
XQ_CLOSE ok rail rect=0.023,0.048 0.063x0.904
XQ_CLOSE ok plate rect=0.375,0.826 0.224x0.074
XQ_CLOSE ok btn-pet rect=0.869,0.896 0.041x0.074
XQ_CLOSE ok btn-bench rect=0.919,0.896 0.041x0.074
XQ_CLOSE ok card-next rect=0.113,0.178 0.230x0.237
XQ_CLOSE ok card-energy rect=0.113,0.456 0.210x0.170
XQ_CLOSE ok card-say rect=0.696,0.063 0.267x0.237
XQ_CLOSE ok card-tiny rect=0.729,0.344 0.235x0.237
XQ_CLOSE ok ring rect=0.833,0.652 0.120x0.219
XQ_CLOSE ok tip-bench rect=0.813,0.905 0.098x0.056
XQ_CLOSE anim enter
XQ_CLOSE open
XQ_CLOSE say updated len=29
XQ_CLOSE data next=13:30 数据结构 · 教学楼 B 座 | energy=有点困 | say=主人今天要上课吧？中午食堂有你上次说的那个，我先帮你排队～
XQ_CLOSE anim settled
XQ_CLOSE bench-hover=True
XQ_CLOSE bench-hover=False
XQ_CLOSE focus on
XQ_CLOSE focus off
XQ_CLOSE focus on
XQ_CLOSE focus off
XQ_CLOSE todo placeholder
XQ_CLOSE settings not in this branch
XQ_CLOSE ring placeholder
XQ_CLOSE say updated len=20
XQ_CLOSE data next=16:00 组会讨论 · 理科楼 302 | energy=精神 | say=我把下午组会的提纲整理好啦，等你一起看～
XQ_CLOSE day fail: 502
XQ_CLOSE feed fail: 502
XQ_CLOSE data next=还没有 | energy=还没有 | say=还没有
```

### 4.3 测试 D：四种退出方式与测试 G 蓝色主题 / 测试 H 睡一会回归
- `Esc` 退出（`logs/B-D-Player.log`）：`XQ_MODE Close -> Pet` → `XQ_CLOSE anim exit` → `XQ_CLOSE close` → `XQ_CLOSE anim done`
- 右下 `桌宠` 方钮退出 + 蓝色主题（`logs/G-blue-Player.log`）：`XQ_UI2 on theme=blue class=xq-theme-blue` → `XQ_CLOSE btn pet` → `XQ_MODE Close -> Pet`
- 竖栏顶部 `♥` 退出 + 175% 缩放（`logs/B-175-Player.log`）：`XQ_CLOSE rail pet` → `XQ_MODE Close -> Pet`
- 桌宠态睡一会与常显方钮回归（`logs/H-nap-Player.log`）：`XQ_HUD gate shown=True near=False menuOpen=False rect(panel)=1474,147 40x40 scale=2.00` → `XQ_TEST nap start` → `XQ_TEST nap asleep=True` → `XQ_TEST nap woke asleep=False`

---

## 5. 备注与待 WB 拍板事项（问题单）

### 问题单 #1：近景右下角两个方钮的文案口径（G59-P2 §3.3 vs G62 §3.1）
- **现象**：
  - `WB-G59-P2-close.md` §3.3 与 `WB-G63-P2-oneshot.md` #6 写的是：「⑧ 展开钮（灰，悬停提示『下一阶段开放』）、⑨ 对话钮（灰，悬停提示『还没开放』）」。
  - 但 `WB-G62-p2p3-parallel.md` §3.1（给 DeepSeek 的派工令）写的是：「方钮里『桌宠』调 `Xiaoqi.Mode.XqMode.Set(Xiaoqi.Mode.XqView.Pet)`，『工作台』灰」，且 `WB-G63-P2-oneshot.md` #8 要求「右下方钮选『桌宠』退出」。
  - 当前 `XqClosePanel.Controls.cs` 实现的是 G62 §3.1 口径：左方钮为可点的「桌宠」（点后退出近景回桌宠），右方钮为灰色的「工作台」（悬停提示「还没开放」）。
- **复现步骤/启动参数**：`-xq-mode close`
- **相关日志**：`XQ_CLOSE ok btn-pet rect=0.869,0.896 0.041x0.074`、`XQ_CLOSE ok btn-bench rect=0.919,0.896 0.041x0.074`、`XQ_CLOSE btn pet`
- **截图**：`shots/B-close-100.png`、`shots/B-close-tip-bench.png`
- **怀疑（推测）**：G62/G63 后续把「形态切换入口只留右下方钮」落到了近景右下角的两个方钮上，因此 DeepSeek 做成了「桌宠 / 工作台（灰）」。因两份文档口径存在差异，我未擅自改动按钮文案，保持 G62 §3.1 现有「桌宠 / 工作台（灰）」实现。
- **是否阻塞 P2**：否（请 WB 复核确认是否保持现样）。
