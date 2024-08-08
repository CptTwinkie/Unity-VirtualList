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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace VirtualList
{
    public class VirtualListSourceChange : UnityEvent<int> { }

    /// <summary>
    /// This is where the magic happens. This class is implemented with specifics for each list type.
    /// <para>The implementation should be placed on the content parent, a child of the ScrollRect object.</para>
    /// <para>Don't forget to set the ScrollRect</para>
    /// </summary>
    public abstract class AbstractVirtualList : MonoBehaviour
    {
        #region Data

        [SerializeField] public ScrollRect ScrollRect;
        /// <summary>
        /// The serialized prefab source. For the code source see <see cref="_prefabSource"/>
        /// </summary>
        [Tooltip("The prefab to instance for the list. Prefabs set through the source override this one")]
        [SerializeField] public GameObject TilePrefab;
        [Tooltip("Used when calculating active indices. Increasing the buffer will instantiate additional prefab instances at the top and bottom")]
        [SerializeField] public int Buffer;
        [SerializeField] public VirtualListSourceChange OnSourceChange = new VirtualListSourceChange();

        private RectTransform _viewport;

        /// <summary>
        /// The list source as passed in from <see cref="SetSource"/>
        /// </summary>
        private IListSource _source;
        /// <summary>
        /// The prefab source passed in from the source. For the serialized source see <see cref="TilePrefab"/>
        /// </summary>
        private IPrefabSource _prefabSource;

        /// <summary>
        /// Number of elements that have been commited (re-parented) to the pool.
        /// </summary>
        private int _poolCommits;
        private Transform _poolParent;
        private readonly List<Cell> _pool = new List<Cell>();
        private readonly Dictionary<int, Cell> _activeCells = new Dictionary<int, Cell>();
        /// <summary>
        /// Tracks the first and last index of currently active cells [start, end)
        /// </summary>
        protected Vector2 _activeIndices = Vector2.zero;

        /// <summary>
        /// This is created on an inactive child object. Pooled items are inactivated by re-parenting them under this game object.
        /// </summary>
        private Transform PoolParent
        {
            get
            {
                if (_poolParent == null)
                {
                    // Cells that are moved to the pool are re-parented with the following.
                    // Because the following object is not active, re-parenting has the effect
                    //   of disabling the component hierarchy of the cell moving to the pool.
                    var go = new GameObject("PoolParent", typeof(RectTransform));
                    go.SetActive(false);
                    _poolParent = go.transform;
                    _poolParent.SetParent(transform, false);
                }

                return _poolParent;
            }
        }

        protected RectTransform Viewport
        {
            get
            {
                if (_viewport == null)
                {
                    _viewport = ScrollRect.viewport != null
                        ? ScrollRect.viewport.GetComponent<RectTransform>()
                        : ScrollRect.GetComponent<RectTransform>();
                }
                return _viewport;
            }
        }

        public IListSource Source => _source;

        private class Cell
        {
            public readonly GameObject View;
            public readonly GameObject Prefab;

            public Cell(GameObject view, GameObject prefab)
            {
                View = view;
                Prefab = prefab;
            }
        }

        #endregion Data

        #region Source Data

        /// <summary>
        /// Sets the data source for the virtual list
        /// </summary>
        public void SetSource(IListSource source)
        {
            _source = source;
            _prefabSource = source as IPrefabSource;
            _source.SetListController(this);

            if (_prefabSource == null && TilePrefab == null)
                Debug.LogError("VirtualList does not have a prefab set.", this);

            Invalidate();
            OnSourceChange?.Invoke(ItemCount());
        }

        /// <summary>
        /// Refreshes view - Call if contents of source changes in a way not handled by cells
        /// </summary>
        public void Invalidate()
        {
            OnInvalidate();
            UpdateVisibilityDisjoint();
        }

        /// <summary>
        /// Removes the data source for the virtual list. Note, this will not destroy any pooled elements, so they can
        /// be reused on a subsequent <see cref="SetSource"/> call.
        /// </summary>
        public void RemoveSource()
        {
            SetSource(null);
        }

        /// <summary>
        /// Clears the list and destroy pooled elements
        /// </summary>
        public void Clear()
        {
            _source = null;
            _prefabSource = null;
            OnSourceChange?.Invoke(ItemCount());
            foreach (var pair in _activeCells)
            {
                Object.Destroy(pair.Value.View);
            }
            _activeCells.Clear();
            _activeIndices = Vector2.zero;

            if (_poolParent != null)
            {
                Object.Destroy(_poolParent.gameObject);
                _poolParent = null;
            }
            _pool.Clear();
        }

        #endregion Source Data

        #region Scrolling

        /// <summary>
        /// Scrolls as-needed to move the cell at the index into view
        /// </summary>
        public void ScrollTo(int index)
        {
            if (!HasIndex(index))
                return;

            Vector2 currentPos = ScrollRect.content.anchoredPosition;

            // Move up/left
            Vector2 startPos = GetStartScrollPosition(index);
            if (currentPos.y > startPos.y || currentPos.x > startPos.x)
            {
                ScrollRect.content.anchoredPosition = startPos;
                UpdateVisibility();
                return;
            }

            // Move down/right
            Vector2 endPos = GetEndScrollPosition(index);
            if (currentPos.y < endPos.y || currentPos.x < endPos.x)
            {
                ScrollRect.content.anchoredPosition = endPos;
                UpdateVisibility();
                return;
            }

            // Don't move
        }

        /// <summary>
        /// Scrolls the prefab at the index to the top of the list
        /// </summary>
        public void ScrollToStart(int index)
        {
            if (!HasIndex(index))
                return;

            ScrollRect.content.anchoredPosition = GetStartScrollPosition(index);
            UpdateVisibilityDisjoint();
        }

        /// <summary>
        /// Scrolls the prefab at the index to the center of the list
        /// </summary>
        public void ScrollToCenter(int index)
        {
            if (!HasIndex(index))
                return;

            ScrollRect.content.anchoredPosition = GetCenterScrollPosition(index);
            UpdateVisibilityDisjoint();
        }

        /// <summary>
        /// Scrolls the prefab at the index to the bottom of the list
        /// </summary>
        public void ScrollToEnd(int index)
        {
            if (!HasIndex(index))
                return;

            ScrollRect.content.anchoredPosition = GetEndScrollPosition(index);
            UpdateVisibilityDisjoint();
        }

        /// <summary>
        /// Sets the stepping on the scrollbars to scroll in one-item increments
        /// </summary>
        public void UpdateScrollbarSteps()
        {
            int steps = 0;
            if (ItemCount() > 0)
            {
                // Viewport size
                float remainingSize = Viewport.rect.size[ScrollRect.horizontal ? 0 : 1];
                remainingSize -= ScrollPadding(top: true);

                // Count cells until the viewport runs out
                int rows = 0;
                bool afterFirst = false;
                float cellSize = ScrollSize(out float spacing);
                do
                {
                    rows++;
                    remainingSize -= cellSize;
                    if (afterFirst)
                        remainingSize -= spacing;
                    else
                        afterFirst = true;
                }
                while (remainingSize > 1f); // one pixel for floating point errors

                // Check if all the rows fit inside the viewport. If they all fit then there is no scrolling.
                int visibleRows = Mathf.Min(rows, ItemCount());
                int totalRows = RowCount();
                if (totalRows > visibleRows)
                    steps = totalRows - visibleRows + 1;
            }

            SetScrollBarSteps(steps);
        }

        #endregion Scrolling

        #region Getters

        public int ItemCount() => _source != null ? _source.Count : 0;
        public bool HasIndex(int index) => _source.HasIndex(index);
        public int RowCount() => ItemCount() / ItemsPerRow();

        /// <summary>
        /// For manually iterating over active cells - use with care
        /// </summary>
        public int StartIndex => (int)_activeIndices.x;

        /// <summary>
        /// For manually iterating over active cells - use with care.
        /// <para>Index after the last active index</para>
        /// </summary>
        public int EndIndex => (int)_activeIndices.y;

        /// <summary>
        /// Do not confuse active with visible, they are not the same.
        /// <para>Don't forget to null check</para>
        /// </summary>
        public GameObject GetActiveCell(int index)
        {
            _activeCells.TryGetValue(index, out Cell cell);
            return cell != null ? cell.View : null;
        }


        /// <summary>
        /// Do not confuse active with visible, they are not the same.
        /// </summary>
        public IReadOnlyList<GameObject> GetActiveCells()
        {
            var list = new List<GameObject>(_activeCells.Count);
            if (_activeCells.Count > 0)
            {
                foreach (Cell cell in _activeCells.Values)
                {
                    list.Add(cell.View);
                }
            }

            return list;
        }

        #endregion Getters

        #region Internals

        #region Lifecycle

        protected virtual void Awake()
        {
            if (ScrollRect == null)
                Debug.LogError("VirtualList has no ScrollRect component set", this);
        }

        protected virtual void Start()
        {
            Invalidate();
            ScrollRect.onValueChanged.AddListener(OnScrollbarValue);
        }

        #endregion Lifecycle

        #region Abstract

        protected abstract void OnInvalidate();
        protected abstract void PositionCell(GameObject cell, int index);
        protected abstract Vector2 CalculateRawIndices(Rect window);
        public abstract Vector2 GetStartScrollPosition(int index);
        public abstract Vector2 GetCenterScrollPosition(int index);
        public abstract Vector2 GetEndScrollPosition(int index);
        public abstract int ItemsPerRow();
        public abstract float ScrollPadding(bool top);
        /// <summary>
        /// The size of each row when scrolling.
        /// </summary>
        /// <paramref name="spacing">The spacing between rows</paramref>
        public abstract float ScrollSize(out float spacing);
        protected abstract void SetScrollBarSteps(int steps);

        #endregion Abstract

        #region Visibility

        /// <summary>
        /// Decide what is visible inside the viewport
        /// </summary>
        private Vector2 CalculateActiveIndices()
        {
            if (_source == null)
                return Vector2.zero;

            RectTransform viewport = Viewport;
            if (viewport == null)
            {
                Debug.LogWarning("VirtualList has no viewport", this);
                return Vector2.zero;
            }

            Vector2 viewportSize = viewport.rect.size;
            RectTransform content = ScrollRect.content;
            Vector2 anchoredPos = content.anchoredPosition;
            Vector2 sizeDelta = content.sizeDelta;
            Vector2 pivot = content.pivot;
            float viewX = -anchoredPos.x + (sizeDelta.x * pivot.x);
            float viewY = anchoredPos.y + (sizeDelta.y * (1 - pivot.y));
            Vector2 viewportPosition = new Vector2(viewX, viewY);
            Vector2 raw = CalculateRawIndices(new Rect(viewportPosition, viewportSize));

            int count = ItemCount();
            int min = Mathf.Max((int)Mathf.Min(raw.x, raw.y) - Buffer, 0);
            int max = Mathf.Min((int)Mathf.Max(raw.x, raw.y) + Buffer, count);
            return new Vector2(min, max);
        }

        protected void OnScrollbarValue(Vector2 scrollValue) => UpdateVisibility();

        /// <summary>
        /// Refresh list visibility
        /// </summary>
        private void UpdateVisibility()
        {
            if (ScrollRect == null)
                return;

            Vector2 newActiveIndices = CalculateActiveIndices();

            if (_activeIndices == newActiveIndices)
                return;

            // Special case for no overlap - I think this is an optimization to quickly refill the list
            if (_activeIndices.y <= newActiveIndices.x || _activeIndices.x >= newActiveIndices.y)
            {
                UpdateVisibilityDisjoint(newActiveIndices);
            }
            else
            {
                // Deactivate first
                for (int i = (int)_activeIndices.x; i < newActiveIndices.x; i++)
                    ActivateCell(i, false);

                for (int i = (int)_activeIndices.y; i >= newActiveIndices.y; i--)
                    ActivateCell(i, false);

                // Then activate
                for (int i = (int)Mathf.Min(newActiveIndices.y, _activeIndices.x) - 1; i >= newActiveIndices.x; i--)
                    ActivateCell(i, true);

                for (int i = (int)Mathf.Max(newActiveIndices.x, _activeIndices.y); i < newActiveIndices.y; i++)
                    ActivateCell(i, true);

            }

            _activeIndices = newActiveIndices;
            CommitToPool();
        }

        private void UpdateVisibilityDisjoint()
        {
            Vector2 newActiveIndices = CalculateActiveIndices();
            UpdateVisibilityDisjoint(newActiveIndices);

            _activeIndices = newActiveIndices;
            CommitToPool();
        }

        private void UpdateVisibilityDisjoint(Vector2 newActiveIndices)
        {
            for (int i = (int)_activeIndices.x; i < _activeIndices.y; i++)
            {
                if (i < newActiveIndices.x || i >= newActiveIndices.y)
                    ActivateCell(i, false);
            }

            for (int i = (int)newActiveIndices.x; i < newActiveIndices.y; i++)
                ActivateCell(i, true);
        }

        #endregion Visibility

        #region Pooling

        /// <summary>
        /// Prefab getter. <see cref="_prefabSource"/> first, then <see cref="TilePrefab"/>
        /// </summary>
        private GameObject PrefabAt(int index)
        {
            if (_prefabSource != null)
                return _prefabSource.PrefabAt(index) ?? TilePrefab;

            return TilePrefab;
        }

        /// <summary>
        /// Pool/Un-pool a cell by index. There is also checking to ensure the correct prefab. This implies changing
        /// prefabs through code is technically supported, but it will not be performant.
        /// </summary>
        private void ActivateCell(int index, bool activate)
        {
            _activeCells.TryGetValue(index, out Cell cell);

            if (activate)
            {
                GameObject prefab = PrefabAt(index);
                if (cell != null && cell.Prefab != prefab)
                {
                    ReturnCellToPool(cell);
                    cell = null;
                }

                if (cell == null)
                {
                    cell = GetCellFromPool(prefab);
                    _activeCells[index] = cell;
                    PositionCell(cell.View, index);
                }

                if (_source != null)
                    _source.SetItem(cell.View, index);
            }
            else if (cell != null)
            {
                ReturnCellToPool(cell);
                _activeCells.Remove(index);
            }
        }

        private Cell GetCellFromPool(GameObject prefab)
        {
            for (int i = _pool.Count - 1; i >= 0; --i)
            {
                Cell cell = _pool[i];
                if (cell.Prefab == prefab)
                {
                    _pool.RemoveAt(i);
                    if (i < _poolCommits)
                    {
                        _poolCommits -= 1;
                    }

                    return cell;
                }
            }
            return new Cell(Instantiate(prefab), prefab);
        }

        private void ReturnCellToPool(Cell pooledObject)
        {
            if (pooledObject != null)
                _pool.Add(pooledObject);
        }

        private void CommitToPool()
        {
            Transform parent = PoolParent;
            for (int i = _poolCommits; i < _pool.Count; ++i)
            {
                GameObject pooledObject = _pool[i].View;

                // @see the comments in the getter for PoolParent. The following
                //   effectively disables the component hierarchy of pooledObject.
                pooledObject.transform.SetParent(parent, false);
            }

            _poolCommits = _pool.Count;
        }

        #endregion Pooling

#if UNITY_EDITOR

        /// <summary>
        /// Used by the editor to spam some cells so that you can see the layout. It leaves you to clean up the mess yourself.
        /// </summary>
        public void PreviewLayout()
        {
            if (Application.isPlaying || TilePrefab == null)
                return;

            Transform trans = transform;
            while (trans.childCount > 0)
            {
                Transform t = trans.GetChild(0);
                t.SetParent(null);
                DestroyImmediate(t.gameObject);
            }

            OnInvalidate();

            for (int i = 0; i < 20; i++)
            {
                GameObject prefab = Instantiate(TilePrefab);
                prefab.name = "Temporary Preview";
                prefab.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
                PositionCell(prefab, i);
            }
        }
#endif

        #endregion Internals
    }
}
