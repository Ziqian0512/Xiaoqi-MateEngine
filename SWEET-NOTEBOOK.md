# 小祈 · 软糖手账（Unity 工作台）

本版替换默认工作台三页。获批依据：软糖手账第三版的此刻、记忆页，以及书页宽高比修正后的大画框手账页。旧 Campus 样片仍可通过 `-xq-campus-preview` 单独打开；旧默认实现只供 `-xq-bench-legacy` 回看。近景和桌宠沿用现有实现。

## 打开

成品入口：`E:\dsh-pet-projects\Xiaoqi-Tonight\6-sweet-notebook.cmd`。
启动器检查已有 Xiaoqi 进程，拒绝叠开第二只；没有假数据、旧 G55 工程或老板项目启动命令。

正常显示使用 `Xiaoqi.exe -settings-v2 -xq-bench -xq-mode bench`，不需要评审或截图开关。工具图标仅在点击时打开既有控制台 `/p/set`。

## 预览与成品使用同一套素材

- 设计坐标为 1440×810。1920×1080 显示器中居中；小屏按同一比例整体缩小，不横向压缩书页。
- 此刻：奶白探身；手账：蓝色针织、侧坐写字；记忆：玫瑰针织、抱相册比心。三页各有独立小头像，点击/悬停切换表情。
- 手账书页为 720×378，人物框为 500×535。每次最多四条主线日程，翻页查看其余内容。正文区域和小头像相互避让。
- 雾蓝网格、纸张阴影、画框、吊坠与主插画合成三张 2880×1620 装饰纹理；这是运行时实际加载的美术资源，没有实时模糊或三维房间依赖。
- 所有时钟、日期、天气、消息、日记、日程、记忆正文、成长内容和媒体状态均由 Unity 原生 Label / VisualElement / ScrollView 绘制。源示例文案被从装饰纹理中移除；文字和点击区域不是截图中的假控件。
- 主插画为静态二维绘画，小头像为独立透明图层；不宣称具有新骨骼动作。

## 真实数据和操作

- 香港时间 UTC+8；生活日按 03:00 换天。周日期带支持切日与前后周，历史日读取 `/api/life?day=...` 和 `/api/g40/diary?day=...`。历史请求独立于当前状态缓存，晚到的响应不能覆盖后来选中的日期；失败不填今天的数据。
- 书页可展开完整手账；记忆卡与成长卡可展开完整文本。弹层内可滚动，关闭按钮、点外侧或 Escape 退出。
- 记忆使用真实 `memory.files`，成长使用 `weekly / self_now`，不显示虚构亲密度、安全感或同步百分比。空记录显示空态。
- 音乐复用现有系统媒体桥，上一首 / 播放暂停 / 下一首按真实能力可用性显示。长曲名省略，点曲目查看完整信息；没有模拟曲目、进度或频谱。
- 此刻优先显示最新实际对话，其次读取真实手账；缺少天气或地点时明确待同步。插画和非事件性装饰句不作为事实记录。
- 展示工作台不新增 Brain 写操作；详细设置通过老板工具台入口处理。

## 从源重建

源排版、原始透明人物与小头像保存在 `Assets/Xiaoqi/Editor/SweetNotebook/Source/`，仅供美术重建，不打入运行包。

用提供 Playwright 的 Node 执行 `Assets/Xiaoqi/Editor/SweetNotebook/export.cjs`，浏览器按固定尺寸导出装饰纹理和原生文本、点击区域的坐标清单。脚本没有修改图片中的人物或后处理移动运行截图。

随后 Unity 6000.2.6f2 执行 `XqSweetNotebookBuild.VerifyAndBuild`：逐张验证 NPOT=None、不压缩、不生成 mipmap、保留 Alpha、4096 导入上限及实际尺寸，再运行既有 11 项语音策略检查和 `XiaoqiBuild.Win64`。

运行包仍经 robocopy 同步到 Tonight，排除 `xiaoqi_config.json`，不删除目录。主相机矩形保持全屏；不在圆角 VisualElement 上启用 Overflow.Hidden。

## 实机复验

使用 `-xq-sweet-capture <目录>` 让同一个生产 UIDocument 输出三页原生实机图、完整屏幕占位、装饰层对照图及 `runtime-report.json`。评审脚本还调用相同的导航和弹层动作，读取前一天手账；不会发送聊天或切换用户正在播放的歌曲。

本机最终图片保存在 `E:\dsh-pet-projects\design-previews\2026-11-19\sweet-notebook\runtime-final\`。完整截图直接来自 Unity PanelSettings.targetTexture，不经修图或位置重排。时钟、日期、实际文案和当前媒体与设计示例自然不同；美术构图使用相同资源。

验证脚本 `Assets/Xiaoqi/Editor/SweetNotebook/verify-runtime.cjs <截图目录>` 检查关键文字截断、三页点击区域、弹层、分页、历史读取与导航往返，并将实机装饰层与同源美术纹理逐像素比较。由于电脑操作工具无法枚举本机透明 Unity 窗口，操作验证使用生产代码相同的屏幕坐标命中函数和动作处理器；不把这等同于已经完成所有操作系统鼠标实点验收。既有 localization / SteamDRM 警告仍需另行处理。
