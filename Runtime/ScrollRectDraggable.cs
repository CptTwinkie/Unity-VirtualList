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
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// A <see cref="ScrollRect"/> for use with list views that have dragging behavior implemented. It prevents
/// conflicts by preventing scrolling while dragging is in progress.
/// </summary>
public class ScrollRectDraggable : ScrollRect
{
    public bool IsDragging { get; set; }

    public override void OnScroll(PointerEventData data)
    {
        if (IsDragging)
            return;

        base.OnScroll(data);
    }
}
