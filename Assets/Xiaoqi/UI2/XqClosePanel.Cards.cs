using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// XqClosePanel 的 3b 部分：② 下一件事卡、③ 精力卡、④ 她说卡、⑤ 今日小事卡、⑥ 一起专注圆环。
    /// 位置尺寸见 XqCloseLayout；玻璃结构沿用 xq.uss 的 xq-body / xq-glass / xq-hi。
    ///
    /// ⑤ 按 §3.3 整张标「占位」并写死示例（服务器没有这些字段），
    /// ⑥ 灰色、点击无反应、标「占位」（服务器没有这个功能）。
    /// ②③④ 由 XqClosePanel.Bind.cs 填内容，本文件只把结构和引用备好。
    /// 不加 xq-shadow：§6 的顺序铁律是先确认全屏窗口里的穿透（卡片上能点、旁边透明处
    /// 透到记事本）依然成立，再补阴影。
    /// </summary>
    public partial class XqClosePanel
    {
        VisualElement _cardNext, _cardEnergy, _cardSay, _cardTiny, _ringFocus;
        VisualElement _bodyNext, _bodyEnergy, _bodySay, _bodyTiny;
        Label _ringWord;

        // ② 下一件事：大字时间 + 小字事项
        Label _nextTime, _nextWhat;
        // ③ 精力：圆环 + 一个词
        XqArc _energyArc;
        Label _energyWord;
        // ④ 她说：整句
        Label _sayText;

        // 字号要跟着 u 走的三类文本，建的时候直接存下来，不在运行时遍历树
        readonly List<Label> _kickers = new List<Label>();
        readonly List<Label> _marks = new List<Label>();
        readonly List<Label> _texts = new List<Label>();
        readonly List<Label> _bigs = new List<Label>();

        // ⑤ 占位折线的写死示例值（0–1），服务器没这个字段，不接数据
        static readonly float[] TinyDemo = { 0.28f, 0.46f, 0.34f, 0.62f, 0.55f, 0.78f, 0.70f };

        void BuildCards()
        {
            // ② 下一件事：大字时间 + 小字事项 + 地点
            _cardNext = CardShell("xq-cl-card-next", false, "xq-close-card-next", out _bodyNext);
            _bodyNext.Add(Kicker("下一件事"));
            _nextTime = Big("");
            _bodyNext.Add(_nextTime);
            _nextWhat = Text("还没有");
            _bodyNext.Add(_nextWhat);
            Add("card-next", XqCloseLayout.NextL, XqCloseLayout.NextT,
                XqCloseLayout.NextW, XqCloseLayout.NextH);

            // ③ 精力：圆环 + 一个词
            _cardEnergy = CardShell("xq-cl-card-energy", false, "xq-close-card-energy", out _bodyEnergy);
            _bodyEnergy.Add(Kicker("精力"));
            var energyRow = new VisualElement { pickingMode = PickingMode.Ignore };
            energyRow.AddToClassList("xq-cl-row-left");
            _energyArc = new XqArc();
            _energyArc.AddToClassList("xq-cl-arc");
            energyRow.Add(_energyArc);
            _energyWord = Text("还没有");
            energyRow.Add(_energyWord);
            _bodyEnergy.Add(energyRow);
            Add("card-energy", XqCloseLayout.EnergyL, XqCloseLayout.EnergyT,
                XqCloseLayout.EnergyW, XqCloseLayout.EnergyH);

            // ④ 她说（强调版玻璃）：整句完整显示
            _cardSay = CardShell("xq-cl-card-say", true, "xq-close-card-say", out _bodySay);
            _bodySay.Add(Kicker("她说"));
            _sayText = Text("还没有");
            _sayText.AddToClassList("xq-cl-say-text");
            _bodySay.Add(_sayText);
            Add("card-say", XqCloseLayout.SayL, XqCloseLayout.SayT,
                XqCloseLayout.SayW, XqCloseLayout.SayH);

            // ⑤ 今日小事（占位：整张标「占位」，内容写死示例）
            _cardTiny = CardShell("xq-cl-card-tiny", false, "xq-close-card-tiny", out _bodyTiny);
            var tinyHead = new VisualElement { pickingMode = PickingMode.Ignore };
            tinyHead.AddToClassList("xq-cl-row");
            tinyHead.Add(Kicker("今日小事"));
            tinyHead.Add(Mark());
            _bodyTiny.Add(tinyHead);
            var spark = new XqMiniLine(TinyDemo);
            spark.AddToClassList("xq-cl-spark");
            _bodyTiny.Add(spark);
            _bodyTiny.Add(Text("今天聊了 14 句"));
            Add("card-tiny", XqCloseLayout.TinyL, XqCloseLayout.TinyT,
                XqCloseLayout.TinyW, XqCloseLayout.TinyH);

            // ⑥ 一起专注圆环（占位）
            _ringFocus = new VisualElement { name = "xq-close-ring", pickingMode = PickingMode.Position };
            _ringFocus.AddToClassList("xq-cl-ring");
            _ringFocus.generateVisualContent += DrawRing;
            _ringWord = new Label("占位") { pickingMode = PickingMode.Ignore };
            _ringWord.AddToClassList("xq-cl-ring-word");
            _ringFocus.Add(_ringWord);
            // 占位圆环点了不该有反应，也不该让点击漏到下面的窗口
            _ringFocus.RegisterCallback<ClickEvent>(e =>
            {
                try { e.StopPropagation(); Pair("ring placeholder"); }
                catch (Exception ex) { Debug.LogWarning("XQ_CLOSE ring: " + ex.Message); }
            });
            Add("ring", XqCloseLayout.RingL, XqCloseLayout.RingT,
                XqCloseLayout.RingSize, XqCloseLayout.RingSize);

            _host.Add(_cardNext);
            _host.Add(_cardEnergy);
            _host.Add(_cardSay);
            _host.Add(_cardTiny);
            _host.Add(_ringFocus);
        }

        /// <summary>玻璃卡外壳：host（可点）→ body（玻璃 + 高光），返回 host 并给出内容容器。</summary>
        VisualElement CardShell(string cls, bool emph, string name, out VisualElement body)
        {
            var host = new VisualElement { name = name, pickingMode = PickingMode.Position };
            host.AddToClassList("xq-cl-card");
            host.AddToClassList(cls);

            body = new VisualElement { pickingMode = PickingMode.Ignore };
            body.AddToClassList("xq-body");

            var glass = new VisualElement { pickingMode = PickingMode.Ignore };
            glass.AddToClassList("xq-glass");
            if (emph) glass.AddToClassList("xq-glass-emph");

            var hi = new VisualElement { pickingMode = PickingMode.Ignore };
            hi.AddToClassList("xq-hi");

            body.Add(glass);
            body.Add(hi);
            host.Add(body);
            return host;
        }

        Label Kicker(string word)
        {
            var l = new Label(word) { pickingMode = PickingMode.Ignore };
            l.AddToClassList("xq-cl-kicker");
            _kickers.Add(l);
            return l;
        }

        Label Text(string word)
        {
            var l = new Label(word) { pickingMode = PickingMode.Ignore };
            l.AddToClassList("xq-cl-text");
            _texts.Add(l);
            return l;
        }

        Label Big(string word)
        {
            var l = new Label(word) { pickingMode = PickingMode.Ignore };
            l.AddToClassList("xq-cl-big");
            _bigs.Add(l);
            return l;
        }

        Label Mark()
        {
            var l = new Label("占位") { pickingMode = PickingMode.Ignore };
            l.AddToClassList("xq-cl-mark");
            _marks.Add(l);
            return l;
        }

        /// <summary>卡片的圆角、内边距、字号、精力圆环直径随 u 变；尺寸在 Place() 里给。</summary>
        void PlaceCards(float u)
        {
            float r = XqCloseLayout.CardR * u;
            float padX = XqCloseLayout.CardPadX * u;
            float padY = XqCloseLayout.CardPadY * u;

            PlaceBody(_bodyNext, r, padX, padY);
            PlaceBody(_bodyEnergy, r, padX, padY);
            PlaceBody(_bodySay, r, padX, padY);
            PlaceBody(_bodyTiny, r, padX, padY);

            float k = XqCloseLayout.CardKickerFont * u;
            for (int i = 0; i < _kickers.Count; i++) _kickers[i].style.fontSize = k;
            float m = XqCloseLayout.CardMarkFont * u;
            for (int i = 0; i < _marks.Count; i++) _marks[i].style.fontSize = m;
            float t = XqCloseLayout.CardTextFont * u;
            for (int i = 0; i < _texts.Count; i++) _texts[i].style.fontSize = t;
            float b = XqCloseLayout.CardBigFont * u;
            for (int i = 0; i < _bigs.Count; i++) _bigs[i].style.fontSize = b;

            PlaceGlyphs(u);
        }

        static void PlaceBody(VisualElement body, float r, float padX, float padY)
        {
            if (body == null) return;
            SetRadius(body, r);
            body.style.paddingTop = padY;
            body.style.paddingBottom = padY;
            body.style.paddingLeft = padX;
            body.style.paddingRight = padX;
        }
    }
}
