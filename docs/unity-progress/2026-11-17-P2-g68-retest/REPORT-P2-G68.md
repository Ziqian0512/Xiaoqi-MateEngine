# G68 · P2 近景回归与 R-5 复测报告

- **执行人**：AGY
- **日期**：2026-11-17
- **被验对象**：`origin/xiaoqi/int-all` @ `a5fe7f0c`（含 `c222ee9` 修复）
- **`.meta` 提交分支**：`origin/xiaoqi/p2-agy4` @ `c6938c3`（仅补生成 `Assets/StreamingAssets/Xiaoqi/fake_day.meta`）
- **证据目录**：`docs/unity-progress/2026-11-17-P2-g68-retest/`

---

## 1. 编译（G68 §3）

- **结果**：**通过**（`XiaoqiBuild.Win64`，0 compile errors，0 warnings，耗时 41s，`logs/build-summary-a5fe7f0.md`）。未修改任何定位/界面/宿主机逻辑文件，未提交 prefab / ProjectSettings / 场景改动。

---

## 2. `-xq-close-test` 近景回归：发现 `MeHost.EnsureOnScreen` 新回归

- **眼位与相对坐标仍准确**（`logs/R1-regression-close-test-Player.log`）：
  - `XQ_PANEL ScreenToPanel flipsY=False origin=0.0,0.0 H=1440`
  - 3 轮切入耗时：`1.03s`、`1.01s`、`1.01s`
  - `XQ_CLOSE eyes` 进入稳定 `view=(0.50,0.57)/(0.50,0.55)/(0.50,0.57)`，退出前三轮均为 `view=(0.50,0.57)`（目标 `(0.50,0.57)`）
  - 退出后身体相对位置偏差：`bodyCx=0 bodyBottom=0 bodyH=-1`
- **阻断问题（窗口跨屏漂移 `drift win=2240,236`）**：
  - `MeHost.EnsureOnScreen()` 改按身体中心 `pt = (wr.left + bodyR.center.x, wr.top + Screen.height - bodyR.center.y)` 调 `MonitorFromPoint(pt)`；
  - 进入 `Close` 态及退出过渡帧期间 `bodyR` 大幅超出屏幕矩形，导致 `pt` 落到屏幕外（`pt=-1325,5792`、`1055,1452`、`1082,12658`、`767,7184`），反复触发 `XQ_WATCH body outside` → `XQ_HOST fix window` → `MoveToCenter()`（`XQ_HOST center -327,-447 2574x1479`），直接把副屏 `DISPLAY2`（`-2567,-683`）上的窗口拉回主屏 `DISPLAY1`（`-327,-447`）：
```text
XQ_TEST close before win=-2567,-683 2574x1479 body=1172..1427 y=6..462 h=455 mode=Pet
XQ_MODE Pet -> Close
XQ_WATCH body outside pt=-1325,5792
XQ_HOST fix window inside=False tiny=False rect=-2567,-683
XQ_HOST resize 2574x1479
XQ_HOST center -327,-447 2574x1479
...
XQ_WATCH body outside pt=1055,1452
XQ_HOST fix window inside=False tiny=False rect=-327,-447
XQ_HOST center -327,-447 2574x1479
...
XQ_TEST close drift win=2240,236 size=0,0 bodyCx=0 bodyBottom=0 bodyH=-1
```

---

## 3. R-5（近景 `⚙` 打开设置页）与退出检查：**无法验证**

- **R-5**：因主人在公司无法操作家里物理桌面，且 Antigravity `run_command` 处于隔离桌面会话中（实测 `CopyFromScreen` 报 `The handle is invalid`，且无法向物理桌面注入 `SendInput`），经主人授权先标为 **无法验证** 并推送当前已测阻断回归给 WB。已准备好桌面自跑脚本供主人回到物理桌面时补测：`C:\Users\10943\AppData\Local\Temp\xq-g68\run_r5_and_prep_b1.ps1`。
- **退出空指针（`SetOtherQuadsActive`）**：真实桌面托盘退出场景暂 **无法验证**（隔离桌面报 `Failed to add system tray icon`、`CloseMainWindow` 未触发托盘退出）；`-xq-close-test` 通过 `Application.Quit()` 正常退出（ExitCode 0）日志中未出现 `NullReferenceException` / `SetOtherQuadsActive`，只读核对 `AvatarWindowHandler.cs:857` 已含 `if (otherQuadGOs[i] == null) continue;`。
