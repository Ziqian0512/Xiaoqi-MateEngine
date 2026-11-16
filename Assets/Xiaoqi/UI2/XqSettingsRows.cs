using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// P1-5 设置页的行模板。可复用行：开关 / 滑条 / 下拉。
    /// 行本身 pickingMode = Position（整行可点），行内文字一律 Ignore，空白处不设背景。
    /// 回调异常统一吞成 `XQ_SET2 ...` 警告，不让面板炸掉。
    /// </summary>
    public static class XqSettingsRows
    {
        static VisualElement MakeRow(string label, out VisualElement right)
        {
            var row = new VisualElement { pickingMode = PickingMode.Position };
            row.AddToClassList("xq-st-row");
            var text = new Label(label);
            text.AddToClassList("xq-st-row-label");
            text.pickingMode = PickingMode.Ignore;
            right = new VisualElement { pickingMode = PickingMode.Ignore };
            right.AddToClassList("xq-st-right");
            row.Add(text);
            row.Add(right);
            // 行内点击不再往上冒（防止冒到面板外的关闭逻辑）
            row.RegisterCallback<ClickEvent>(e => e.StopPropagation());
            return row;
        }

        /// <summary>行右侧灰字提示（如「暂未接入」），插在标签与控件之间。</summary>
        public static void Hint(VisualElement row, string text)
        {
            if (row == null) return;
            var hint = new Label(text);
            hint.AddToClassList("xq-st-hint");
            hint.pickingMode = PickingMode.Ignore;
            row.Insert(1, hint);
        }

        /// <summary>开关行：点整行都能切。</summary>
        public static VisualElement Toggle(string label, bool value, Action<bool> onChange)
        {
            bool state = value;
            var row = MakeRow(label, out var right);
            var tg = new VisualElement { pickingMode = PickingMode.Ignore };
            tg.AddToClassList("xq-st-tg");
            var knob = new VisualElement { pickingMode = PickingMode.Ignore };
            knob.AddToClassList("xq-st-tg-knob");
            tg.Add(knob);
            right.Add(tg);
            tg.EnableInClassList("xq-st-tg-on", state);
            row.RegisterCallback<ClickEvent>(_ =>
            {
                if (!row.enabledSelf) return;
                state = !state;
                tg.EnableInClassList("xq-st-tg-on", state);
                try { onChange?.Invoke(state); }
                catch (Exception e) { Debug.LogWarning("XQ_SET2 " + e.Message); }
            });
            return row;
        }

        /// <summary>滑条行：拖动即时 onChange；松手调 OnCommit（面板在这里落盘）；双击回默认。</summary>
        public static SliderRow Slider(string label, float min, float max, float value,
                                       Action<float> onChange, float resetTo)
        {
            return new SliderRow(label, min, max, value, onChange, resetTo);
        }

        public sealed class SliderRow
        {
            public VisualElement Root { get; private set; }
            /// <summary>松手 / 双击回默认之后调用，面板在这里 XiaoqiConfig.Save。</summary>
            public Action OnCommit;
            /// <summary>显示用倍数（例如 0-1 的值想显示成 0-100）。</summary>
            public float ValueScale = 1f;
            public string Suffix = "";
            /// <summary>数字格式，默认 "0"；大小行用 "0.00" 与旧设置页一致。</summary>
            public string Format = "0";
            /// <summary>当前值（原始范围，不是 ValueScale 之后的显示值）。</summary>
            public float Value { get { return _value; } }

            readonly VisualElement _track;
            readonly VisualElement _fill;
            readonly Label _val;
            readonly float _min, _max, _reset;
            readonly Action<float> _onChange;
            float _value;
            bool _drag;
            bool _justDragged;
            float _lastClickAt = -1f;

            internal SliderRow(string label, float min, float max, float value,
                               Action<float> onChange, float resetTo)
            {
                _min = min;
                _max = max;
                _reset = resetTo;
                _onChange = onChange;

                Root = MakeRow(label, out var right);
                _track = new VisualElement { pickingMode = PickingMode.Position };
                _track.AddToClassList("xq-st-track");
                _fill = new VisualElement { pickingMode = PickingMode.Ignore };
                _fill.AddToClassList("xq-st-fill");
                _track.Add(_fill);
                _val = new Label("");
                _val.AddToClassList("xq-st-val");
                _val.pickingMode = PickingMode.Ignore;
                right.Add(_track);
                right.Add(_val);

                _track.RegisterCallback<PointerDownEvent>(e =>
                {
                    if (!Root.enabledSelf) return;
                    e.StopPropagation();
                    _drag = true;
                    _justDragged = false;
                    // TODO(编译员)：确认 VisualElement.CapturePointer / ReleasePointer（UI Toolkit 标准 API，本仓库无先例）
                    _track.CapturePointer(e.pointerId);
                    DragTo(e.localPosition.x);
                });
                _track.RegisterCallback<PointerMoveEvent>(e =>
                {
                    if (!_drag) return;
                    DragTo(e.localPosition.x);
                });
                _track.RegisterCallback<PointerUpEvent>(e =>
                {
                    if (!_drag) return;
                    _drag = false;
                    _justDragged = true;
                    _track.ReleasePointer(e.pointerId);
                    Commit();
                });
                _track.RegisterCallback<ClickEvent>(_ =>
                {
                    if (!Root.enabledSelf) return;
                    if (_justDragged) { _justDragged = false; return; }
                    if (Time.unscaledTime - _lastClickAt < 0.35f)
                    {
                        _lastClickAt = -1f;
                        SetValue(_reset, true);
                        Commit();
                    }
                    else _lastClickAt = Time.unscaledTime;
                });

                SetValue(value, false);
            }

            /// <summary>外部改了值之后重画（不触发 onChange）。</summary>
            public void Refresh() { SetValue(_value, false); }

            public void SetValue(float v, bool notify)
            {
                _value = Mathf.Clamp(v, _min, _max);
                float t = _max > _min ? (_value - _min) / (_max - _min) : 0f;
                // TODO(编译员)：确认 Length.Percent（UI Toolkit 标准写法，本仓库无先例）
                _fill.style.width = Length.Percent(Mathf.Clamp01(t) * 100f);
                _val.text = (_value * ValueScale).ToString(Format) + Suffix;
                if (!notify) return;
                try { _onChange?.Invoke(_value); }
                catch (Exception e) { Debug.LogWarning("XQ_SET2 " + e.Message); }
            }

            void DragTo(float localX)
            {
                float w = _track.resolvedStyle.width;
                if (float.IsNaN(w) || w < 8f) return;
                SetValue(Mathf.Lerp(_min, _max, Mathf.Clamp01(localX / w)), true);
            }

            void Commit()
            {
                try { OnCommit?.Invoke(); }
                catch (Exception e) { Debug.LogWarning("XQ_SET2 " + e.Message); }
            }
        }

        /// <summary>下拉行：自己画的玻璃选项列表，不用 DropdownField（系统样式不对）。</summary>
        public static VisualElement Dropdown(string label, string[] options, int index, Action<int> onChange)
        {
            var opts = options ?? new string[0];
            int cur = opts.Length == 0 ? -1 : Mathf.Clamp(index, 0, opts.Length - 1);
            var row = MakeRow(label, out var right);
            var sel = new Label("");
            sel.AddToClassList("xq-st-sel");
            sel.pickingMode = PickingMode.Ignore;
            right.Add(sel);

            VisualElement pop = null;
            Action refresh = () =>
            {
                sel.text = (cur >= 0 && cur < opts.Length ? opts[cur] : "") + " ▾";
            };
            refresh();

            Action closePop = () =>
            {
                if (pop == null) return;
                if (pop.parent != null) pop.parent.Remove(pop);
                pop = null;
            };

            row.RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                if (!row.enabledSelf) return;
                if (pop != null) { closePop(); return; }
                var host = row.parent;
                if (host == null || opts.Length == 0) return;

                pop = new VisualElement { pickingMode = PickingMode.Position };
                pop.AddToClassList("xq-st-pop");
                float estH = opts.Length * 32f + 12f;
                var rb = row.worldBound;
                var hb = host.worldBound;
                float x = Mathf.Max(8f, rb.xMax - hb.xMin - 148f);
                float y = rb.yMax - hb.yMin + 4f;
                if (y + estH > hb.height - 8f) y = Mathf.Max(8f, rb.yMin - hb.yMin - estH - 4f);
                pop.style.left = x;
                pop.style.top = y;

                for (int i = 0; i < opts.Length; i++)
                {
                    int pick = i;
                    var opt = new Label(opts[i]);
                    opt.AddToClassList("xq-st-opt");
                    if (i == cur) opt.AddToClassList("xq-st-opt-on");
                    opt.pickingMode = PickingMode.Position;
                    opt.RegisterCallback<ClickEvent>(ev =>
                    {
                        ev.StopPropagation();
                        cur = pick;
                        refresh();
                        closePop();
                        try { onChange?.Invoke(pick); }
                        catch (Exception ex) { Debug.LogWarning("XQ_SET2 " + ex.Message); }
                    });
                    pop.Add(opt);
                }
                host.Add(pop);
            });
            return row;
        }

        /// <summary>按钮行：右侧可带一段灰字，再挂一个药丸按钮。点整行不触发，只有按钮触发。</summary>
        public static VisualElement Button(string label, string hintText, string buttonText, Action onClick)
        {
            var row = MakeRow(label, out var right);
            if (!string.IsNullOrEmpty(hintText)) Hint(row, hintText);
            var btn = new Label(buttonText);
            btn.AddToClassList("xq-st-btn");
            btn.pickingMode = PickingMode.Position;
            btn.RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                if (!row.enabledSelf) return;
                try { onClick?.Invoke(); }
                catch (Exception ex) { Debug.LogWarning("XQ_SET2 " + ex.Message); }
            });
            right.Add(btn);
            return row;
        }

        /// <summary>文字行：右侧一段灰字（只读，如模型路径、版本号）。</summary>
        public static VisualElement Text(string label, string value)
        {
            var row = MakeRow(label, out var right);
            var val = new Label(value ?? "");
            val.AddToClassList("xq-st-textval");
            val.pickingMode = PickingMode.Ignore;
            right.Add(val);
            return row;
        }

        /// <summary>说明行：整行一段灰字（如模型授权），不做左右分栏。</summary>
        public static VisualElement Note(string text)
        {
            var l = new Label(text ?? "");
            l.AddToClassList("xq-st-note");
            l.pickingMode = PickingMode.Ignore;
            return l;
        }

        /// <summary>一行两列的输入框行。password=true 时显示为圆点。</summary>
        public static InputRow Input(string label, string value, bool password, Action<string> onCommit)
        {
            return new InputRow(label, value, password, onCommit, null, null);
        }

        /// <summary>带右侧按钮的输入框行（令牌行的「粘贴」）。</summary>
        public static InputRow Input(string label, string value, bool password, Action<string> onCommit,
                                     string buttonText, Action<InputRow> onButton)
        {
            return new InputRow(label, value, password, onCommit, buttonText, onButton);
        }

        public sealed class InputRow
        {
            public VisualElement Root { get; private set; }
            public string Value { get { return _field.value ?? ""; } }

            readonly TextField _field;

            internal InputRow(string label, string value, bool password,
                              Action<string> onCommit, string buttonText, Action<InputRow> onButton)
            {
                Root = MakeRow(label, out var right);
                // TODO(编译员)：确认 UI Toolkit 的 TextField / isPasswordField / SetValueWithoutNotify（标准 API，本仓库无先例）
                _field = new TextField { pickingMode = PickingMode.Position };
                _field.AddToClassList("xq-st-input");
                _field.isPasswordField = password;
                _field.SetValueWithoutNotify(value ?? "");
                _field.RegisterCallback<KeyDownEvent>(e =>
                {
                    if (e.keyCode != KeyCode.Return && e.keyCode != KeyCode.KeypadEnter) return;
                    e.StopPropagation();
                    Commit(onCommit);
                });
                _field.RegisterCallback<FocusOutEvent>(_ => Commit(onCommit));
                right.Add(_field);

                if (string.IsNullOrEmpty(buttonText)) return;
                var btn = new Label(buttonText);
                btn.AddToClassList("xq-st-btn");
                btn.AddToClassList("xq-st-btn-gap");
                btn.pickingMode = PickingMode.Position;
                btn.RegisterCallback<ClickEvent>(e =>
                {
                    e.StopPropagation();
                    if (!Root.enabledSelf) return;
                    try { onButton?.Invoke(this); }
                    catch (Exception ex) { Debug.LogWarning("XQ_SET2 " + ex.Message); }
                });
                right.Add(btn);
            }

            /// <summary>外部改文本（粘贴）用，不触发提交。</summary>
            public void SetText(string v) { _field.SetValueWithoutNotify(v ?? ""); }

            void Commit(Action<string> onCommit)
            {
                try { onCommit?.Invoke(Value); }
                catch (Exception e) { Debug.LogWarning("XQ_SET2 " + e.Message); }
            }
        }

        /// <summary>状态行：右侧一盏灯 + 文字，可再挂一个按钮（连接页的连接状态）。</summary>
        public static StatusRow Status(string label, string buttonText, Action onButton)
        {
            return new StatusRow(label, buttonText, onButton);
        }

        public sealed class StatusRow
        {
            public VisualElement Root { get; private set; }

            readonly Label _led;
            readonly Label _text;

            internal StatusRow(string label, string buttonText, Action onButton)
            {
                Root = MakeRow(label, out var right);
                _led = new Label("");
                _led.AddToClassList("xq-st-led");
                _led.pickingMode = PickingMode.Ignore;
                _text = new Label("");
                _text.AddToClassList("xq-st-statustext");
                _text.pickingMode = PickingMode.Ignore;
                right.Add(_led);
                right.Add(_text);

                if (string.IsNullOrEmpty(buttonText)) return;
                var btn = new Label(buttonText);
                btn.AddToClassList("xq-st-btn");
                btn.AddToClassList("xq-st-btn-gap");
                btn.pickingMode = PickingMode.Position;
                btn.RegisterCallback<ClickEvent>(e =>
                {
                    e.StopPropagation();
                    if (!Root.enabledSelf) return;
                    try { onButton?.Invoke(); }
                    catch (Exception ex) { Debug.LogWarning("XQ_SET2 " + ex.Message); }
                });
                right.Add(btn);
            }

            /// <summary>state：0 = 未知，1 = 正常，2 = 异常。</summary>
            public void Set(string text, int state)
            {
                if (_text != null) _text.text = text ?? "";
                if (_led == null) return;
                _led.EnableInClassList("xq-st-led-on", state == 1);
                _led.EnableInClassList("xq-st-led-off", state == 2);
            }
        }

        /// <summary>色点行：右侧三个（或 N 个）圆点，点一下选中。colors 与 labels 一一对应。</summary>
        public static VisualElement Dots(string label, string[] labels, Color[] colors, int index, Action<int> onChange)
        {
            var row = MakeRow(label, out var right);
            int cur = Mathf.Clamp(index, 0, Mathf.Max(0, (colors != null ? colors.Length : 0) - 1));
            int n = colors != null ? colors.Length : 0;
            var dots = new VisualElement[n];
            Action refresh = () =>
            {
                for (int i = 0; i < dots.Length; i++)
                    if (dots[i] != null) dots[i].EnableInClassList("xq-st-dot-on", i == cur);
            };
            for (int i = 0; i < n; i++)
            {
                int pick = i;
                var d = new VisualElement { pickingMode = PickingMode.Position };
                d.AddToClassList("xq-st-dot");
                d.style.backgroundColor = colors[i];
                // 圆点不带文字，标签只在 tooltip 里给编译员看
                d.tooltip = labels != null && i < labels.Length ? labels[i] : "";
                d.RegisterCallback<ClickEvent>(e =>
                {
                    e.StopPropagation();
                    if (!row.enabledSelf) return;
                    cur = pick;
                    refresh();
                    try { onChange?.Invoke(pick); }
                    catch (Exception ex) { Debug.LogWarning("XQ_SET2 " + ex.Message); }
                });
                dots[i] = d;
                right.Add(d);
            }
            refresh();
            return row;
        }
    }
}
