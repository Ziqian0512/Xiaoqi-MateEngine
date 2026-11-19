# Xiaoqi-MateEngine (`xiaoqi/p2-agy4`) 多 Agent 协作状态板

> 最后更新：2026-11-19
> 仓库：`https://github.com/Ziqian0512/Xiaoqi-MateEngine.git`
> 协作主开发分支：**`xiaoqi/p2-agy4`**（对应本地 `E:\dsh-pet-projects\Xiaoqi-ME-p2`）

---

## 1. 已完成（Current State，2026-11-19 获批 V4 落地）
- 三形态沿用成熟的模式切换、现有 3D 模型与表情链路；新增统一的柔白紫烘焙材质 `XqMoonSurface`。桌宠气泡/胶囊同步材质，近景与工作台采用获批布局。没有启用原生桌面背景模糊。
- **近景中间镂空**：全屏 host 无背景，不覆盖桌面；保留时钟、真实日期/地点/天气、偏移便签和无框活动线。移除旧假日程进度、固定天气与满屏频谱柱。音乐上移并压缩高度，保留上一首/播放暂停/下一首；无封面为中性音符，未提供时间线时显示 `--:--`。
- 近景右侧收口摸头、听我说、看我屏幕、安静陪伴；沿用现有短眨眼/表情恢复，没有扩展僵硬的全身动作池。添加少量现有蝴蝶结/云朵贴纸和绘制式猫耳。底部低幅折线仅取 Unity 自身实际音频（当前 TTS），无声时平直。
- **工作台当前主线**：后续由 `28363b6` 更新为此刻/手账/记忆三页的二维图形拼贴，底部设置入口。本轮增加独立 `-xq-campus-preview`「窗边大学生活」此刻页样片，默认工作台保留；详情见第 7 节与 CAMPUS-PREVIEW.md。
- **交互**：工作台按下后在同一目标松开且位移不超过 8px 才执行；拖动不切页。照片预览期间阻止底层点击，Esc 优先关预览。近景新侧栏使用既有几何命中链路；输入框补提示，静音设置修复重复翻转。
- 真实聊天与 IndexTTS、近景必播/桌宠概率播报，以及网易云 SMTC 链路沿用第 4/5 节结果。播放器未返回的能力不伪造。

## 2. 待推进 / 共同任务（Shared Backlog）
1. 用户实机观感验收：3D 人物与面板的桌面合成、浅色/深色/复杂背景可读性、多屏/DPI、透明窗口点击与听感。本轮已完成 Unity 面板实际渲染检查；窗口工具未能枚举透明窗口，不能把面板渲染图称为完整桌面截图。
2. STT 语音识别仍未接通；录音入口保留真实失败提示，不能视为可用的语音输入。
3. 当前网易云 SMTC 未返回时长，封面流尚未读通；完整歌单由网易云管理。音乐页提供打开网易云入口，后续再评估可用的歌单接口。
4. 原生背景模糊保持实验状态。继续使用可实现的烘焙材质基线；不承诺复制网页 backdrop-filter 效果。
5. 如需单眼闭合或新动作，先验证模型通道和已有素材，再逐个加入反馈；不急于铺全量动作池。Brain / 控制台联调仍在关联 Amamiya-Inori 仓库。

---
## 3. 已知硬坑（踩坑记录，接手必看）
- **禁止在圆角 `VisualElement` 上设 `overflow = Overflow.Hidden`**：Unity 6 UI Toolkit 在透明桌面窗口下会触发圆角裁剪遮罩 Bug，导致整个卡片变成实心纯白块。
- **禁止缩小 `MainCamera.rect`**：会导致透明窗口底部区域每帧不被清屏，产生叠纸牌残影拖尾。
- **禁止用 `git checkout --` 清理 Unity 自动改写的资产**：`addressables_content_state.bin`、`ProjectSettings.asset`、`lilToonSetting.json`、`EditorUserSettings.asset` 勿强行还原，提交代码时只 `git add Assets/Xiaoqi/` 与文档即可。

