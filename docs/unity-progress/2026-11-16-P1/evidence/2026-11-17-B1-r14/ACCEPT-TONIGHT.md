# 小祈晚上物理桌面验收清单（基于 `int-all @ 10530de5` / G74 W3）

- **可运行包固定路径**：`E:\dsh-pet-projects\Xiaoqi-Tonight\Xiaoqi\Xiaoqi.exe`
- **一键启动脚本（三个 `.cmd` 均已带 `-xq-bench`）**：
  - 主验收（`pink` 默认）：`E:\dsh-pet-projects\Xiaoqi-Tonight\1-启动验收(pink).cmd`
  - 主题 `blue` 验收：`E:\dsh-pet-projects\Xiaoqi-Tonight\2-主题blue.cmd`
  - 主题 `violet` 验收：`E:\dsh-pet-projects\Xiaoqi-Tonight\3-主题violet.cmd`
- **AGY 改良对比包（可选对比体验）**：`E:\dsh-pet-projects\xq-builds\w3-agy-alt\Xiaoqi.exe`（分支 `xiaoqi/agy-w3-alt`）
- **测完发回日志**：`C:\Users\10943\AppData\LocalLow\Xiaoqi\Xiaoqi\Player.log`

---

## 本轮重点验收清单（状态：**待晚上主人验收**）

1. **三路都能进工作台**：桌宠右键 →「工作台」、桌宠右下角方钮 →「工作台」、近景右下角「工作台」按钮（多屏下板高约为所在屏 74% ≈ 799px，不再越出下沿）。
2. **整体半透 + 小祈固定在板后**：默认整块半透毛玻璃（已去掉板底叠卡），她以固定站姿立在板正后方、头顶略探出板上沿，工作台期间不随机切换动作。
3. **整体拖动**：按住**左侧药丸栏空白处**或**白板顶部 98px**拖动，板和她作为一个整体一起移动，松手停住且不出屏。
4. **刚体统一视差**：光标左右移动时，左栏与右板作为同一个舞台整体平移+轻倾转（不再左栏单独滑走），背后小祈随光标真 3D 转身（最多约 `12°`）。
5. **控件、右键与退出恢复**：点 `×`/玻璃开关/重试/侧栏 2 号有响应且不漏 `XQ_HUD down`；工作台内右键不弹桌宠菜单；退出回桌宠后她的朝向与待机动作恢复正常。
6. **手感、质感与动效**：毛玻璃通透感、拖动手感、视差动效顺滑度（均待晚上主人实机确认，可顺带对比 `xq-builds\w3-agy-alt`）。
