# 小祈晚上物理桌面验收清单（基于 `int-all @ 8dbbd5e2` / G74 W2）

- **可运行包固定路径**：`E:\dsh-pet-projects\Xiaoqi-Tonight\Xiaoqi\Xiaoqi.exe`
- **一键启动脚本（三个 `.cmd` 均已带 `-xq-bench`）**：
  - 主验收（`pink` 默认）：`E:\dsh-pet-projects\Xiaoqi-Tonight\1-启动验收(pink).cmd`
  - 主题 `blue` 验收：`E:\dsh-pet-projects\Xiaoqi-Tonight\2-主题blue.cmd`
  - 主题 `violet` 验收：`E:\dsh-pet-projects\Xiaoqi-Tonight\3-主题violet.cmd`
- **测完发回日志**：`C:\Users\10943\AppData\LocalLow\Xiaoqi\Xiaoqi\Player.log`

---

## 本轮重点验收清单（状态：**待晚上主人验收**）

1. **两条入口都能出板**：
   - 从**桌宠态**右键/快捷入口直接点「工作台」；
   - 从**近景**点右下角「工作台」按钮。
   - 若任一路仍不显示白板，请发回 `Player.log` 里的 `XQ_BENCH diag` 那一行。
2. **新版布局（「置底右侧立绘 + 左侧白板叠卡」）**：她在屏幕右侧约 `78%` 屏高、脚在屏幕下沿外置底不动、身体不歪；4:3 白板在左（屏高 `74%` / 宽 `≤62%`），白板底下露出 3 层依次滑出的叠层卡片；板和她不重叠或仅半透明薄雾重叠。
3. **光标/陀螺仪视差**：鼠标光标移到屏幕最左 vs 最右，她随光标轻转身（最多 `7°`）且侧栏 / 板 / 行 / 叠卡按深度反向分层错位，体感是否舒服（太晃可随时调 `bench_parallax_deg` 或关 `bench_parallax`）。
4. **拖动、控件、禁右键与退出回正**：
   - 按住**左侧药丸栏空白处**或**白板顶部 98px**拖动整板，板动而**她不动**（日志 `XQ_BENCH board drag start/end`）；
   - 点 `×` / 薄雾玻璃开关 / 重试 / 侧栏 2 号触发对应日志且**无** `XQ_HUD down`；工作台内右键她不弹桌宠菜单；
   - 桌宠 ↔ 近景 ↔ 工作台来回切几轮，退出工作台后她的站姿与朝向完整还原。
5. **手感与质感**：叠层卡片质感、薄雾质感、视差动效是否顺畅与还差哪点（均待晚上主人实机确认）。
