using System;
using UnityEngine;
using UnityEngine.UIElements;
using Xiaoqi.Mode;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// XqClosePanel 的 3a 控件部分：① 竖栏、⑦ 名牌、⑧⑨ 两个方钮，以及脸区禁区自检。
    /// 与 XqClosePanel.cs 是同一个 partial 类，拆开只为单文件不超 300 行。
    /// </summary>
    public partial class XqClosePanel
    {
        /// <summary>① 左侧竖栏：顶部 ♥ 回桌宠，下面 4 个圆（今天 / 她说过的话 / 待办 / 设置）。</summary>
        void BuildRail()
        {
            _rail = new VisualElement { name = "xq-close-rail", pickingMode = PickingMode.Position };
            _rail.AddToClassList("xq-cl-rail");
            Add("rail", XqCloseLayout.RailL, XqCloseLayout.RailT,
                XqCloseLayout.RailW, XqCloseLayout.RailH);

            Dot("♥", "on");     // 回桌宠：当前所在的视图
            Dot("今", "off");
            Dot("话", "off");
            Dot("待", "off");
            Dot("设", "off");

            _host.Add(_rail);
        }

        void Dot(string word, string state)
        {
            var dot = new VisualElement { pickingMode = PickingMode.Ignore };
            dot.AddToClassList("xq-cl-dot");
            if (!string.IsNullOrEmpty(state)) dot.AddToClassList(state);
            var label = new Label(word) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("xq-cl-dot-word");
            dot.Add(label);
            _rail.Add(dot);
        }

        /// <summary>⑦ 名牌：「小祈 · 自己人」。固定文案，不由数值推出，不写认识几天。</summary>
        void BuildPlate()
        {
            _plate = new VisualElement { name = "xq-close-plate", pickingMode = PickingMode.Position };
            _plate.AddToClassList("xq-cl-plate");
            var text = new Label("小祈 · 自己人") { pickingMode = PickingMode.Ignore };
            text.AddToClassList("xq-cl-plate-text");
            _plate.Add(text);
            Add("plate", XqCloseLayout.PlateL, XqCloseLayout.PlateT,
                XqCloseLayout.PlateW, XqCloseLayout.PlateH);
            _host.Add(_plate);
        }

        /// <summary>⑧⑨ 右下两个方钮：左「桌宠」可点回桌宠，右「工作台」灰着、悬停「还没开放」。</summary>
        void BuildButtons()
        {
            _btnPet = new VisualElement { name = "xq-close-btn-pet", pickingMode = PickingMode.Position };
            _btnPet.AddToClassList("xq-cl-btn");
            AddText(_btnPet, "桌宠");
            Add("btn-pet", XqCloseLayout.BtnPetL, XqCloseLayout.BtnTop,
                XqCloseLayout.BtnSize, XqCloseLayout.BtnSize);
            _btnPet.RegisterCallback<ClickEvent>(e =>
            {
                try
                {
                    e.StopPropagation();
                    Debug.Log("XQ_CLOSE btn pet");
                    XqMode.Set(XqView.Pet);
                }
                catch (Exception ex) { Debug.LogWarning("XQ_CLOSE btn pet: " + ex.Message); }
            });

            _btnBench = new VisualElement { name = "xq-close-btn-bench", pickingMode = PickingMode.Position };
            _btnBench.AddToClassList("xq-cl-btn");
            _btnBench.AddToClassList("off");
            AddText(_btnBench, "工作台");
            Add("btn-bench", XqCloseLayout.BtnBenchL, XqCloseLayout.BtnTop,
                XqCloseLayout.BtnSize, XqCloseLayout.BtnSize);
            // 悬停提示：沿用 XqPetHud 的 EnableInClassList 写法，不做新机制
            _tipBench = new Label("还没开放") { pickingMode = PickingMode.Ignore };
            _tipBench.AddToClassList("xq-cl-tip");
            _btnBench.Add(_tipBench);
            _btnBench.RegisterCallback<MouseEnterEvent>(_ =>
            {
                try { _tipBench.EnableInClassList("on", true); }
                catch (Exception ex) { Debug.LogWarning("XQ_CLOSE tip: " + ex.Message); }
            });
            _btnBench.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                try { _tipBench.EnableInClassList("on", false); }
                catch (Exception ex) { Debug.LogWarning("XQ_CLOSE tip: " + ex.Message); }
            });

            _host.Add(_btnPet);
            _host.Add(_btnBench);
        }

        static void AddText(VisualElement parent, string word)
        {
            var label = new Label(word) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("xq-cl-btn-word");
            parent.Add(label);
        }

        // ---------------- 脸区禁区 ----------------

        /// <summary>
        /// Build 时对每个登记过的元素算一遍「屏幕比例矩形」，压到脸区禁区就写日志。
        /// 用比例而不是像素：§3.1 的禁区本身就是按屏幕百分比定义的，换分辨率结论不变。
        /// </summary>
        void AssertFaceZone()
        {
            foreach (var s in _slots)
            {
                var n = XqCloseLayout.Norm(s.left, s.top, s.wStage, s.hStage);
                if (XqCloseLayout.InFaceZone(n))
                    Debug.LogWarning("XQ_CLOSE violate " + s.name
                        + " rect=" + Fmt(n) + " face=" + Fmt(XqCloseLayout.FaceRect));
                else
                    Debug.Log("XQ_CLOSE ok " + s.name + " rect=" + Fmt(n));
            }
            var tip = XqCloseLayout.TipNear(XqCloseLayout.BtnBenchL);
            if (XqCloseLayout.InFaceZone(tip))
                Debug.LogWarning("XQ_CLOSE violate tip-bench rect=" + Fmt(tip));
            else
                Debug.Log("XQ_CLOSE ok tip-bench rect=" + Fmt(tip));
        }

        static string Fmt(Rect r)
        {
            return r.x.ToString("0.000") + "," + r.y.ToString("0.000") + " "
                + r.width.ToString("0.000") + "x" + r.height.ToString("0.000");
        }

        // ---------------- 圆角与字号（随 u 变，尺寸在 Place() 里） ----------------

        void PlaceRail(float u)
        {
            if (_rail == null) return;
            SetRadius(_rail, XqCloseLayout.RailR * u);
            _rail.style.paddingTop = XqCloseLayout.RailPad * u;
            foreach (var dot in _rail.Children())
            {
                float d = XqCloseLayout.DotSize * u;
                dot.style.width = d;
                dot.style.height = d;
                dot.style.marginBottom = XqCloseLayout.DotGap * u;
                var word = dot.Q(className: "xq-cl-dot-word");
                if (word != null) word.style.fontSize = XqCloseLayout.DotFont * u;
            }
        }

        void PlacePlate(float u)
        {
            if (_plate == null) return;
            SetRadius(_plate, XqCloseLayout.PlateH * 0.5f * u);
            var t = _plate.Q(className: "xq-cl-plate-text");
            if (t != null) t.style.fontSize = XqCloseLayout.PlateFont * u;
        }

        void PlaceButtons(float u)
        {
            for (int i = 0; i < 2; i++)
            {
                var btn = i == 0 ? _btnPet : _btnBench;
                if (btn == null) continue;
                SetRadius(btn, XqCloseLayout.BtnR * u);
                var w = btn.Q(className: "xq-cl-btn-word");
                if (w != null) w.style.fontSize = XqCloseLayout.BtnFont * u;
            }
            if (_tipBench != null) _tipBench.style.fontSize = XqCloseLayout.BtnFont * u;
        }

        static void SetRadius(VisualElement el, float r)
        {
            el.style.borderTopLeftRadius = r;
            el.style.borderTopRightRadius = r;
            el.style.borderBottomLeftRadius = r;
            el.style.borderBottomRightRadius = r;
        }
    }
}
