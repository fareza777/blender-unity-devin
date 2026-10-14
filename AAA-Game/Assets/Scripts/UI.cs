using UnityEngine;
using UnityEngine.UI;

namespace Slice
{
    public static class UiKit
    {
        static Font _font;
        public static Font Font => _font ? _font : _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static Canvas Canvas(string name)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var s = go.GetComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920, 1080);
            return c;
        }

        public static RectTransform Panel(Transform parent, Vector2 anchor, Vector2 size, Color col)
        {
            var go = new GameObject("panel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = anchor;
            rt.sizeDelta = size; rt.anchoredPosition = Vector2.zero;
            go.GetComponent<Image>().color = col;
            return rt;
        }

        public static Text Txt(Transform parent, string s, int size, Color col, Vector2 anchor, Vector2 pos, TextAnchor align = TextAnchor.MiddleCenter, Vector2? sizeDelta = null)
        {
            var go = new GameObject("txt", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = Font; t.text = s; t.fontSize = size; t.color = col; t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            if (sizeDelta.HasValue) rt.sizeDelta = sizeDelta.Value;
            return t;
        }

        public static RectTransform Bar(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color bg, Color fg, out Image fill)
        {
            var rt = Panel(parent, anchor, size, bg); rt.anchoredPosition = pos;
            var go = new GameObject("fill", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(rt, false);
            fill = go.GetComponent<Image>(); fill.color = fg;
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal;
            var frt = (RectTransform)go.transform;
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.offsetMin = frt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Button Btn(Transform parent, string label, int size, Vector2 pos, Vector2 dims, Color bg, Color fg)
        {
            var rt = Panel(parent, new Vector2(0.5f, 0.5f), dims, bg); rt.anchoredPosition = pos;
            var b = rt.gameObject.AddComponent<Button>();
            var colors = b.colors; colors.highlightedColor = bg * 1.35f; colors.pressedColor = bg * 0.8f; b.colors = colors;
            Txt(rt, label, size, fg, new Vector2(0.5f, 0.5f), Vector2.zero);
            return b;
        }
    }
}
