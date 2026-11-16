# P1-4a 意图映射草案（B2）

> 作者：WB　日期：2026-11-16　状态：**主人已确认（11-16 13:05）**，B7 代码已写 `[未编]`。第 6 节 5 个答案：留着 / 不要 / **困了自己睡**（与我的建议相反，按主人的）/ 接受 / 要
> 依据：主库 `relay/stage.py`（`_where` / `_pose`）、`state/cards/*.v4.json`（17 份真实日卡）、ME 动作清单（`95d973d5:app/desk/web/anim/index.tsv`，125 条）、ME 状态机参数表、本仓库 `PetPresence.cs` / `XiaoqiFace.cs` / `MeHost.cs`。
> **我没有看过任何一条 ME 动画的画面**，下面「候选剪辑」是按名字和参数推的，置信度已标。第 5 节说怎么低成本补上这一步。

---

## 1. 结论（一屏）

1. 服务器输出的取值域是**封闭的**：`where` 3 个、`pose` 9 个、`spot` 5 个、`prop` 4 个。不用猜，下面第 2 节是 17 份真实卡片跑出来的实际分布。
2. **P1 是桌宠，没有房间**，所以 `spot` / `prop` 在 P1 全部不用（G52 7.7 里「prop 本期不用」同理）。P1 只吃：`where`、`pose`、`phone.typing`、`body.sleepy`、`face`。
3. **已经接了的，B7 不用再做**：
   - `face.top / level / rose` → `XiaoqiFace`（表情，含「情绪变强」尖峰）。
   - `body.sleepy ≥ sleepy_hi` → `XiaoqiFace`（眼皮 + 打哈欠 `XQ_YAWN`）。
   - `where=dorm && pose=sleep` → `PetPresence`（睡姿，走 P1-2b 的 `xqHold`），`where≠dorm` → 头顶标签「在本部上课 · 约 17:30 回来」。
4. **B7 真正要新做的只有一件事**：`pose`（dorm 时）和 `where`（外出时）影响**待机池和动作档位**。
5. 9 个 pose 里，ME 有素材的只有：`sleep`（有）、`sit_idle`（坐窗时有）、`stand_window`/`stand_tidy`（只能当普通站着）。`lie_phone`、`sit_write`、`sit_eat` **没有对应动画**（`isEating` 参数存在，但无代码驱动，也未确认 clip）。这三个在 P1 如实降级，不硬凑。
6. 服务器侧发现 2 处小问题（第 4 节），是 DSH 的活，不挡 P1。

---

## 2. 输入：17 份日卡、393 段的实际分布

用主库 `relay/stage.py` 的 `_where()` / `_pose()` 对真实卡片跑出来（不是手填）：

| 次数 | where | spot | pose | prop | 常见 `seg.name` |
|---:|---|---|---|---|---|
| 105 | out | door | walk_out | — | 下课了收拾东西、在图书馆多坐了一会、午饭：学校餐厅 |
| 78 | dorm | desk | sit_idle | phone | 在宿舍待着、在宿舍歇着、回同学消息对作业 |
| 74 | transit | door | walk_in | — | 回宿舍的路上、走去本部、走去西宝城 |
| 33 | dorm | wardrobe | stand_tidy | — | 洗澡、晚上洗漱 |
| 31 | dorm | bed | lie_phone | phone | 睡前刷手机、醒了赖在床上刷手机 |
| 21 | dorm | bed | sleep | — | 睡了、又睡了、睡前刷手机（仅 1 段，见第 4 节）|
| 17 | dorm | bed | sit_idle | — | 起来洗漱 |
| 12 | dorm | desk | sit_write | pen | 写 MATH1013 习题、CC 读文献、宿舍自习 |
| 11 | dorm | desk | sit_eat | bowl | 午饭：买回宿舍吃、晚饭：宿舍叫外卖 |
| 10 | transit | door | walk_out | — | 走去本部、走去学校餐厅 |
| 1 | dorm | desk | sit_idle | — | COMP1110 小组会 |

`seg.act` 实际出现过的值：空闲 114、走路 73、吃 36、家务 33、睡觉 21、起床 17、自习 16、躺着 16、填充 15、晚上 15、上课 13、港铁 8、兼职 4、空串 4、周末 3、走路 / 小巴 3、小组会 1、分 1。

**含义：** 她一天里约 **47% 的段（out + transit）人不在宿舍**；在宿舍的约 53% 里，最多的是 `sit_idle`（坐着发呆/回消息）。所以外出时桌宠怎么表现，比在宿舍的 9 个 pose 重要得多。