## 4. 2026-11-19 链路真实性与近景反馈第一轮
- 聊天改接现有 `POST /v1/chat/completions`（非流式响应，客户端逐字呈现）；删除离线预设回复。发送新消息先清旧回复，忙碌时保留输入，防止并发轮次错位。
- TTS 修正为 `POST /api/voice/preview` 后下载返回的 WAV；内网音频下载绕过系统代理。已实测本机 `127.0.0.1:8008/health` 在线（IndexTTS-2.5），云端语音合成返回真实 `/voice/preview-*.wav`。
- 聊天端到端请求到达云端，但模型上游返回 **HTTP 402 Payment Required**（已只读核对 dspet-relay 日志）；需检查上游账户额度，尚未验收真实聊天成功。未修改或重启云端服务。
- STT 尚无已确认可用的服务；删除无麦克风/识别失败后自动编造问题发送的行为，明确提示失败。语音识别仍待联调，不能视为已接通。
- 音乐尚未接入真实播放源：移除内置模拟歌单与推进，禁用近景播放按钮、显示未接入；频谱仅使用本地实际音频输出（当前 TTS），外部播放器标题只作窗口元数据，不假装测得系统频谱。
- “歌单音乐”导航改称“陪伴控制”，与现有 Brain 控制内容对应；相册与文字记忆拆分仍待办。
- 近景进入、摸头、表情按钮复用现有表情控制器，增加短眨眼、渐入/停留/恢复和 1.6 秒防重复触发；原“比心”仅改变表情，改名“开心”。没有新增全身动作或单眼通道。
- 尚需用户实际观感验收：表情幅度、眨眼速度、透明窗口命中、近景完整体验。单眼闭合虽有底层通道，未验证模型支持，本轮不启用。
- 验证：Unity 6000.2.6f2 / XiaoqiBuild.Win64 最终构建成功并退出 0；robocopy 同步到 Xiaoqi-Tonight，退出码 3（成功），保留 xiaoqi_config.json。云端生成音频下载 HTTP 200，audio/wav，88108 字节，RIFF/WAVE 文件头通过。尚未完成窗口内听感与交互观感验收。

### 充值后链路复验（2026-11-19）
- 用户已充值上游模型，重新测试真实 /v1/chat/completions，返回非空回复：收到，我在这，听得见。此前 HTTP 402 阻塞本次未复现。
- 将实际回复提交 /api/voice/preview，IndexTTS 合成成功；下载音频 HTTP 200，191532 字节，RIFF/WAVE 头有效。聊天→语音合成→音频下载验证通过。
- 这是接口链路验证，未替代桌宠内实际听感与交互验收；麦克风 STT 仍待接通。仅更新文档，无代码或构建变更。

## 5. 2026-11-19 按形态播报 + Windows 音乐会话（模板另行评审）
- 近景直接回复自动播报；桌宠直接回复默认 35%，偏好设置可轮换 0/35/65/100%。工作台默认不自动播报。自动语音总开关、静音、勿扰、办公隐藏私聊优先；播放中切静音/勿扰会停止声音。
- 新一轮输入可中断上一轮逐字显示和播报，不排队播放旧回应。近景字幕默认保留，偏好设置可以关闭；错误、录音和等待状态仍保留可见文字。
- 新收到的私聊 feed 也按形态处理：初始历史不补播，直接聊天回复去重，桌宠主动内容再降低概率并加 60 秒冷却。
- 新增 XqMediaSession.txt（ASCII Windows PowerShell 5.1 WinRT helper）。Unity 持有一个隐藏子进程，经 stdin/stdout 读系统当前会话和发送控制，退出时清理自己创建的进程；不使用模拟歌单或全局媒体键。
- 网易云 PC 3.1.43 起初未开启 SMTC；已在其“设置→系统→开启SMTC”勾选。实测能读曲名、歌手、播放状态，支持暂停/恢复；测试中暂停后 playing=false、恢复后 playing=true。未改歌单。
- 当前网易云会话没有返回时间线时长（duration=0），封面流在 PowerShell 接口下尚未成功读出。对应时间显示 --:--、封面留空；没有补假进度。上一首/下一首按实际可用能力启用，未通过切歌打断用户当前歌曲。
- 音乐收藏不属于系统媒体能力，继续禁用。系统频谱仍只取 Unity 自身实际声音，不将媒体会话元数据当作音频采样。
- 已运行 11 项语音策略断言（近景、桌宠概率阈值及 0/100%、静音、勿扰、办公与总开关）；编译日志 build-media-final.log。
- 三形态面板仅制作评审模板：E:\dsh-pet-projects\design-previews\2026-11-19\index.html；可切换形态、深浅桌面、字幕及相册预览。图片与文字均为构图示例，近景/桌宠实施仍保留现有 3D 模型。用户尚未批准面板重排，不将模板样式写入 Unity。
- 最终构建成功（Unity 退出 0），11 项策略检查通过；已同步到 Xiaoqi-Tonight（robocopy 成功），保留配置文件。模板已用独立浏览器检查三态切换、相册大图、字幕及摸头示例，无脚本异常，760px 视口无横向溢出。桌宠内实际听感与操作仍待用户验收。

