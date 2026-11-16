# R2-1 · 语法体检（xiaoqi/int-d1）

> 2026-11-16 14:40（UTC+8）· Grok · 只汇报，不修 · 对应任务书 v2.5

- 分支：`xiaoqi/int-d1` @ `a936ed09e69eea9799c4f462f90ca410d650af5f`
- 范围：`Assets/Xiaoqi/**/*.cs`
- 工具：tree-sitter 0.26.1 + tree-sitter-c-sharp 0.23.5（Linux 上只做语法解析，扫描所有 ERROR / MISSING 节点；没有 Unity DLL，所以不做语义检查）
- 检查文件数：**50**
- 语法错误：**0**

历史：`ee26640a`（49 个文件，0 个错误）、`bcbfa2da`（49 个文件，0 个错误），这两个提交都已作废。

说明：tree-sitter 只按语法规则解析，比 Roslyn 宽松一些，个别很新的 C# 语法可能看不出问题。「找不到类型」这类错误不在检查范围内，D3 新用到的 API（`TextField`、`isPasswordField`、`SetValueWithoutNotify`、`KeyDownEvent`、`GUIUtility.systemCopyBuffer`）也要等 R2-2 在 Inori02 上用 Unity 编译时才能确认。
