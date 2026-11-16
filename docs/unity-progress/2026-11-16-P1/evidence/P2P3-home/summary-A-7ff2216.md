# xq-verify summary

- time: 2026-11-16 18:59:54
- machine: INORI02
- branch: HEAD  sha: 7ff2216
- unity: C:\Program Files\Unity\Hub\Editor\6000.2.6f2\Editor\Unity.exe (project wants 6000.2.6f2)

## Build
- unity exit code: 0   build result line: 'Success'   seconds: 211
- compile errors: 0   warnings: 282
- exe exists: True
- RESULT: BUILD OK

### files changed by the build (DO NOT commit prefab/scene/ProjectSettings; new .meta files are fine)
     M Assets/AddressableAssetsData/link.xml.meta
     M "Assets/MATE ENGINE - Packages/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset"
     M Assets/Xiaoqi/Resources/XiaoqiSystems.prefab
     M Assets/Xiaoqi/UI2/XqPanelSettings.asset
     M ProjectSettings/lilToonSetting.json
    ?? Assets/Xiaoqi/Pet/BodyDirector.cs.meta
    ?? Assets/Xiaoqi/Pet/BrainLog.cs.meta
    ?? Assets/Xiaoqi/Pet/IdleDirector.cs.meta
    ?? Assets/Xiaoqi/Pet/XqClipSheet.cs.meta
    ?? Assets/Xiaoqi/Pet/XqHoverReact.cs.meta
    ?? Assets/Xiaoqi/Pet/XqPoseBlend.cs.meta
    ?? Assets/Xiaoqi/Pet/XqStageBody.cs.meta
    ?? Assets/Xiaoqi/Resources/XqSettings.uss.meta
    ?? Assets/Xiaoqi/UI2/XqSettingsApply.cs.meta
    ?? Assets/Xiaoqi/UI2/XqSettingsPanel.cs.meta
    ?? Assets/Xiaoqi/UI2/XqSettingsRows.cs.meta

## Smoke (DISPLAY2 \\.\DISPLAY2 -2560,-683 2560x1440; primary left untouched)
- **base**: alive after 20s = True; XQ_ lines = 55; exception-ish lines = 0; screenshot = smoke-base.png
- **nap**: alive after 20s = True; XQ_ lines = 58; exception-ish lines = 0; screenshot = smoke-nap.png
- **theme-blue**: alive after 20s = True; XQ_ lines = 56; exception-ish lines = 0; screenshot = smoke-theme-blue.png
- **settings-v2**: alive after 20s = True; XQ_ lines = 61; exception-ish lines = 0; screenshot = smoke-settings-v2.png

NOTE: smoke only proves the program starts and logs. Click-through, dancing, sleeping, fullscreen-hide, 175% scale need a human at the screen (see G60 B1 table).
