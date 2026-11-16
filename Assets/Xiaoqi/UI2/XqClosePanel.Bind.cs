using System;
using UnityEngine;
using UnityEngine.UIElements;
using Xiaoqi.Mode;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// XqClosePanel 的 P2-5 部分：按 G59-P2 §3.3 接数据 + 竖栏交互。
    ///
    /// 数据：② 下一件事读 stage.next（time+事项）+ stage.spot（地点），没有就用 day 的
    /// 下一段兜底；③ 精力读 stage.body.stamina / sleepy；④ 她说读 feed 最新 kind=say。
    /// 断线或没数据一律灰字「还没有」，不保留旧数据、不编文案。
    ///
    /// 竖栏：♥ 退出近景（= 调 XqMode.Set(XqView.Pet)，与桌宠方钮同一路径）；
    /// 📅 聚焦「下一件事」卡、💬 聚焦「她说」卡（1.04 倍 0.25 秒，再点还原）；
    /// ✓ 待办灰着（服务器没有这个功能）；⚙ 设置：本分支（xiaoqi/p2）里没有设置页，
    /// 灰着并提示「还没开放」，等设置页合进来再接。
    /// </summary>
    public partial class XqClosePanel
    {
        XqCloseData _data;
        bool _daySub;
        bool _stageSub;
        int _saySeq = -1;
        VisualElement _focusCard;
        float _focusT = 1f;
        bool _focusOn;
        float _focusK = 1f;

        void BindData()
        {
            if (_host == null) return;
            try
            {
                if (_data == null) _data = XqCloseData.Ensure();
                if (!_daySub && _data != null)
                {
                    _data.Changed += OnDataChanged;
                    _daySub = true;
                }
                // OnStage 是静态事件，重复 += 会重复回调，所以只在第一次绑
                if (!_stageSub)
                {
                    XiaoqiLink.OnStage += OnStage;
                    _stageSub = true;
                }
            }
            catch (Exception e) { Debug.LogWarning("XQ_CLOSE bind: " + e.Message); }
        }

        void UnbindData()
        {
            try
            {
                if (_data != null && _daySub) _data.Changed -= OnDataChanged;
                _daySub = false;
                if (_stageSub) { XiaoqiLink.OnStage -= OnStage; _stageSub = false; }
                if (_data != null) _data.End();
            }
            catch (Exception e) { Debug.LogWarning("XQ_CLOSE unbind: " + e.Message); }
        }

        void OnDataChanged()
        {
            try { RefreshSay(); RefreshNext(); }
            catch (Exception e) { Debug.LogWarning("XQ_CLOSE refresh: " + e.Message); }
        }

        void OnStage(StageData s)
        {
            try { RefreshNext(s); RefreshEnergy(s); }
            catch (Exception e) { Debug.LogWarning("XQ_CLOSE stage: " + e.Message); }
        }

        // ---------------- ② 下一件事 ----------------

        void RefreshNext() { RefreshNext(XiaoqiLink.Current); }

        void RefreshNext(StageData s)
        {
            if (_nextTime == null) return;

            string when = null, what = null, where = null;

            var nx = s != null ? s.next : null;
            if (nx != null && (!string.IsNullOrEmpty(nx.at) || !string.IsNullOrEmpty(nx.where)))
            {
                when = nx.at;
                // 事项名 stage.next 里没有，去 day 的同起始时间的段里找
                var d = _data != null ? _data.Day : null;
                var seg = d != null ? d.At(nx.at) : null;
                what = XqCloseDay.What(seg);
                where = XqCloseDay.Place(!string.IsNullOrEmpty(nx.where) ? nx.where : (seg != null ? seg.where : null));
                if (string.IsNullOrEmpty(what)) what = where;
                else if (!string.IsNullOrEmpty(where)) what = what + " · " + where;
            }

            // stage.next 没有时用 day 的下一段兜底
            if (string.IsNullOrEmpty(when))
            {
                var d = _data != null ? _data.Day : null;
                var seg = d != null ? d.Upcoming() : null;
                if (seg != null)
                {
                    when = seg.start;
                    what = XqCloseDay.What(seg);
                    string pl = XqCloseDay.Place(seg.where);
                    if (!string.IsNullOrEmpty(pl)) what = string.IsNullOrEmpty(what) ? pl : what + " · " + pl;
                }
            }

            bool has = !string.IsNullOrEmpty(when) || !string.IsNullOrEmpty(what);
            _nextTime.style.display = has && !string.IsNullOrEmpty(when) ? DisplayStyle.Flex : DisplayStyle.None;
            _nextTime.text = has ? (when ?? "") : "";
            _nextWhat.text = has ? (string.IsNullOrEmpty(what) ? "今天没有更多安排" : what) : "还没有";
            SetGray(_nextWhat, !has);
        }

        // ---------------- ③ 精力 ----------------

        void RefreshEnergy(StageData s)
        {
            if (_energyWord == null || _energyArc == null) return;
            string word = null;
            float amount = -1f;
            if (s != null && s.body != null)
            {
                // 量纲沿用 XqPetHud：假数据里 stamina / sleepy 是 0–100
                float st = Mathf.Clamp(s.body.stamina, 0f, 100f);
                amount = st / 100f;
                if (s.body.sleepy >= 70f) word = "有点困";
                else if (st <= 35f) word = "累了";
                else word = "精神";
            }
            _energyWord.text = word ?? "还没有";
            SetGray(_energyWord, word == null);
            _energyArc.Amount = amount;
            _energyArc.MarkDirtyRepaint();
        }

        // ---------------- ④ 她说 ----------------

        void RefreshSay()
        {
            if (_sayText == null) return;
            string say = _data != null ? _data.Say : null;
            bool has = !string.IsNullOrEmpty(say);
            _sayText.text = has ? say : "还没有";
            SetGray(_sayText, !has);
            if (has)
            {
                int h = say.GetHashCode();
                if (h != _saySeq)
                {
                    _saySeq = h;
                    Pair("say updated len=" + say.Length);
                }
            }
        }

        static void SetGray(Label l, bool gray)
        {
            if (l == null) return;
            l.EnableInClassList("xq-cl-empty", gray);
        }

        // ---------------- 竖栏交互 ----------------

        /// <summary>♥ = 退出近景；📅 / 💬 = 聚焦对应卡片；两下都是点一下切换。</summary>
        void WireRail()
        {
            DotClick("♥", () =>
            {
                try { XqMode.Set(XqView.Pet); }
                catch (Exception e) { Debug.LogWarning("XQ_CLOSE rail pet: " + e.Message); }
            });
            DotClick("今", () => Focus(_cardNext));
            DotClick("话", () => Focus(_cardSay));
            DotClick("待", () => Pair("todo placeholder"));
            DotClick("设", () => Pair("settings not in this branch"));
        }

        void DotClick(string word, Action act)
        {
            if (_rail == null) return;
            var dot = DotByWord(word);
            if (dot == null) return;
            dot.pickingMode = PickingMode.Position;
            dot.RegisterCallback<ClickEvent>(e =>
            {
                try
                {
                    e.StopPropagation();
                    if (act != null) act();
                }
                catch (Exception ex) { Debug.LogWarning("XQ_CLOSE dot " + word + ": " + ex.Message); }
            });
        }

        VisualElement DotByWord(string word)
        {
            if (_rail == null) return null;
            foreach (var child in _rail.Children())
            {
                var l = child.Q(className: "xq-cl-dot-word");
                if (l is Label lb && lb.text == word) return child;
            }
            return null;
        }

        /// <summary>点一下放大聚焦到 1.04 倍，再点还原。同一时刻只有一张卡被聚焦。</summary>
        void Focus(VisualElement card)
        {
            if (card == null) return;
            if (_focusCard == card && _focusOn) _focusOn = false;
            else { _focusCard = card; _focusOn = true; }
            _focusT = 0f;
            Pair("focus " + (_focusOn ? "on" : "off"));
        }

        /// <summary>
        /// 推进聚焦缩放。0.25 秒 smoothstep，系数存在 _focusK 里，
        /// 由动画那边乘到 style.scale 上——两处都写 style.scale 会互相覆盖。
        /// </summary>
        void TickFocus()
        {
            if (_focusOn && _focusT >= 1f) { _focusK = XqCloseLayout.FocusZoom; return; }
            if (!_focusOn && _focusT >= 1f) { _focusK = 1f; return; }

            _focusT = Mathf.Min(1f, _focusT + Time.unscaledDeltaTime / XqCloseLayout.FocusSec);
            float e = _focusT * _focusT * (3f - 2f * _focusT);
            float from = _focusOn ? 1f : XqCloseLayout.FocusZoom;
            float to = _focusOn ? XqCloseLayout.FocusZoom : 1f;
            _focusK = Mathf.Lerp(from, to, e);
            if (_focusT >= 1f) _focusK = to;
        }
    }
}
