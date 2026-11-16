# Xiaoqi Patches

Upstream: `https://github.com/shinyflvre/Mate-Engine`
Fork: `https://github.com/Ziqian0512/Xiaoqi-MateEngine`
Branch: `xiaoqi/main`

## Rules

- 小祈代码只放 `Assets/Xiaoqi/`。
- 必须改 Mate Engine 原文件 / ProjectSettings / 主场景时，登记在这里。
- 原文件改动上限 10 处；超过就停下来重新设计。
- 私有素材放 `Assets/Xiaoqi/_Private/`，已在 `.gitignore`。
- 构建脚本：`Assets/Xiaoqi/Editor/XiaoqiBuild.cs`。

## Patches

| # | File | Change | Reason |
|---|------|--------|--------|
| 0 | `Assets/Xiaoqi/Editor/XiaoqiBuild.cs` | 新增构建入口 | U0 |
| 1 | `Assets/Xiaoqi/Editor/XiaoqiIdentity.cs` | 关闭 Unity 启动画面 / Shinymoon 启动 logo | E0 |
| 2 | `Assets/MATE ENGINE - System Tray/SystemTray.cs` | 托盘 tooltip 从 iconName 改为「小祈」 | E0；主人 11-14 明确批准这次改 ME 原文件 |
| 3 | `Assets/MATE ENGINE - Scripts/AvatarHandlers/AvatarAnimatorController.cs` | G55：一处；P1-2a：`xqHoldDance`，`CheckForSound` 开头 return，`StartDancing` / `SetDancing` 改 public，加 `XqSetDanceIndex`，拖拽清除 | 菜单跳舞被 2 秒音检打断 |
| 4 | `Assets/MATE ENGINE - Scripts/AvatarHandlers/AvatarSleepController.cs` | P1-2b：`xqHold`、`XqSetSleeping`，`Update` 开头处理 | `enableSleep=false` 时每帧把睡姿写回 |
| 5 | `ProjectSettings/ProjectSettings.asset` | companyName / productName 改为 Xiaoqi，清空 Standalone 图标（G55 已改，B0 补登） | 去 ME 品牌 |
