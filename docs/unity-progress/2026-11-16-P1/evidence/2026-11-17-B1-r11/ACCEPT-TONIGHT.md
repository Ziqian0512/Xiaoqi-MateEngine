# 小祈晚上物理桌面验收清单（基于 `int-all @ bd024f74`）

- **可运行包固定路径**：`E:\dsh-pet-projects\Xiaoqi-Tonight\Xiaoqi\Xiaoqi.exe`
- **一键启动脚本（三个 `.cmd` 均已带 `-xq-bench`）**：
  - 主验收（`pink` 默认）：`E:\dsh-pet-projects\Xiaoqi-Tonight\1-启动验收(pink).cmd`
  - 主题 `blue` 验收：`E:\dsh-pet-projects\Xiaoqi-Tonight\2-主题blue.cmd`
  - 主题 `violet` 验收：`E:\dsh-pet-projects\Xiaoqi-Tonight\3-主题violet.cmd`
- **测完发回日志**：`C:\Users\10943\AppData\LocalLow\Xiaoqi\Xiaoqi\Player.log`

---

## 本轮重点验收清单（状态：**待晚上主人验收**）

1. **工作台大小与贴边互动**：进工作台看她身高是否缩回桌宠大小（约屏高 34%，~370px）、只贴在 4:3 白板的上沿/左沿/右沿（探头/露半身），不会掉到板下方；拖动她只吸附三条边、点她能换姿势；4:3 白板和左侧药丸栏完整无裁切溢出。
2. **工作台点击响应与禁右键**：点面板右上 `×`、薄雾玻璃开关、侧栏 2 号页签（弹「这一页下一期开放」提示）及离线条「重试」，确认响应灵敏、日志有 `XQ_BENCH btn close / glass= / nav 2 / retry` 且**无** `XQ_HUD down`；工作台里右键小祈**不再弹出**桌宠右键菜单。
3. **模式来回切换**：桌宠 ↔ 近景 ↔ 工作台来回切（含点 × / Esc），确认动效顺畅、无 Exception。
4. **物理手感/观感**：拖动手感、薄雾玻璃开关动效、透明处穿透、她与面板的视觉比例协调度（均待晚上主人实机确认）。
