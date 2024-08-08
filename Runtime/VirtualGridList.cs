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
    public class VirtualGridList : AbstractVirtualList
    {
        public enum GridAxis { Horizontal = 0, Vertical = 1 }

        public RectOffset Padding;
        public GridAxis Axis;
        public Vector2 CellSize = new Vector2(100f, 100f);
        public Vector2 Spacing;
        public int Limit = 1;
        private int _axis;

        protected override void OnInvalidate()
        {
            _axis = (int)Axis;
            RecalculateSize();
        }

        private void RecalculateSize()
        {
            int primary = Mathf.CeilToInt(ItemCount() / (float)Limit);
            int otherAxis = 1 - _axis;

            Vector2 size = Vector2.zero;
            size[_axis] = CellSize[_axis] * primary + Mathf.Max(0, primary - 1) * Spacing[_axis];
            size[otherAxis] = CellSize[otherAxis] * Limit + Mathf.Min(0, Limit - 1) * Spacing[otherAxis];
            size.x += Padding.horizontal;
            size.y += Padding.vertical;
            ScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            ScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
        }

        protected override void PositionCell(GameObject cell, int index)
        {
            var trans = cell.GetComponent<RectTransform>();
            trans.SetParent(ScrollRect.content, false);

            int otherAxis = 1 - _axis;
            int primary = index / Limit;
            int secondary = index % Limit;

            float primaryPos = primary * (CellSize[_axis] + Spacing[_axis]) + PaddingForAxis(_axis);
            float secondaryPos = secondary * (CellSize[otherAxis] + Spacing[otherAxis]) + PaddingForAxis(otherAxis);

            trans.SetInsetAndSizeFromParentEdge(EdgeForAxis(_axis), primaryPos, CellSize[_axis]);
            trans.SetInsetAndSizeFromParentEdge(EdgeForAxis(otherAxis), secondaryPos, CellSize[otherAxis]);
        }

        private float PaddingForAxis(int ax) => ax == 0 ? Padding.left : Padding.top;

        private RectTransform.Edge EdgeForAxis(int ax) => ax == 1 ? RectTransform.Edge.Top : RectTransform.Edge.Left;

        protected override Vector2 CalculateRawIndices(Rect window)
        {
            Vector2 pos = window.position;
            Vector2 size = window.size;

            float pad = PaddingForAxis(_axis);
            float lowestPosVisible = pos[_axis] - pad;
            float highestPosVisible = pos[_axis] + size[_axis] + CellSize[_axis] - pad;
            float rowSize = CellSize[_axis] + Spacing[_axis];

            int min = Limit * RowAtPos(lowestPosVisible, rowSize);
            int max = Limit * RowAtPos(highestPosVisible, rowSize);
            return new Vector2(min, max);
        }

        private int RowAtPos(float pos, float rowSize) => (int)(pos / rowSize);

        public override Vector2 GetStartScrollPosition(int index) => GetOffset(index, 0f);
        public override Vector2 GetCenterScrollPosition(int index) => GetOffset(index, 0.5f);
        public override Vector2 GetEndScrollPosition(int index) => GetOffset(index, 1f);

        public override int ItemsPerRow() => Limit;

        public override float ScrollPadding(bool top)
        {
            if (Axis == GridAxis.Vertical)
                return top ? Padding.top : Padding.bottom;

            return top ? Padding.left : Padding.right;
        }

        public override float ScrollSize(out float spacing)
        {
            int axis = Axis == GridAxis.Horizontal ? 0 : 1;
            spacing = Spacing[axis];
            return CellSize[axis];
        }

        protected override void SetScrollBarSteps(int steps)
        {
            if (Axis == GridAxis.Horizontal && ScrollRect.horizontal && ScrollRect.horizontalScrollbar)
                ScrollRect.horizontalScrollbar.numberOfSteps = steps;

            if (Axis == GridAxis.Vertical && ScrollRect.vertical && ScrollRect.verticalScrollbar)
                ScrollRect.verticalScrollbar.numberOfSteps = steps;
        }

        /// <summary>
        /// Calculates the offset to display an item with a certain percentage from the top or left
        /// </summary>
        private Vector2 GetOffset(int index, float percentageFromStart)
        {
            int primary = index / Limit; // suspicious int divide
            float primaryPos = (float)primary * (CellSize[_axis] + Spacing[_axis]) + PaddingForAxis(_axis);
            float offset = primaryPos - ((Viewport.rect.size[_axis] - CellSize[_axis]) * percentageFromStart);
            offset = Mathf.Clamp(offset, 0f, ScrollRect.content.rect.size[_axis] - Viewport.rect.size[_axis]);

            return Axis == GridAxis.Vertical
                ? new Vector2(0f, offset)
                : new Vector2(-offset, 0f);
        }
    }
}
