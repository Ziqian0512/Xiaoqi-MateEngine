# xq-verify summary

- time: 2026-11-16 12:17:20
- machine: INORI02
- branch: cursor/p1-1-ui-clickthrough-bdd1  sha: a73dd06
- unity: C:\Program Files\Unity\Hub\Editor\6000.2.6f2\Editor\Unity.exe (project wants 6000.2.6f2)

## Build
- unity exit code: 0   build result line: ''   seconds: 59
- compile errors: 0   warnings: 0
- exe exists: True
- RESULT: BUILD OK

### files changed by the build (DO NOT commit prefab/scene/ProjectSettings; new .meta files are fine)
    (none)

## Smoke (primary screen 1920x1080)
- **base**: alive after 20s = True; XQ_ lines = 53; exception-ish lines = 2; screenshot = smoke-base.png
- **nap**: alive after 20s = True; XQ_ lines = 56; exception-ish lines = 2; screenshot = smoke-nap.png
- **theme-blue**: alive after 20s = True; XQ_ lines = 53; exception-ish lines = 2; screenshot = smoke-theme-blue.png
- **settings-v2**: alive after 20s = True; XQ_ lines = 54; exception-ish lines = 2; screenshot = smoke-settings-v2.png

NOTE: smoke only proves the program starts and logs. Click-through, dancing, sleeping, fullscreen-hide, 175% scale need a human at the screen (see G60 B1 table).
