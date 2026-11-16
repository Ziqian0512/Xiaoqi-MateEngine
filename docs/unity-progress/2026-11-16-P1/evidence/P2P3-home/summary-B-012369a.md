# xq-verify summary

- time: 2026-11-16 19:07:38
- machine: INORI02
- branch: HEAD  sha: 012369a
- unity: C:\Program Files\Unity\Hub\Editor\6000.2.6f2\Editor\Unity.exe (project wants 6000.2.6f2)

## Build
- unity exit code: 0   build result line: 'Success'   seconds: 264
- compile errors: 0   warnings: 282
- exe exists: True
- RESULT: BUILD OK

### files changed by the build (DO NOT commit prefab/scene/ProjectSettings; new .meta files are fine)
     M Assets/AddressableAssetsData/link.xml.meta
     M "Assets/MATE ENGINE - Packages/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset"
     M Assets/Xiaoqi/Resources/XiaoqiSystems.prefab
     M Assets/Xiaoqi/UI2/XqPanelSettings.asset
     M ProjectSettings/lilToonSetting.json
    ?? Assets/Xiaoqi/Mode.meta
    ?? Assets/Xiaoqi/Mode/XqBenchWindow.cs.meta
    ?? Assets/Xiaoqi/Mode/XqCloseCamera.cs.meta
    ?? Assets/Xiaoqi/Mode/XqEventSink.cs.meta
    ?? Assets/Xiaoqi/Mode/XqMode.cs.meta
    ?? Assets/Xiaoqi/Mode/XqModeConfig.cs.meta
    ?? Assets/Xiaoqi/Pet/BodyDirector.cs.meta
    ?? Assets/Xiaoqi/Pet/BrainLog.cs.meta
    ?? Assets/Xiaoqi/Pet/IdleDirector.cs.meta
    ?? Assets/Xiaoqi/Pet/XqClipSheet.cs.meta
    ?? Assets/Xiaoqi/Pet/XqHoverReact.cs.meta
    ?? Assets/Xiaoqi/Pet/XqPoseBlend.cs.meta
    ?? Assets/Xiaoqi/Pet/XqStageBody.cs.meta
    ?? Assets/Xiaoqi/UI2/Resources.meta
    ?? Assets/Xiaoqi/UI2/Resources/XqClose.uss.meta
    ?? Assets/Xiaoqi/UI2/XqCloseAnim.cs.meta
    ?? Assets/Xiaoqi/UI2/XqCloseData.cs.meta
    ?? Assets/Xiaoqi/UI2/XqCloseDay.cs.meta
    ?? Assets/Xiaoqi/UI2/XqCloseLayout.cs.meta
    ?? Assets/Xiaoqi/UI2/XqClosePanel.Anim.cs.meta
    ?? Assets/Xiaoqi/UI2/XqClosePanel.Bind.cs.meta
    ?? Assets/Xiaoqi/UI2/XqClosePanel.Cards.cs.meta
    ?? Assets/Xiaoqi/UI2/XqClosePanel.Controls.cs.meta
    ?? Assets/Xiaoqi/UI2/XqClosePanel.Glyphs.cs.meta
    ?? Assets/Xiaoqi/UI2/XqClosePanel.cs.meta

## Smoke (DISPLAY2 \\.\DISPLAY2 -2560,-683 2560x1440; primary left untouched)
- **base**: alive after 20s = True; XQ_ lines = 55; exception-ish lines = 0; screenshot = smoke-base.png
- **nap**: alive after 20s = True; XQ_ lines = 63; exception-ish lines = 0; screenshot = smoke-nap.png
- **theme-blue**: alive after 20s = True; XQ_ lines = 60; exception-ish lines = 0; screenshot = smoke-theme-blue.png
- **settings-v2**: alive after 20s = True; XQ_ lines = 57; exception-ish lines = 0; screenshot = smoke-settings-v2.png

NOTE: smoke only proves the program starts and logs. Click-through, dancing, sleeping, fullscreen-hide, 175% scale need a human at the screen (see G60 B1 table).
