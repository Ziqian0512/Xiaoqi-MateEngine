# 小祈晚上物理桌面验收清单（基于 `int-all @ aab9080f` / G74 W5b）

- **可运行包固定路径**：`E:\dsh-pet-projects\Xiaoqi-Tonight\Xiaoqi\Xiaoqi.exe`
- **一键启动脚本（三个 `.cmd` 均已带 `-xq-bench`）**：
  - 主验收（`pink` 默认）：`E:\dsh-pet-projects\Xiaoqi-Tonight\1-启动验收(pink).cmd`
  - 主题 `blue` 验收：`E:\dsh-pet-projects\Xiaoqi-Tonight\2-主题blue.cmd`
  - 主题 `violet` 验收：`E:\dsh-pet-projects\Xiaoqi-Tonight\3-主题violet.cmd`
- **测完发回日志**：`C:\Users\10943\AppData\LocalLow\Xiaoqi\Xiaoqi\Player.log`

---

## 本轮重点验收清单（状态：**待晚上主人验收**）

1. **近景抬层与关闭恢复**：近景下右键 → 设置 → 行为，二级与三级卡必须盖在「今日小事 / 她说 / 25:00」等装饰卡之上（日志 `XQ_MENU2 lift hud above=…`）；菜单关闭后装饰卡与近景左栏点击正常。
2. **外观「模型」行不折行**：桌宠态右键 → 设置 → 外观，「模型」行不再折行，提示仅显示 `16.vrm` 之类文件名。
3. **三主题级联、控件点击与 W4 工作台回归**：`pink/blue/violet` 级联毛玻璃与控件点击落盘、工作台 W4 保持不变。
4. **手感、质感与动效**：均待晚上主人实机确认。
