# 小祈晚上物理桌面验收清单（基于 `int-all @ 628221fc`）

- **可运行包固定路径**：`E:\dsh-pet-projects\Xiaoqi-Tonight\Xiaoqi\Xiaoqi.exe`
- **一键启动脚本**：
  - 主验收（默认 `pink`）：双击 `E:\dsh-pet-projects\Xiaoqi-Tonight\1-启动验收(pink).cmd`
  - 第 5 项换色对比：先退出后再分别双击 `2-主题blue.cmd`、`3-主题violet.cmd`
- **测完发回日志**：`C:\Users\10943\AppData\LocalLow\Xiaoqi\Xiaoqi\Player.log`（以及如有需要 `C:\Users\10943\AppData\LocalLow\Xiaoqi\Xiaoqi\brain.log`）

---

## 主人按顺序点这六项（每项只看 10~20 秒）

1. **设置页**：主菜单点「设置」/托盘打开、点面板内外，并切到近景点竖栏 `⚙` 打开再点空白关掉。
   - 期望现象：面板居中完整停住不秒关；点页内不关、点页外关；近景点 `⚙` 后关掉仍在近景且不弹出「今日日程」卡片。
   - 看哪行日志：`XQ_CLOSE settings open`（仅 1 次）、`XQ_SET2 open` 与 `XQ_SET2 close`（不是同一时刻紧挨着秒关）、无多余 `XQ_TODAY open`。
2. **拖她**：把小祈依次拖到屏幕最上沿、左右边缘松手，再跨两块显示器拖一次。
   - 期望现象：松手不闪回下方中间、能停在边缘并自然跨屏。
   - 看哪行日志：拖动期间无 `XQ_HOST fix window` / `XQ_HOST center`。
3. **菜单抖**：右键打开菜单让她在原地待机 10 秒，再滚轮放大和缩小各开一次菜单。
   - 期望现象：待机 10 秒期间菜单与缩放稳住不抽搐抖动；人物变大或变小时菜单仍跟在身边不飞偏。
   - 看哪行日志：`XQ_MENU2 place` 中 `scale=` 不乱跳，仅位移时才出现 `XQ_MENU2 anchor-moved` / 重排。
4. **坐窗睡**：把小祈拖到任意普通窗口顶边坐下，右键开菜单并点「睡一会」，之后再叫醒并试一次拖醒。
   - 期望现象：能稳稳坐上窗顶边，菜单开在她身边；点「睡一会」先下窗再进入睡眠；叫醒和拖动唤醒都能正常退出睡眠。
   - 看哪行日志：`XQ_MENU2 open` / `XQ_MENU2 click` 正常命中（非 `blank-click` 或 `picked=null`），无异常报错。
5. **主题**：分别用 `2-主题blue.cmd` 和 `3-主题violet.cmd` 启动，看气泡/方钮/今天卡/菜单/设置页配色。
   - 期望现象：两套主题色调统一清晰、文字可读、背景与边框无明显难看或发白裁切问题。
   - 看哪行日志：`XQ_UI2 theme=blue class=xq-theme-blue` 与 `XQ_UI2 theme=violet class=xq-theme-violet`。
6. **坐窗口后退出进程**：让她坐到某窗口上保持一会儿，再从托盘点「退出」正常关闭进程。
   - 期望现象：顺利退出不卡死、不弹崩溃框。
   - 看哪行日志：`Player.log` 末尾无 `SetOtherQuadsActive` 的 `NullReferenceException` 堆栈。
