# Campus Now sample — review and reproduction

Status: historical opt-in Unity runtime sample. The default workbench is now the approved three-page Sweet Notebook design; see SWEET-NOTEBOOK.md. This Campus sample remains accessible only with its explicit flag. Only its Now page was redesigned; Life and Memory retain the previous content. Detailed settings open the existing browser console through the Tools entry.

## Open the shipped sample

Close the current Xiaoqi instance first, then run (the launcher checks for an existing Xiaoqi process and refuses to create a second instance):

`E:\dsh-pet-projects\Xiaoqi-Tonight\5-campus-preview.cmd`

Equivalent command:

```text
Xiaoqi.exe -xq-theme violet -settings-v2 -xq-bench -xq-mode bench -xq-campus-preview
```

The sample is a live page in the actual Windows player. Its text, cards, photos and music buttons are UI Toolkit elements. The capture flag is not needed to display it. Without `-xq-campus-preview`, the existing workbench is used.

## Pixel and asset contract

- At a 1920x1080 viewport, the runtime stage is 1440x810, centered. Both the full-viewport and exact stage crop come directly from the same runtime UI panel.
- No separate HTML layout, screenshot-only text, retouching or post-render rearrangement is used.
- Time is explicitly Hong Kong time (UTC+8). State, diary excerpts, schedule, weather and song metadata are read from existing sources; missing data is shown as unavailable. Illustrated room lighting is static artwork.
- The room and character below are committed runtime assets. They are loaded unchanged through Resources. No image generation is needed to reproduce this layout.
- Import settings preserve NPOT dimensions, use uncompressed textures without mipmaps, and preserve character alpha. This prevents the character narrowing seen with Unity default import settings.
- MainCamera remains full-screen. No rounded element uses Overflow.Hidden. This design does not depend on real-time blur, lighting, a 3D dorm scene or a new avatar animation pool.
- Illustrations in the photo area are existing static assets. The photo modal and real media controls remain operational. Capture checks do not change the playing song.

## Committed sources

- `Assets/Xiaoqi/UI2/XqCampusPreview.cs`: shared runtime layout, data binding and controls.
- `Assets/Xiaoqi/UI2/XqCampusCapture.cs`: explicit opt-in runtime panel capture and review checks.
- `Assets/Xiaoqi/Editor/XqCampusPreviewBuild.cs`: texture dimension/import verification followed by existing 11 voice assertions and XiaoqiBuild.Win64.
- `Assets/Xiaoqi/Resources/XqCampusRoom.png`: 1672x941 room background.
- `Assets/Xiaoqi/Resources/XqCampusInori.png`: 1024x1536 transparent character.

## Rebuild and capture

Build with Unity 6000.2.6f2:

```text
Unity.exe -batchmode -nographics -quit -projectPath E:\dsh-pet-projects\Xiaoqi-ME-p2 -executeMethod XqCampusPreviewBuild.VerifyAndBuild -logFile E:\dsh-pet-projects\Xiaoqi-ME-p2\build-campus-delivery.log
```

Run the built player with the ordinary sample flags plus:

```text
-xq-campus-capture E:\dsh-pet-projects\design-previews\2026-11-19\campus-sample\final
```

The capture hook waits for the actual host and data loading, saves `campus-now-full.png`, `campus-now-panel.png` and `layout.json`, then checks photo open/close and navigation. It never sends chat messages or injects example state. Clock, content and media naturally change between captures; geometry and artwork are shared with the ordinary player.

## Local review artifacts and prompts

`E:\dsh-pet-projects\design-previews\2026-11-19\campus-sample\final\`

`E:\dsh-pet-projects\design-previews\2026-11-19\campus-sample\PROMPTS.md` contains the exact prompts and reference role for both assets. Generated with the built-in image_gen tool. Source outputs were copied into the project; there is no runtime dependency on the generation directory.
