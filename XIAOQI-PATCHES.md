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
