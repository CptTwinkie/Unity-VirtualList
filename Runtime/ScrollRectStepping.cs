// VirtualList by Zach Kamsler
//
// Enhancements and ScrollRect components by Charles Winters
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
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VirtualList;

/// <summary>
/// A <see cref="ScrollRect"/> for use with VirtualList list views to provide scroll stepping.
/// </summary>
public class ScrollRectStepping : ScrollRect
{
    [SerializeField] private AbstractVirtualList _virtualList = default;

    private bool HorizontalStepping => horizontal && horizontalScrollbar && horizontalScrollbar.numberOfSteps > 0;
    private bool VerticalStepping => vertical && verticalScrollbar && verticalScrollbar.numberOfSteps > 0;

    protected override void OnEnable()
    {
        base.OnEnable();

        if (!Application.isPlaying)
        {
            return;
        }

        if (_virtualList)
        {
            _virtualList.UpdateScrollbarSteps();
            _virtualList.OnSourceChange.AddListener(OnSourceChange);
        }

    }

    private void OnSourceChange(int _)
    {
        _virtualList.UpdateScrollbarSteps();
    }

    public override void OnScroll(PointerEventData data)
    {
        if (VerticalStepping || HorizontalStepping)
        {
            StepScroll(data);
        }
        else
        {
            base.OnScroll(data);
        }
    }

    private void StepScroll(PointerEventData data)
    {
        Vector2 delta = data.scrollDelta;
        bool stepMoveX = delta.x != 0f && HorizontalStepping;
        bool stepMoveY = delta.y != 0f && VerticalStepping;

        if (stepMoveX)
        {
            float stepSize = horizontalScrollbar.stepSize;
            if (horizontalScrollbar.reverseValue)
            {
                stepSize = -stepSize;
            }

            float newValue = horizontalScrollbar.value + (delta.y > 0f ? stepSize : -stepSize);
            horizontalScrollbar.value = Mathf.Clamp01(newValue);
        }

        if (stepMoveY)
        {
            float stepSize = verticalScrollbar.stepSize;
            if (verticalScrollbar.reverseValue)
            {
                stepSize = -stepSize;
            }

            float newValue = verticalScrollbar.value + (delta.y > 0f ? stepSize : -stepSize);
            verticalScrollbar.value = Mathf.Clamp01(newValue);
        }

        data.scrollDelta = new Vector2(!stepMoveX ? delta.x : 0f, !stepMoveY ? delta.y : 0f);
        base.OnScroll(data);
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (!Application.isPlaying)
        {
            return;
        }

        _virtualList.OnSourceChange.RemoveListener(OnSourceChange);
    }
}
