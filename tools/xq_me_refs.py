#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""G55 6.5：检查 Assets/Xiaoqi 下是否还直接引用 Mate Engine 的类。

用法：python tools/xq_me_refs.py [--root .]
退出码：引用数 > 0 时返回 1。
"""
import argparse
import os
import re
import sys

ME_DIR = os.path.join("Assets", "MATE ENGINE - Scripts")
XQ_DIR = os.path.join("Assets", "Xiaoqi")
WHITELIST = {
    os.path.join("Assets", "Xiaoqi", "Host", "MeHost.cs"),
    os.path.join("Assets", "Xiaoqi", "Runtime", "XiaoqiHide.cs"),
    os.path.join("Assets", "Xiaoqi", "Runtime", "XiaoqiAudit.cs"),
    os.path.join("Assets", "Xiaoqi", "Runtime", "XiaoqiRunKey.cs"),
    os.path.join("Assets", "Xiaoqi", "Runtime", "XiaoqiDefaults.cs"),
}

# 与 Unity / .NET 同名、会造成误报的名字（脚本会把它们打印出来）
UNITY_DOTNET_NAMES = {
    "Object", "String", "Action", "Func", "List", "Dictionary", "IList", "IEnumerator",
    "DateTime", "TimeSpan", "Path", "File", "Directory", "Environment", "Math", "Convert",
    "Color", "Color32", "Vector2", "Vector3", "Vector4", "Quaternion", "Matrix4x4",
    "Rect", "Bounds", "Ray", "Camera", "Screen", "Time", "Input", "Debug", "Application",
    "Resources", "GameObject", "Transform", "Component", "Behaviour", "MonoBehaviour",
    "Renderer", "SkinnedMeshRenderer", "Animator", "AnimationClip", "AudioClip", "AudioSource",
    "Texture", "Texture2D", "Sprite", "Material", "Shader", "Canvas", "CanvasGroup",
    "Button", "Image", "Text", "Toggle", "Slider", "Dropdown", "ScrollRect", "LayoutGroup",
    "RectTransform", "Font", "Coroutine", "WaitForSeconds", "WaitForEndOfFrame",
    "SystemInfo", "QualitySettings", "AssetBundle", "SceneManager", "Scene",
    "SpriteMeshType", "TextAnchor", "WrapMode", "FilterMode", "TextureWrapMode",
    "CameraClearFlags", "RenderMode", "ScreenCapture", "JsonUtility", "PlayerPrefs",
    "KeyCode", "AudioListener", "Light", "ParticleSystem", "Physics", "Collider",
    "AnimationCurve", "Gradient", "ScriptableObject", "SerializeField", "HideInInspector",
    "RuntimeInitializeOnLoadMethod", "RuntimeInitializeLoadType", "Application",
}

DECL_RE = re.compile(
    r"^\s*(?:(?:public|internal|private|protected|static|sealed|abstract|partial|readonly|unsafe|new)\s+)*"
    r"(?:class|struct|enum|interface)\s+([A-Za-z_]\w*)",
    re.MULTILINE,
)
NS_RE = re.compile(r"^\s*namespace\s+([A-Za-z_][\w\.]*)", re.MULTILINE)


def strip_comments(text: str) -> str:
    text = re.sub(r"/\*.*?\*/", lambda m: "\n" * m.group(0).count("\n"), text, flags=re.S)
    text = re.sub(r"//[^\n]*", "", text)
    return text


def collect_me_names(root: str):
    names = set()
    me_root = os.path.join(root, ME_DIR)
    for base, _, files in os.walk(me_root):
        for fn in files:
            if not fn.endswith(".cs"):
                continue
            path = os.path.join(base, fn)
            try:
                text = open(path, encoding="utf-8", errors="ignore").read()
            except OSError:
                continue
            text = strip_comments(text)
            for m in DECL_RE.finditer(text):
                names.add(m.group(1))
            for m in NS_RE.finditer(text):
                names.add(m.group(1).split(".")[0])
    return names


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", default=".")
    args = ap.parse_args()
    root = os.path.abspath(args.root)

    me_names = collect_me_names(root)
    dropped = sorted(me_names & UNITY_DOTNET_NAMES)
    me_names -= UNITY_DOTNET_NAMES
    # 太短/太通用的名字不参与整词搜索
    me_names = {n for n in me_names if len(n) > 2}

    print("ME names collected: %d" % len(me_names))
    if dropped:
        print("dropped (Unity/.NET same name): " + ", ".join(dropped))

    hits = []
    for base, _, files in os.walk(os.path.join(root, XQ_DIR)):
        for fn in files:
            if not fn.endswith(".cs"):
                continue
            path = os.path.join(base, fn)
            rel = os.path.relpath(path, root).replace("/", os.sep)
            if rel in WHITELIST:
                continue
            try:
                lines = open(path, encoding="utf-8", errors="ignore").read().splitlines()
            except OSError:
                continue
            in_block = False
            for i, line in enumerate(lines, 1):
                raw = line
                probe = line.strip()
                if in_block:
                    if "*/" in probe:
                        in_block = False
                    continue
                if probe.startswith("/*"):
                    in_block = "*/" not in probe
                    continue
                if probe.startswith("//"):
                    continue
                probe = re.sub(r"//.*$", "", line)
                probe = re.sub(r'"(\\.|[^"\\])*"', '""', probe)
                for name in me_names:
                    if re.search(r"\b%s\b" % re.escape(name), probe):
                        hits.append((rel, i, name, raw.strip()))

    hits.sort()
    for rel, i, name, raw in hits:
        print("%s:%d: %s   | %s" % (rel, i, name, raw))
    print("total=%d" % len(hits))
    return 1 if hits else 0


if __name__ == "__main__":
    sys.exit(main())
