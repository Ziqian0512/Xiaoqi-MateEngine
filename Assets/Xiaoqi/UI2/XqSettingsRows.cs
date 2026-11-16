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
                _val.text = (_value * ValueScale).ToString("0") + Suffix;
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
    }
}
