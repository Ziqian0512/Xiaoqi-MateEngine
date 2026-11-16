# R2 回报（G61-A v2.6 §2.10 + t35u 到家编译）

> 2026-11-16 14:50（Asia/Shanghai，UTC+8）· Grok

- **R2-1 语法体检**：`xiaoqi/int-d1` @ `7ff2216a`，`Assets/Xiaoqi/**/*.cs` 一共 50 个文件，**语法错误 0 个**。详见 `R2-1-syntax.md`，工具是 tree-sitter-c-sharp，只查语法。已作废的 `ee26640`、`bcbfa2da`、`a936ed09` 之前也都是 0 个。
- **R2-2 编译 `xiaoqi/int-d1` @ `7ff2216a`**（14:50）：**无法送达**。传输包已经准备好（git bundle，`^a73dd06 xiaoqi/int-d1`，head `7ff2216a`，168 KB）。14:50 查过，Inori02 仍然离线（从 13:04 起），所以当时没有编译，没有跑 `-xq-brain-fast`（B3）和 `-fake-stage`（B7）验证，`TODO(编译员)` 的 API 当时未确认。
- **R2-3 拼图**（14:50）：没做，因为它依赖 R2-2。

需要主人在屏幕前才能做的：B1 人工验收 8 项（挂起中）。

---

> 2026-11-16 19:26（Asia/Shanghai，UTC+8）· Grok · Inori02 到家后补做

机器：INORI02。显示器：DISPLAY1 主屏 1920×1080 @ (0,0)；DISPLAY2 `\\.\DISPLAY2` 实际 bounds={X=-2560,Y=-683,Width=2560,Height=1440}（工作区高度 1392），不是任务书里旧的 2048×1152。缩放 100%（`XQ_BENCH dpi=96 scale=1.00`）。冒烟和无人值守截图都取 DISPLAY2，没有改系统 DPI。原来的 `E:\dsh-pet-projects\Xiaoqi-ME` 仍在 `a73dd06` / `cursor/p1-1-ui-clickthrough-bdd1`，只读，git 状态未改（仍是那几项已知遗留）。

## A 编译 · `xiaoqi/int-d1` @ `7ff2216abb714875225891f3809309b7150e587f`

目录：`E:\dsh-pet-projects\Xiaoqi-ME-int`。Unity 6000.2.6f2，`xq-verify.ps1 -NoGit`（临时目录副本只改了冒烟截 DISPLAY2，不进 git）。

- 编译：**通过**。`Build Finished, Result: Success.` Unity 退出码 0，耗时 **211 秒**，error CS **0** 个，警告 282。`Build\Xiaoqi\Xiaoqi.exe` 存在（667648 字节，19:03:07）。全程约 300 秒（含 4 场景冒烟），脚本退出码 0。
- 编译错误清单：无。
- 修了哪些：无（没有改逻辑，也没有改 API 调用）。
- `TODO(编译员)` 7 处（`B4-REPORT.md` 第三节：`schedule.Execute().Every()`、`GUIUtility.systemCopyBuffer`、`CapturePointer/ReleasePointer`、`Length.Percent`、`TextField/isPasswordField/SetValueWithoutNotify`、`KeyDownEvent`、`FocusOutEvent`）：**编过**，未改调用。
- 冒烟（DISPLAY2 2560×1440）：base / nap / theme-blue / settings-v2 均为 20 秒后仍存活，exception-ish 行 0。settings-v2 日志有 `XQ_SET2 doc=XqUi2Document`、`XQ_SET2 open`、`XQ_SET2 apply start done`。
- 构建生成但**未提交**的文件：11 个新 `.meta`（BodyDirector / BrainLog / IdleDirector / XqClipSheet / XqHoverReact / XqPoseBlend / XqStageBody / XqSettings.uss / XqSettingsApply / XqSettingsPanel / XqSettingsRows），以及 prefab / ProjectSettings 等构建改写。不往 `xiaoqi/int-d1` 推。摘要见 `evidence/P2P3-home/summary-A-7ff2216.md`。

## B 编译 · `xiaoqi/p2` @ `012369aa868e9831172402fde4a04915472fcaa3`

