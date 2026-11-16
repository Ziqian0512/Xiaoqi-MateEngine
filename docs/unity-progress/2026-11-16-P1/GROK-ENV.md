# GROK-ENV · Grok 环境自检（G61-A §2）

> 2026-11-16 11:55（UTC+8）· Grok 写 · 只做了 §2，B1 没开始
> 测的是 Grok 自己的虚拟桌面，不是主人的 Inori02。

**结论：都不能。**虚拟桌面是 Linux，不能装 Windows 版 Unity 6000.2.6f2，所以编不了 Windows 包，也跑不了 `Xiaoqi.exe`。按 §2 的规定，第 3 项失败后就停了。4–8 项没有测，不做替代方案。

| # | 问题 | 回答 |
|---|---|---|
| 1 | 系统 / 版本 / 分辨率 | **不是 Windows**。Debian GNU/Linux 13（trixie），x86_64，8 核，内存 15 GB（自检时只有约 3 GB 空闲），磁盘 122 GB 空闲。单屏 1920×1200，100 DPI |
| 2 | 能克隆 fork 吗 | **能**。`git ls-remote` 读到 `xiaoqi/p1` = `3a6f597b`；今天 11:09 从 codeload 下载 `a73dd06` 的源码 zip（570 MB）用了 48 秒。完整 `git clone`（约 2 GB）没有实际跑 |
| 3 | 能装 Unity 6000.2.6f2 + Windows Build Support 吗 | **不能**。官方的 `UnitySetup64-6000.2.6f2.exe` 能访问（HTTP 200，4.05 GB，没被转到国内镜像），但那是 Windows 安装器，这台 Linux 上装不了。同版本的 Linux 编辑器存在（`Unity-6000.2.6f2.tar.xz`，4.41 GB），但对应的 Linux 版 Windows Mono 构建支持包 `LinuxEditorTargetInstaller/UnitySetup-Windows-Mono-Support-for-Editor-6000.2.6f2.tar.xz` 返回 404。因此没有继续尝试，也不找替代版本 |
| 4 | 能激活个人版许可证吗 | 没测（第 3 项失败）。如果以后要激活，需要主人在桌面上自己登录，不在聊天里发密码 |
| 5 | 能命令行编译吗 | 没测（第 3 项失败） |
| 6 | 能运行 `Xiaoqi.exe` 并看到窗口吗 | **不能**。Linux 不能直接运行 Windows exe，机器上也没有装 Wine |
| 7 | 窗口透明吗 | 没测（第 6 项不能） |
| 8 | 点透明处，记事本能收到点击吗 | 没测（第 6 项不能；这台机器也没有 Windows 记事本） |
| 9 | 能把系统缩放调到 175% 吗 | **不能**（没有 Windows 的显示缩放设置） |
| 10 | 能截屏 / 录屏吗 | 截屏**能**。机器上有 `ffmpeg`，可以录 mp4，但这次没有实际录一段。往主人电脑传文件，单个最大 100 MiB |
| 11 | 第二块显示器 | **没有**（只有一个 1920×1200 屏） |

## 给 WB 的事实备注（只是事实，不替你决定）

- 主人自己的 Windows 机器 Inori02 上已经装了官方的 Unity 6000.2.6f2，`XiaoqiBuild.Win64` 也成功跑过（11-16 03:55，日志 `Build\p1-3-build.log`，结果 “Build Successful”），主显示器是 1920×1080 / 100%。Grok 能远程在那台机器上执行命令，但每一步都需要主人批准。要不要用那台机器、怎么用，由 WB 决定。
- 11-16 03:55 那个包是从 `ad51f40`（P1-3c）+ 本机未提交的多屏修复编出来的。这个修复后来作为 `a73dd06` 提交并推送了。