---

## 3. ME 能做什么（只列有驱动或有参数的）

Animator 参数（来自 `AvatarAnimatorControllerV2_1.controller`，32 个，和代码核对过谁在写）：

| 参数 | 含义 | 谁在驱动 | P1 能不能用 |
|---|---|---|---|
| `IdleIndex`(float) + `isIdle` | 待机混合树 | ME 自己；`IdleDirector`（B3）接管 | 能，B3 做 |
| `IsSleeping` | 睡觉（`PET_SLEEPING`，35 s） | `AvatarSleepController`；我们用 `xqHold` 顶住 | 能，已做 |
| `isWindowSit` + `WindowSitIndex` | 坐在窗口上 | ME；`WindowSitEnabled` | 能，要用户开、要有窗口 |
| `isSitting` / `isTaskbarSit` | 坐地/坐任务栏 | ME，条件要看 `AvatarAnimatorController` | **待 B3 查**状态与剪辑 |
| `isDancing` + `DanceIndex` | 跳舞 | ME；我们用 `xqHoldDance` | 能，已做 |
| `isDragging` | 被拖 | ME | 自动 |
| `Headpat` / `HairStroke` | 摸头/摸头发 | ME 的摸头模块 | 已被 `PetPat` 另走 |
| `isTalking` | 说话口型 | ME | 不用 |
| `WalkLeft` / `WalkRight` | 在桌面上左右走 | `AvatarLocomotionController`（P1-2c 已从菜单撤掉） | **不用**（会和「外出」混淆，主人 11-15 已定撤） |
| `HideLeft` / `HideRight` | 躲到屏幕边（`PET_HIDE`，14 s） | ME（原本给全屏躲避用） | 候选，见 6-1 |
| `isEating` / `isDrinking` | 吃/喝 | **没有任何代码在写它们** | 要先确认当前场景用的控制器里状态有没有非空 clip |

动作剪辑里**没有**「写字/打字」「躺着玩手机」「洗漱」（依据：125 条完整清单 `index.tsv`；注意 `XQ_CLIPS` 日志只打印名字含 dance/idle 的，不是完整清单）。清单里有 `sit_smoothie_01`（喝饮料）和 `kawaiimacaronmotion01/02`（吃马卡龙一类），可能是 `isDrinking` / `isEating` 的素材，待动画拼图确认。G52 房间方案里用过的 `PET_SIT_01/02`、`WINDOW_LAY_10` 在工程里有 `.anim`，但那是房间用的独立控制器，P1 桌宠用的是 ME 自带的那套，不直接通用。

---

## 4. 服务器侧发现（给 DSH，不挡 P1）

1. **「睡前刷手机」有 1 段（共 17 段）`act=睡觉`**，`_pose()` 先判 `act=="睡觉"`，所以那一段会落到 `sleep`，Unity 让她睡着，而文字写的是「在刷手机」。其余 16 段 `act=躺着`，正常落 `lie_phone`。数据量小（1/393），但会让「睡着了却写着刷手机」。建议：`_pose()` 里先判 `name` 含「刷手机/玩手机」→ `lie_phone`，再判 `act=="睡觉"`。（实测：`醒了，赖在床上刷手机` 15 段 `act=空闲`，靠名字里的「赖」命中，没问题。）
2. `prop`：代码返回 `bowl`，G52 的表写的是 `food`。P1 不用 prop，P2 写房间时以代码为准。

---

## 5. 映射表

### 5.1 第一层：`where`

| `where` | 次数占比 | P1 她怎么表现 | 依据 |
|---|---:|---|---|
| `dorm` | 53% | 在场，按 5.2 的 pose 档位走 | — |
| `transit` / `out` | 47% | **在场但安静**：待机池只用「calm」档，不主动跳舞/打哈欠；头顶标签显示去哪、几点回来（`PetPresence` 已做）| 桌宠要陪伴，不凭空消失；消失/躲边是产品决定，见 6-1 |

### 5.2 第二层：`pose`（仅 `where=dorm`）

「置信度」= 我对这条映射**能在画面上成立**的把握。「候选剪辑」是按名字推的，未看画面。

