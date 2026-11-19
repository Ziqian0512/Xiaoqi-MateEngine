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