目录：`E:\dsh-pet-projects\Xiaoqi-ME-p2`（新目录，bundle 取出后 detach 到该提交；Library 从 `Xiaoqi-ME` robocopy）。

- 编译：**通过**。`Build Finished, Result: Success.` Unity 退出码 0，耗时 **264 秒**，error CS **0** 个，警告 282。脚本退出码 0，全程约 358 秒。
- 编译错误清单：无。
- 修了哪些：无。p2 的 `TODO(编译员)`（`XqClosePanel.Anim.cs` 的 `schedule.Execute().Every()`）：**编过**。
- 冒烟：4 场景均 20 秒后存活，exception-ish 行 0。p2 不含设置页，settings-v2 场景只是带了该参数启动。
- 构建新 `.meta`（Mode/*、XqClose* 等）未提交。摘要见 `evidence/P2P3-home/summary-B-012369a.md`。

## 二、无人值守验证

### 1）B · `Xiaoqi.exe -xq-mode close`（19:16:23–19:16:42，Asia/Shanghai）

结论：日志符合期望（`XQ_MODE Pet -> Close`，`XQ_CLOSE doc=XqUi2Document`，`XQ_CLOSE violate` 0 条，无 `XQ_HOST_MISS`，exception 0）。DISPLAY2 全屏截图里看不到近景面板和角色。

关键日志：

```
XQ_MODE Pet -> Close
XQ_CLOSE doc=XqUi2Document
XQ_CLOSE ok rail rect=0.023,0.048 0.069x0.904
XQ_CLOSE ok plate rect=0.375,0.826 0.244x0.074
XQ_CLOSE ok btn-pet rect=0.869,0.896 0.044x0.074
XQ_CLOSE ok btn-bench rect=0.919,0.896 0.044x0.074
XQ_CLOSE ok card-next rect=0.113,0.178 0.250x0.237
XQ_CLOSE ok card-energy rect=0.113,0.456 0.228x0.170
XQ_CLOSE ok card-say rect=0.696,0.063 0.290x0.237
XQ_CLOSE ok card-tiny rect=0.729,0.344 0.255x0.237
XQ_CLOSE ok ring rect=0.833,0.652 0.131x0.219
XQ_CLOSE ok tip-bench rect=0.804,0.905 0.106x0.056
XQ_CLOSE anim enter
XQ_CLOSE open
XQ_CLOSE anim settled
XQ_CLOSE day fail: 502
```

`violate_count=0`。另有 `XQ_CLOSE day fail: 502`（本机没有可用的 day 接口）。截图：`evidence/P2P3-home/close-1.5s.png`（DISPLAY2 2560×1440，非黑屏；画面是任务管理器和已打开的窗口，近景 UI 未出现在这张图里）。

### 2）B · `Xiaoqi.exe -xq-mode bench -xq-bench`（第二次 19:18:35–19:18:56；第一次 19:17:18 窗口未成功移到 DISPLAY2，坐标与第二次相同）

结论：期望的日志行都出现；`XQ_BENCH sit ok=False`；结束 Edge（只杀了命令行带 `%LOCALAPPDATA%\Xiaoqi\edge-profile` 的进程，用户其它 Edge 未动）后出现 `XQ_BENCH closed: process exited` 和 `XQ_MODE Bench -> Pet`，Xiaoqi 进程未崩。工作台窗口坐标落在正数区域，DISPLAY2 截图里看不到 Edge 窗口。

配置：exe 旁边的 `xiaoqi_config.json` 里本来就有本地 `server`（loopback，长度 21），token 为空，未改。

关键日志（第二次，与第一次相同）：

```
XQ_MODE Pet -> Bench
XQ_BENCH start pid=57004 want=187,426 2186x986 (token hidden)
XQ_BENCH dpi=96 scale=1.00 size=2186x986
XQ_BENCH placed before=187,426 2186x986 after=187,426 2186x986 want=187,426 2186x986
XQ_BENCH sit ok=False frac=0.49
XQ_BENCH closed: process exited
XQ_MODE Bench -> Pet
```

exception 0。`before` / `after` / `want` 三个值都是 `187,426 2186x986`。这组数字等于以原点 (0,0)、尺寸 2560×1440 套默认比例（left=0.073, top=0.296, w=0.854, h=0.685），而 DISPLAY2 的实际原点是 (-2560,-683)。即便第二次已把 Unity 窗口 `SetWindowPos` 到 (-2560,-683)，放置坐标仍不变。截图：`evidence/P2P3-home/bench-2s.png`（DISPLAY2，非黑屏，看不到 Edge 工作台窗口）。

### 3）A · B3 / B7（`emotion_line` 跑完已改回 false）

**B3 `-xq-brain-fast` 60 秒（19:19:18–19:20:21）**

结论：(a) 有 `XQ_IDLE bound anim=CustomVRM(Clone) cur=0 pool=10 switch_time=1e9`，无 `XQ_IDLE_MISS`；(b) `brain.log` 里 `evt=idle` **11** 行（≥8），每行都有 `src=`；(c) 无 exception / NullReference / `XQ_BRAIN write` 警告。另有一行 `src=random evt=skip reason=not-idle-state`（19:19:23）。

```
XQ_IDLE bound anim=CustomVRM(Clone) cur=0 pool=10 switch_time=1e9
XQ_BRAIN 2026-11-16 19:19:33 src=random evt=idle from=0 pick=7:PET_IDLE_4 fade=0.76 tier=normal ... why=timer
…（共 11 条 evt=idle，19:19:33–19:20:16）
```

完整 `brain.log`：`evidence/P2P3-home/brain-brain-fast.log`。

**B7 `emotion_line=true` + `-xq-brain-fast` + `-fake-stage`，各 ≥40 秒**

- `sit_write`：符合。`2026-11-16 19:20:42 src=server evt=stage where=dorm pose=sit_write tier=calm label=fake sit_write sleepy=0`；另有 `XQ_STAGE pose=sit_write where=dorm tier=calm`、`XQ_PRESENCE dorm=True sleep=False seg=fake sit_write`。exception 0。
- `pet_typing`：符合。`2026-11-16 19:21:39 src=server evt=typing bubble=1`；同时 `src=server evt=stage where=dorm pose=lie_phone tier=calm label=回邮件 sleepy=0`。exception 0。
- `pet_sleepy`：符合时间窗口（从启动 19:22:28 起）。`19:22:30 src=server evt=stage where=dorm pose=sit_idle tier=calm label=- sleepy=80`；约 11 秒后 `19:22:39 src=server evt=self_sleep start sleepy=80 hold=8 min=0.4` 与 `XQ_NAP start min=0.4`；约 35 秒后 `19:23:03 src=server evt=self_sleep wake cooldown_min=0.5` 与 `XQ_NAP end`。exception 0。

完整 brain.log：`brain-b7-sit.log` / `brain-b7-type.log` / `brain-b7-sleepy.log`。

## 三、R2-3 拼图（A 的 exe，只带 `-xq-clipsheet`）

结论：**78 条剪辑，78 条可用**（PNG 均非纯色；已转 JPEG 宽≤960、质量 80，打包 `evidence/clipsheet/clipsheet.zip`）。

```
XQ_CLIPSHEET start avatar=VRMModel clips=78 dir=C:\Users\10943\AppData\Local\Xiaoqi\clipsheet
…（78 条 XQ_CLIPSHEET clip <name> len=…）
XQ_CLIPSHEET done clips=78
```

进程自行退出，exception 0。`clips.csv` 78 行，每条 `frames=3`。用 24 点采样检查，纯色 0 张。`PET_SLEEPING` 的三帧画面是站立姿势（不是躺姿），记一笔事实，未改源码。

## 其它

- 未做 B1 人工验收 8 项。
- 未合并任何分支，未推 `xiaoqi/p1` / `main` / `cursor/*` / `xiaoqi/int-d1` / `xiaoqi/p2`。
- 证据待推到 `xiaoqi/b1-grok`（本回合执行侧没有可用的 CloudAgent 工具，文件已放在盒子 `/workspace/r2/home/push/`）。
- A 的 `xiaoqi_config.json` 在 B7 后已把 `emotion_line` 改回 `false`（token 仍为空，server 仍是 loopback）。脚本曾误把备份覆盖，是按字段改回的，文件可能被 `ConvertTo-Json` 重新排版，未提交。
