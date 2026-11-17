# P1 / P2 / P3 关卡证据总表（基于 `int-all @ 18097d60` + W4 `2ac1238b`）

> 规则：每项仅填**本次或前序已实证落盘的日志行 / 截图路径**；受隔离桌面限制或未实机点击的物理项一律标「**待主人**」。

---

## 一、P1（桌宠态）

| 验收项 | 实证日志行 | 截图路径（相对 `2026-11-16-P1/`） | 状态 / 待主人 |
|---|---|---|---|
| 启动唤醒与透明窗口/主题 | `XQ_START waking→ok 2.5s` / `XQ_UI2 on theme=pink ... clearColor=False clearDepth=False hook=GetOnOpaquePixel` | `evidence/2026-11-17-B1-r16/l2_settings.png` | 自动化已证；三主题观感待主人 |
| 右键菜单主卡与全行不翻车（`#18`） | `XQ_MENU2 open side=L body=865..1116 scale=1.00` / `XQ_MENU2 test-open mode=Pet` | `evidence/2026-11-17-B1-r16/l2_settings.png` | 物理右键点选全行命中「待主人」 |
| 设置二级 6 项 + 三级级联（W5） | `XQ_MENU2 settings page=外观..关于` / `XQ_SET2 open cascade=True` / `XQ_SET2 cascade place page=外观 x=15 y=780 size=380x276 side=L limit=1918x1080 squeeze` | `l2_settings.png`、`l3_0_look.png`~`l3_5_about.png`、`blue-l3_look.png`、`violet-l3_look.png` | 三主题布局已证；悬停 1s / 全控件点击落盘「待主人」 |
| 设置关闭与拦截 | `XQ_SET2 close` + `XQ_MENU2 close by=esc` / `OpenNow=True -> False` | `evidence/2026-11-17-B1-r16/w5-probe-pink-Player.log` | 点空白不穿/右键关/拖她拦截恢复「待主人」 |
| 独立设置卡（托盘/旧路径，顶部 6 标签） | `XQ_SET2 open cascade=False` / `XQ_SET2 place side=L wa=7,48 1911x1032 size=380x219` | `evidence/2026-11-17-B1-r16/standalone.png` | 托盘点击切换 6 标签「待主人」 |
| 坐窗睡 / 今天卡 / 拖动手感（`#13` `#15` `#17`） | —（W2 主人口头已确认「设/今/坐窗」前序修复） | — | 物理手感与完整 B1 终验「待主人」 |

---

## 二、P2（近景态）

| 验收项 | 实证日志行 | 截图路径（相对 `2026-11-16-P1/`） | 状态 / 待主人 |
|---|---|---|---|
| 3 轮进出漂移 = 0（`-xq-close-test`） | `XQ_TEST close round=1..3 entered in=1.01s~1.02s` / `XQ_TEST close drift win=0,0 size=0,0 bodyCx=0 bodyBottom=0 bodyH=0` | `evidence/2026-11-17-B1-r16/w5-close-test-18097d6-Player.log` | 自动化已证 |
| 近景右键菜单不压脸（`-xq-menu-test`） | `XQ_MENU2 test-open mode=Close` / `XQ_MENU2 place close-face side=R l1=1202,194 limit=1920x1080` | `evidence/2026-11-17-B1-r16/w5-close-menu-test-18097d6-Player.log` | 视觉不压脸「待主人」 |
| 近景右键展开设置三级卡（W5） | `XQ_SET2 cascade place page=行为 x=1530 y=517 size=380x366 side=R limit=1918x1080 squeeze` | `evidence/2026-11-17-B1-r16/close_l3_behave.png` | 物理实操（含与右侧装饰卡片是否遮挡判断）「待主人」 |
| 2 秒站定不抽搐 / 5 分钟近景卡片数据 / R-5 体感 | — | — | 主人「可以」口头确认前标「待主人」 |

---

## 三、P3（工作台，W4 `2ac1238b` 已口头验收，W5 回归未改）

| 验收项 | 实证日志行 | 截图路径（相对 `2026-11-16-P1/`） | 状态 / 待主人 |
|---|---|---|---|
| 半透毛玻璃 + 三主题 + 无裁切（`#22`） | `XQ_BENCH open k=1.332 glass=True` / `XQ_BENCH diag ... op=1.00 builtPx=799` | `evidence/2026-11-17-B1-r15/w4-pink-02.png`、`w4-blue-02.png`、`w4-violet-02.png`、`r16/w4_bench_ok.png` | W4 主人实机已口头确认 |
| 胸像构图（1.50）+ 全身缩进（`mode=1`） | `XQ_CLOSE eyes view=(0.37,0.72) target=(0.37,0.72) bench frac=1.53/1.50` / `mode1: bench frac=0.69/0.68` | `evidence/2026-11-17-B1-r15/w4-pink-02.png`、`w4-mode1-02.png` | W4 主人实机已口头确认 |
| 反向平移视差 + 她 3D 转身 | `XQ_BENCH parallax src=mouse` / `parL stTx=16 rot=189.0` vs `parR stTx=-16 rot=172.0`、`stRot=0.00 stScale=1.000` | `evidence/2026-11-17-B1-r15/w4-pink-06_parL.png`、`06b_parC.png`、`07_parR.png` | W4 主人实机已口头确认 |
| 整板拖动同步 + 5 轮切出恢复 | `round1..5-pet: idle=0.00 speed=1.00 rot=(0.0, 180.0, 0.0)` | `evidence/2026-11-17-B1-r15/w4-pink-08_drag.png` | 物理拖左栏/顶部与三路入口按钮「待主人」最终确认 |
