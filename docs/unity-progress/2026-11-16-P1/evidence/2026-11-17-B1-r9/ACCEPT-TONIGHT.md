# 小祈第二轮实机验收清单（基于 `int-all @ 1454ba8f`，含 #13~#17 修复与 `-xq-bench`）

- **可运行包固定路径**：`E:\dsh-pet-projects\Xiaoqi-Tonight\Xiaoqi\Xiaoqi.exe`
- **一键启动脚本（三个 `.cmd` 均已校验带 `-xq-bench`）**：
  - 主验收（`pink` 默认）：`E:\dsh-pet-projects\Xiaoqi-Tonight\1-启动验收(pink).cmd`
  - 主题 `blue` 验收（第 3 项）：`E:\dsh-pet-projects\Xiaoqi-Tonight\2-主题blue.cmd`
  - 主题 `violet` 验收：`E:\dsh-pet-projects\Xiaoqi-Tonight\3-主题violet.cmd`
- **测完发回日志**：`C:\Users\10943\AppData\LocalLow\Xiaoqi\Xiaoqi\Player.log`（以及如有上一轮 `Player-prev.log`）

---

## 主人复测 10 项清单（状态：**待晚上主人验收**）

1. **设置页内点击不透传**：点「设置」打开设置页 → 在页内点任意位置或开关，她**不动**、不弹今天卡；日志不应出现 `XQ_HUD down` 紧跟 `drag_end`。
2. **点设置页外不透传**：设置页开着，点页外关闭 → 页关闭，她**不动**、不弹今天卡。
3. **`-xq-theme blue` 主题统一**：用 `2-主题blue.cmd` 启动打开设置页 → 设置页为蓝色主题（日志 `picked=…xq-theme-blue`）。
4. **设置页大小与摆位**：设置页大小适中、不遮她、不被裁；她在屏幕左/右/中三种位置各开一次（日志 `XQ_SET2 place side=… wa=… size=… dpi=…`）。
5. **设置页与右键菜单互斥**：设置页开着右键她 → 设置页自动关、菜单开；菜单开着点设置 → 菜单关、设置开。
6. **「今天」卡片摆位**：桌宠态把她拖到屏幕左侧 → 点她出「今天」，卡片在她右边或左边，都在屏幕内、不压她。
7. **近景交互**：左栏不再是巨型长条；右键她**不出**桌宠菜单；做动作时身体不抽搐。
8. **坐窗睡不掉下窗**：把她坐到窗口上 → 右键「睡一会」，不掉下窗口并观察是否进睡姿；叫醒/拖走后回到坐窗（日志 `XQ_SLEEP sit-keep`、`XQ_SLEEP sit-check sleeping=…`、`XQ_SLEEP sit-restore`）。
9. **副屏 175% 缩放适配**：切到副屏 175% 看设置页/今天卡/菜单大小合理，工作台方钮大小与位置正确（日志 `XQ_UI2 on … scale=` 约 `1.00` / `1.75`）。
10. **工作台入口与操作**：带 `-xq-bench` 启动后右键「让她…→工作台」（或 `Ctrl+Alt+3`），确认工作台能进，右外侧双方钮可点/灰置悬停、拖动跟随、全屏隐藏正常。