| `pose` | 次数 | P1 动作 | 候选剪辑 / 参数 | 置信度 | 为什么 |
|---|---:|---|---|:--:|---|
| `sleep` | 21 | 睡姿 | `IsSleeping`（`PET_SLEEPING`） | 高 | P1-2b 已做，`PetPresence` 已接 |
| `sit_idle`（desk）| 78 | 待机池 `normal` 档 | `PET_IDLE_*` 全池 | 中 | 坐着发呆 ≈ 普通站着待机；P1 不做坐椅子 |
| `sit_idle`（bed，起来洗漱）| 17 | 待机池 `normal` | 同上 | 中 | 同上 |
| `sit_write` | 12 | 待机池 `calm` | `HUS_IDLE*` 里幅度小的 | 低 | **缺素材**：没有写字/打字剪辑。降级为安静站着，头顶标签显示 `seg.name`（如「写 MATH1013 习题」）|
| `sit_eat` | 11 | 待机池 `calm`；若 `isEating` 能用则切 | `isEating`（待确认）| 低 | 参数有，驱动没有，clip 未确认 |
| `lie_phone` | 31 | 待机池 `calm` + 表情偏「困/平常」 | — | 低 | **缺素材**：没有躺姿手机。`WINDOW_LAY_*` 是躺在窗沿，要先有窗口，不适用 |
| `stand_tidy`（洗澡/洗漱）| 33 | 待机池 `normal`（6-2 定：不躲边）| 不用 `HideLeft/Right` | 低 | 没有洗漱剪辑；6-2 主人定「不要躲边」|
| `stand_window` | — | 待机池 `calm` + 暂时不看鼠标 | `AvatarMouseTracking` 的 `headBlend` 调低 | 中 | 「望窗外」 = 不盯着主人；这次数据里 0 次，规则在，留着 |

### 5.3 第三层：修饰（不换 pose，叠在上面）

| 信号 | P1 做什么 | 已接？ |
|---|---|:--:|
| `phone.typing = true` | 头顶小气泡「正在打字…」（走 `PetBubble`），**不做手上动作** | 否，B7 新做 |
| `face.top / level / rose` | 表情 | **是**（`XiaoqiFace`）|
| `body.sleepy ≥ sleepy_hi` | 眼皮 + 打哈欠 | **是**（`XiaoqiFace`）|
| `body.sleepy` 高 且 `where=dorm` 且非睡姿 | 待机池权重偏向 `calm` | 否，B7 新做 |
| `seg.name` | 头顶标签（外出时） | **是**（`PetPresence`）；dorm 时的 `sit_write/sit_eat/lie_phone` 标签是 B7 新做 |
| `body.stamina / social`、`event`、`next`（dorm 时）| P1 不用 | — |

### 5.4 待机池分档（`calm` / `normal`）

ME 有 10 条待机（`PET_IDLE`、`PET_IDLE_2…`、`HUS_IDLE01…09` 等）。**我不知道哪条动作幅度大，不凭名字分档。** 要看一眼才能分——见第 7 节「拼图」。拼图出来之前，`calm` 档 = `normal` 档 = 全池，B3 的 `IdleDirector` 先按「全池 + 最近 3 条不重复」做，分档只是个列表常量，之后改一行。

---

## 6. 要主人定的 5 件事

| # | 问题 | 我的建议 | 不选建议的后果 | **主人答复** |
|---|---|---|---|---|
| 6-1 | 外出（47% 的时间）桌宠要**消失/躲到边上**，还是**留在桌面安静待着**？ | 留着，安静待着，标签写去哪 | 选躲边：用 `HideLeft/Right`，她一天近一半时间看不见；选消失：要新写隐藏逻辑，且主人找她时可能以为坏了 | 留着 |
| 6-2 | `stand_tidy`（洗澡/洗漱，33 段）要不要用「躲到边上」？ | 不用，用普通待机 | 用的话要限制频率，否则每段洗漱都躲一次 14 秒动画 | 不要 |
| 6-3 | 困意高但没到睡觉时，她要不要**自己睡着**？ | 不要，只打哈欠、动作偏安静 | 要的话主人在工作时她会突然睡倒，要给「叫醒」入口，范围变大 | **困了自己睡**（≠建议） |
| 6-4 | `lie_phone / sit_write / sit_eat` 缺素材，P1 接受「降级成安静待机 + 头顶写在干嘛」吗？ | 接受，素材 P2 再补（房间里有 `WINDOW_LAY_10`、`PET_SIT_01/02`） | 不接受：要先找/做 3 条动画，P1 验收线后移 | 接受 |
| 6-5 | `phone.typing` 时头顶出「正在打字…」小气泡，要不要？ | 要 | 不要：主人看不出她在回消息，和 G52 房间里 `BedPhone/DeskType` 的作用丢了 | 要 |

