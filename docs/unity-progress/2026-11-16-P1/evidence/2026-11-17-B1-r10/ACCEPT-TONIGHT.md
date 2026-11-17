# 小祈晚上物理桌面验收清单（基于 `int-all @ 468a93a2` + `XqBenchPanel` 编译修正，含原生工作台 & #18/#20）

- **可运行包固定路径**：`E:\dsh-pet-projects\Xiaoqi-Tonight\Xiaoqi\Xiaoqi.exe`
- **一键启动脚本（三个 `.cmd` 均已带 `-xq-bench`）**：
  - 主验收（`pink` 默认）：`E:\dsh-pet-projects\Xiaoqi-Tonight\1-启动验收(pink).cmd`
  - 主题 `blue` 验收：`E:\dsh-pet-projects\Xiaoqi-Tonight\2-主题blue.cmd`
  - 主题 `violet` 验收：`E:\dsh-pet-projects\Xiaoqi-Tonight\3-主题violet.cmd`
- **测完发回日志**：`C:\Users\10943\AppData\LocalLow\Xiaoqi\Xiaoqi\Player.log`

---

## 本轮重点验收清单（状态：**待晚上主人验收**）

1. **右键菜单不再错位（#18）**：桌宠态右键打开菜单，依次点「今天 / 让她… / 安静一会 / 设置」看行点击是否准确对应、不再点「让她…」误弹今日日程；近景态右键菜单只在 `-xq-menu-test` 启用，正常下右栏/左栏操作。
2. **原生工作台进入与版式**：点近景右下「工作台」钮（#20）或桌宠菜单「让她…→工作台」（或 `Ctrl+Alt+3`）进工作台：
   - 窗口是否铺满显示器；
   - 面板是否出现且完整（纯白超大圆角 4:3 板 + 左侧悬浮药丸栏 + 今日页各行无溢出）；
   - 她是否站在屏幕左侧且没被面板盖住（镜头里大小位置对不对）；
   - 薄雾玻璃开关、左栏动效是否顺、透明处能否穿透、拖动手感是否正常。
3. **退出切换**：工作台点右上 `×` 或按 `Esc` 回近景，再按 `Esc`（或点右下「桌宠」）回桌宠，检查动作流畅且日志无 `Exception`。
4. **离线兜底**：本机未开本地后端（或断网/改连不上 server）时打开工作台：面板不白屏，右下出现「本地服务没开 · 显示的是上次的内容」提示条，点「重试」出现日志 `XQ_BENCH retry`。
5. **其余复测**：设置页里外点击、`-xq-theme blue` 主题色、坐窗睡不掉下窗、副屏 175% 大小。
