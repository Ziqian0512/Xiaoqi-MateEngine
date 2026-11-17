# 小祈晚上物理桌面验收清单（基于 `int-all @ ff82f463` / G74 W6）

- **可运行包固定路径**：`E:\dsh-pet-projects\Xiaoqi-Tonight\Xiaoqi\Xiaoqi.exe`
- **一键启动脚本（前三个均已带 `-xq-bench`，第四个带 `-xq-bench -xq-bench-fake`）**：
  - 主验收（`pink` 默认）：`E:\dsh-pet-projects\Xiaoqi-Tonight\1-启动验收(pink).cmd`
  - 主题 `blue` 验收：`E:\dsh-pet-projects\Xiaoqi-Tonight\2-主题blue.cmd`
  - 主题 `violet` 验收：`E:\dsh-pet-projects\Xiaoqi-Tonight\3-主题violet.cmd`
  - 工作台四页假数据验收：`E:\dsh-pet-projects\Xiaoqi-Tonight\4-假数据.cmd`
- **测完发回日志**：`C:\Users\10943\AppData\LocalLow\Xiaoqi\Xiaoqi\Player.log`

---

## 工作台四页验收（状态：**待晚上主人验收**）

1. **第 0 页（「今天」）回归**：左栏 6 个钮（`home/heart/cal/chat/spark/gear`），`home` 默认高亮；第 0 页与之前一致（半透毛玻璃、胸像、视差、拖板、6 行内容、薄雾玻璃开关）。
2. **四页假数据（运行 `4-假数据.cmd`）**：依次点左栏 1（她）/ 2（生活）/ 3（记忆）/ 4（成长），每页 6 行有内容、文字不折成竖排、不出板/不被裁、无 `□` 缺字；三主题（`pink/blue/violet`）切换一致。
3. **离线条与缓存恢复**：不带 `-xq-bench-fake` 且未连服务器时，进「她」页无缓存第一行显示「连不上服务器，也没有缓存」，其余行收起，右下离线条亮；有缓存后离线重进显示缓存内容与顶部「缓存 HH:mm:ss」，点「重试」可重新请求。
4. **滚轮与六层快照**：记忆页/生活页内鼠标滚轮可上下翻动；六层快照按钮可打开 `/snapshot`。
5. **近景菜单与外观（W5b 回归）**：近景右键 → 设置 → 行为盖在装饰卡之上，菜单关闭后装饰卡与左栏可点；桌宠外观「模型」行不折行。
6. **手感、质感与信息密度**：均待晚上主人实机确认。