## 6. 2026-11-19 获批模板实施与交付记录
- 用户批准 V4 开工；当前状态以第 1/2 节为准。第 4/5 节是过程历史，其中“音乐未接入”“模板未获批”等描述已被后续结果替代。
- 实施集中在 `XqApprovedPresentation.cs` 和既有面板接线；保留 MainCamera 全屏清屏，没有新增圆角 Overflow.Hidden。`XqApprovedReview` 仅在显式传入 `-xq-approved-review <目录>` 时启动，不发送聊天，不在正常运行中抓图。
- 实际渲染检查发现并修复：旧高光层改变子元素索引导致活动文字被隐藏、假进度残留；卡片旧高光/投影残留；封面空白；静音设置二次翻转。装饰忽略命中；近景聊天和模式入口不再随视差漂移。
- 最终编译：`XqVoicePolicyChecks.VerifyAndBuild` 内运行 11 项语音策略断言，再调用 `XiaoqiBuild.Win64`，Unity 6000.2.6f2 构建成功，退出 0。日志 `build-approved-delivery.log`。
- 成品通过 robocopy 同步到 `E:\dsh-pet-projects\Xiaoqi-Tonight\Xiaoqi`，退出码 3；配置文件保留，Assembly-CSharp.dll 源/目标 SHA256 相同。Unity 自动改写资产不混入本轮提交。
- 渲染产物在 `E:\dsh-pet-projects\design-previews\2026-11-19\unity-delivery-review`。图像是运行中 UI Toolkit 面板输出，透明处在查看器中可能呈黑色；不含相机渲染的 3D 人物，不代表 3D 被移除。完整桌面合成与操作手感仍需用户验收。
- 最终七张面板输出均非空；近景中央 1000×500 像素区域 alpha 均为 0（覆盖像素数 0），运行日志完成三态往返与五个工作台页面检查，无新增面板异常。报告 render-checks.json。

## 7. 2026-11-19 近景 V20 视觉重塑与桌宠修复交付记录
- **3D 与音频**：`XqCloseCamera.cs` 修复工作台返回桌宠/近景时 3D 本体丢失（保持 `cam.rect = (0,0,1,1)` 与 `cullingMask` 不被覆盖）；`XqSysAudioBridge.cs` 接入 Windows CoreAudio `IAudioMeterInformation` 系统级实时音量峰值与 `XqMusicBridge.IsPlaying` 兜底，驱动底部 160-bar 粉色声浪。
- **工作台左侧栏**：`BuildApprovedRail()` 重塑为 64px 极简圆角胶囊侧栏，`设置` 按钮独立固定于最底部。
- **近景 V20 布局与二次元自定义贴纸**：
  - 左上时钟区仅保留左上角单枚蝴蝶结与高对比深紫日期/地点/天气块。
  - 右上角重构为 Q 版探头竖卡（`XqChibiPeek.png`）+ 翅膀星星竖向 `TOKEN 80%` 能量柱（`XqStarTop.png`）。
  - 右下角将“正在做的事”胶囊上移至模式切换条正上方对齐。
  - 左下 Spotify 风格音乐卡彻底清除外层紫框（`XqMoonStyle.Clear(_cardMusic)`），底部控制键改为亮白高对比；歌名/歌手限制单行省略防穿模。
  - 4 个互动组件围绕小祈左右各 2 个四散布局，接入 4 套透明二次元贴纸素材：`XqBadgeHeart.png`（摸摸头）、`XqBadgeCassette.png`（听我说）、`XqBadgeWindow.png`（看我屏幕）、`XqBadgeMoon.png`（安静陪伴）。
  - 底部中央堆叠为：`AMAMIYA Inori` 签名（下移放大）→ 粉色声浪条 → 单行黑字右对齐回复 → 细长柔紫微雾输入框。
