# 小祈晚上物理桌面验收清单（基于 `int-all @ 82367953` / G74 W1c）

- **可运行包固定路径**：`E:\dsh-pet-projects\Xiaoqi-Tonight\Xiaoqi\Xiaoqi.exe`
- **一键启动脚本（三个 `.cmd` 均已带 `-xq-bench`）**：
  - 主验收（`pink` 默认）：`E:\dsh-pet-projects\Xiaoqi-Tonight\1-启动验收(pink).cmd`
  - 主题 `blue` 验收：`E:\dsh-pet-projects\Xiaoqi-Tonight\2-主题blue.cmd`
  - 主题 `violet` 验收：`E:\dsh-pet-projects\Xiaoqi-Tonight\3-主题violet.cmd`
- **测完发回日志**：`C:\Users\10943\AppData\LocalLow\Xiaoqi\Xiaoqi\Player.log`

---

## 本轮重点验收清单（状态：**待晚上主人验收**）

1. **工作台大小（约屏高 80%）**：进工作台看 4:3 白板 + 左侧药丸栏大小是否合适（相对上一版放大至屏高 ~80%，上下略留空），文字内容无溢出。
2. **拖板整体移动 + 她同步跟着**：按住**左侧药丸栏空白处**或**白板顶部标题区（98px）**拖动，确认整个工作台跟着移动、她保持贴边一起动、拖到屏幕边缘不会出屏，且日志有 `XQ_BENCH board drag start` / `end x= y=`、**无** `XQ_HUD down`。
3. **贴边歪头 / 转身与点/拖她换边换姿**：她贴左/右沿时看歪头+转身朝板是否自然、贴上沿只露头是否自然；按住她拖动可在上/左/右三边切换、单击她切换姿势（0/1）。
4. **控件响应、禁右键与退出回正**：点 `×`、薄雾玻璃开关、侧栏 2 号页签、离线条「重试」正常触发 `XQ_BENCH btn close / glass= / nav 2 / retry`；工作台里右键她不弹桌宠菜单；近景 ↔ 工作台 ↔ 桌宠来回切 5 轮，退出工作台后她朝向/旋转完全还原。
5. **物理手感/视觉观感**：整板拖动手感、薄雾动效、透明处穿透与贴边姿势观感（均待晚上主人实机确认）。
