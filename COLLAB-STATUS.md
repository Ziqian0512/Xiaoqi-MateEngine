# Xiaoqi-MateEngine (`xiaoqi/p2-agy4`) 多 Agent 协作状态板

> 最后更新：2026-11-19
> 仓库：`https://github.com/Ziqian0512/Xiaoqi-MateEngine.git`
> 协作主开发分支：**`xiaoqi/p2-agy4`**（对应本地 `E:\dsh-pet-projects\Xiaoqi-ME-p2`）

---

## 1. 已完成（Current State）
- **三态模式切换统一**：桌宠（`XqPetHud`）、近景（`XqClosePanel.Controls.cs`）、工作台（`XqBenchPanel.cs`）均采用统一三态胶囊，明确高亮当前所在形态；近景已移除多余 `⚙` 设置弹窗（设置统一收口在工作台「偏好设置」页）。
- **近景视觉（V18 方案 B 预渲染亚克力 + 破框贴纸 + 3D 视差）**：
  - 所有近景卡片统一使用 `XqLiquidGlass.ApplyAcrylicB`（柔白紫渐变 + 3% 微粒 + 顶边镜面高光 + 紧贴投影 `flexGrow=1`）。
  - 音乐卡改用横向 `starry nights` 布局 + `XqMusicCover.png`；左上时钟区配破框蝴蝶结 `XqStickerRibbon.png`，音乐卡右上配破框云朵 `XqStickerCloud.png`。
  - 保留全屏粉色透明音频频谱（`XqSysAudioBridge.cs`）与语音交互（`XqVoiceChatClient.cs`、`XqVoiceOverlay.cs`）。
- **工作台视觉与功能**：
  - 根治底部拖影与黑角：`XqCloseCamera.ApplyBenchViewport` 保持全屏 `Rect(0,0,1,1)` 清屏，工作台下仅将 `cullingMask = 0`；`_panel` 铺设统一实底柔紫底色。
  - 中央更换为定制 2D 二次元半身主图 `XqBenchHero.png`；右侧照片墙接入 3 张 2D 二次元日常照（`XqPhotoDorm.png` / `XqPhotoStudy.png` / `XqPhotoPeace.png`），点击原位弹窗大图预览（不再错误跳转至文字记忆列表）。

---

## 2. 待推进 / 共同任务（Shared Backlog）
1. **工作台「日常相册」页（Page 2）**：目前首页照片墙已支持点击大图预览，但左侧第 3 个导航图标「日常相册」页（`BuildMemPage`）内部仍为旧版文本记忆列表，需拆分为「真实相册画廊」与「记忆归档」或重写相册页。
2. **工作台中央视觉 / 破框层次**：如后续需升级工作台（如参考图类琉璃分层、2D 角色头发/衣角破框穿插、或接入 ComfyUI 本地二次元模型自动刷新日常照），可在本分支继续迭代。
3. **音乐卡真实播放进度与时长联动**：当前近景音乐卡左右时间刻度为静态示例（`0:50 / 3:04`），需接入 `XqMusicBridge` 实时进度或系统媒体信息（SMTC）。
4. **Brain / 控制台联调**：见关联仓库 `Ziqian0512/Amamiya-Inori`（本地 `E:\dsh-pet-projects\dspet-main`）。

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