- **构建与同步**：Unity 6000.2.6f2 `XiaoqiBuild.Win64` 退出码 0（`build_v20.log`）；`robocopy` 同步至 `E:\dsh-pet-projects\Xiaoqi-Tonight\Xiaoqi`（退出码 3）；已实机拉起 `Xiaoqi.exe`。

## 7. 2026-11-19 可复现的「窗边大学生活」此刻页样片
- 用户要求预览必须能在 Unity 成品复现。本轮仅深化此刻页，以显式 `-xq-campus-preview` 开关评审，未替换其他 agent 已提交的默认三页设计。工具入口跳转既有浏览器控制台，展示页不新增重度设置。
- 以小祈港大计算机大一身份制作两张实际运行素材：XqCampusRoom.png（1672x941 宿舍背景），XqCampusInori.png（1024x1536 真透明银白发/猫耳发箍、针织衫、耳机、笔记本立绘）。内置 image_gen 生成后原样复制到 Assets/Xiaoqi/Resources；完整提示词保存在本机 campus-sample/PROMPTS.md。
- 背景、人物、真实 UI 控件为分层二维实现；空间光影来自背景插画，不依赖实时三维房间、桌面模糊或新动作池。预览由实际 Windows player 的同一个 UI Toolkit 面板直接输出，捕获逻辑不注入示例数据、不重排截图、不修图。
- 布局：中央人物、左侧香港时间/真实天气地点/近况/日程、右侧真实留言（无留言回退到已有手账并改标题）/两张可展开插画/真实系统媒体上一首、暂停、下一首。缺失数据有空态，没有填固定天气、模拟歌曲或同步百分比。素材与纸卡共享运行代码。
- 导入检查发现 Unity 默认 NPOT 缩放会改变人物比例；已锁定 NPOT=None、无压缩、关闭 mipmap 和人物透明边缘处理，并逐张断言导入尺寸等于 PNG 原始尺寸。XqCampusPreviewBuild.VerifyAndBuild 随后执行既有 11 项语音策略检查和 XiaoqiBuild.Win64。
- 最终构建 build-campus-release.log 成功退出 0。最终运行检查：1920x1080 视口，舞台裁切恰为 1440x810，左右列不交叠；照片真实展开/关闭、手账→记忆→此刻往返检查通过，无新增页面异常。已有 localization/SteamDRM 警告未在此轮处理。
- 成品已 robocopy 同步到 Xiaoqi-Tonight/Xiaoqi（退出码 3），保留配置。独立启动入口 E:\dsh-pet-projects\Xiaoqi-Tonight\5-campus-preview.cmd。正常显示只需要 preview 开关，不需要 capture 开关。时间、消息和当前媒体会随真实数据变化，布局/素材与截图一致。
- 最终预览与几何报告：E:\dsh-pet-projects\design-previews\2026-11-19\campus-sample\final\campus-now-panel.png / campus-now-full.png / layout.json。复现操作、素材路径及约束见仓库 CAMPUS-PREVIEW.md。下一步由用户评审此刻页，再决定是否扩展其他页面。
- 用户报告旧版桌宠叠出后，只读实测一度存在 G55/Build/Xiaoqi/Xiaoqi.exe（-deme-audit）及 Tonight 新版两个独立进程。本轮脚本未引用 G55，未能确认旧版启动来源；再次复查时均已退出，未自动重启。样片启动入口新增已有 Xiaoqi 进程检查，拒绝从该入口叠开实例。未修改或终止其他历史工程。此补充仅修改启动脚本与文档，Unity 代码及已验收预览保持不变。
