// VirtualList
//
// Zach Kamsler
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
// CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using UnityEngine;

namespace VirtualList
{
    public class VirtualHorizontalList : AbstractVirtualList
    {
        public RectOffset Padding;
        public float CellSize;
        public float Spacing;

        protected override void OnInvalidate() => RecalculateSize();

        private void RecalculateSize()
        {
            int primary = ItemCount();
            float size = Padding.horizontal + CellSize * primary + Mathf.Max(0, primary - 1) * Spacing;
            ScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
        }

        protected override void PositionCell(GameObject cell, int index)
        {
            var trans = cell.GetComponent<RectTransform>();
            trans.SetParent(ScrollRect.content, false);

            float primaryPos = index * (CellSize + Spacing) + Padding.left;

            trans.anchorMin = new Vector2(0, 0); // bottom-left
            trans.anchorMax = new Vector2(0, 1); // bottom-right
            trans.sizeDelta = new Vector2(CellSize, -Padding.vertical);
            trans.pivot = new Vector2(0f, 1f); // anchor to bottom-right
            trans.anchoredPosition = new Vector2(primaryPos, Padding.top);
        }

        protected override Vector2 CalculateRawIndices(Rect window)
        {
            Vector2 pos = window.position;
            Vector2 size = window.size;

            const int kAxis = 0;
            float pad = Padding.left;
            float lowestPosVisible = pos[kAxis] - pad;
            float highestPosVisible = pos[kAxis] + size[kAxis] + CellSize - pad;
            float colSize = CellSize + Spacing;

            int min = (int)(lowestPosVisible / colSize);
            int max = (int)(highestPosVisible / colSize);
            return new Vector2(min, max);
        }

        public override Vector2 GetStartScrollPosition(int index) => GetOffset(index, 0f);
        public override Vector2 GetCenterScrollPosition(int index) => GetOffset(index, 0.5f);
        public override Vector2 GetEndScrollPosition(int index) => GetOffset(index, 1f);

        public override int ItemsPerRow() => 1;

        public override float ScrollPadding(bool top) => (float) (top ? Padding.left : Padding.right);

        public override float ScrollSize(out float spacing)
        {
            spacing = Spacing;
            return CellSize;
        }

        protected override void SetScrollBarSteps(int steps)
        {
            if (ScrollRect.horizontal && ScrollRect.horizontalScrollbar)
                ScrollRect.horizontalScrollbar.numberOfSteps = steps;
        }

        /// <summary>
        /// Calculates the offset to display an item with a certain percentage from the left
        /// </summary>
        private Vector2 GetOffset(int index, float percentageFromLeft)
        {
            float primaryPos = -((float)index * (CellSize + Spacing) + (float)Padding.left);
            Rect rect = Viewport.rect;
            float offset = primaryPos + ((rect.size.x - CellSize) * percentageFromLeft);
            return new Vector2(Mathf.Clamp(offset, -Mathf.Max(0f, ScrollRect.content.rect.size.x - rect.size.x), 0f), 0f);
        }
    }
}