**主人答复（11-16 13:05）：留着 / 不要 / 困了自己睡 / 接受 / 要。** B7 按此执行，见第 8 节。

**6-3 的落地口径**（我补的约束，防止「工作时突然睡倒」；数值都在 `emotion_line.json` 的 `body` 里可调）：
- 总开关仍是 `emotion_line`（默认关）；`body.self_sleep` 默认开。
- 条件同时满足才睡：`where=dorm` 且非睡姿、`body.sleepy ≥ sleepy_hi(70)` **持续 90 秒**、服务器数据 30 秒内是新的、没被碰过 20 秒（拖/按/鼠标停在她身上）、没在拖/跳舞/坐窗/菜单打开/被全屏躲开。
- 一次最多睡 15 分钟（`self_sleep_min`），醒后 20 分钟内不再自己睡；困意回落到 `sleepy_hi-30` 以下就醒。
- 叫醒入口已有：拖她、菜单「叫醒她」。

---

## 7. 怎么低成本补上「没看过画面」

让 Grok（在 INORI02 上）做一件小事，**不用改代码**：在 `-batchmode` 编译完的包里，用 Animator 参数轮流把 10 条待机各放 3 秒，每条截 3 张图（0 s / 1.5 s / 3 s），拼成一张图 + 一个 `clips.csv`（剪辑名、时长）。我看图给 `calm / normal` 分档，主人看图确认。这步依赖 B1 的人工验收先有人在屏幕前（截图不能是黑的）。

---

## 8. B7 实际做了什么（`[未编]`）

1. 新 `Pet/XqStageBody.cs`：订阅 `XiaoqiLink.OnStage`，算档位（`TierFor`）→ `BodyDirector.SetServerTier`；`DormLabel()` 给头顶标签；`phone.typing` 假→真时 `PetBubble.Enqueue("正在打字…")`（60 秒内不重复、睡着/安静/外出不出）；自己睡着逻辑（6-3，见第 6 节口径）。日志：stdout `XQ_STAGE …`；`brain.log` 里 `src=server` 的 `evt=stage / typing / self_sleep`。
2. `BodyDirector` 的 server 层 `SetServerTier` 本来就有，只改档位权重，不直接播剪辑。**档位目前没有实际效果**：`IdleDirector.CalmSlots` 还是 null（等动画拼图分档），所以现在只是把意图和档位写进日志、接通管线，分档生效要等拼图。
3. `PetPresence`：dorm 时标签用 `XqStageBody.DormLabel`；**顺手修了一个会在真服务器上暴露的问题**：`OnStage` 每 `poll_sec` 秒触发一次，原来每次都把 `IsAsleep` 改成服务器值——菜单「睡一会」、拖醒后的 30 秒宽限、自己睡着，都会在 ≤2 秒内被冲掉（假数据只推一次所以之前没暴露）。现在：本地小睡未到点保持睡着；刚被叫醒的 30 秒宽限内不再被服务器睡姿立刻压回；`SetSleeping` 只在值变化时调。
4. `EmotionLineConfig.BodyCfg` 新增 5 项（`self_sleep`、`self_sleep_hold_sec`、`self_sleep_min`、`self_sleep_cooldown_min`、`self_sleep_idle_sec`）；`XiaoqiBuild` 预制体加 `XqStageBody`；新假数据 `fake_stage/pet_typing.json`（lie_phone + typing=true，段名「回邮件」）。
5. 验收（`-fake-stage` 一次性推送，不是 poll）：
   - `-emotion-line`（或 cfg `emotion_line=true`）+ `-fake-stage sit_write`：头顶标签 = 段名，`brain.log` 有 `src=server evt=stage tier=calm`。
   - `-fake-stage pet_typing`：出一次「正在打字…」气泡。
   - `-fake-stage pet_sleepy -xq-brain-fast`：鼠标移开 ≥3 秒，约 8–10 秒内睡着（`evt=self_sleep start`），约 25 秒后醒（`wake`）；拖她立刻醒。
   - 真服务器：改日卡 pose，≤`poll_sec`(2 秒)+3 秒换档位/标签。
6. **不做**：`spot`/`prop`、`WalkLeft/Right`、`isEating`、躲边、`stand_window` 时调低 `headBlend`（本批数据里 0 次，且会和设置页的「看向的力度」抢同一个值）。
