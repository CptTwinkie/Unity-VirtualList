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

namespace VirtualList
{
    // An interface used by the SimpleSource
    public interface IViewFor<T>
    {
        void Set(T value);
    }

    /// <summary>
    /// A simple data source backed by an IList (which can be an array)
    /// </summary>
    /// <typeparam name="TData">Any data container that can be passed to your prefab component through an <see cref="IViewFor{TData}"/> interface.</typeparam>
    /// <typeparam name="TView">The prefab component which must implement <see cref="IViewFor{TData}"/></typeparam>
    public class SimpleSource<TData, TView> : IListSource where TView : Component, IViewFor<TData>
    {
        private readonly IList<TData> _list;
        private AbstractVirtualList _controller;

        public SimpleSource(IList<TData> list)
        {
            _list = list;
        }

        public TData this[int index] => HasIndex(index) ? _list[index] : default;

        /// <summary>
        /// Number of items
        /// </summary>
        public int Count => _list != null ? _list.Count : 0;

        public bool HasIndex(int index) => index >= 0 && index < _list.Count;

        public void AddItem(TData item, bool notify = true)
        {
            _list.Add(item);
            SourceChange(notify);
        }
        public bool RemoveItem(TData item, bool notify = true) => SourceChange(notify, _list.Remove(item));
        public bool RemoveAt(int index, bool notify = true) => SourceChange(notify, HasIndex(index) ? RemoveAt(index) : false);

        public IReadOnlyList<TData> AsReadOnlyList() => (IReadOnlyList<TData>) _list;

        public void SetItem(GameObject view, int index)
        {
            var element = _list[index];
            var display = view.GetComponent<TView>();
            display.Set(element);
        }

        public void SetListController(AbstractVirtualList controller) => _controller = controller;

        private bool SourceChange(bool notify, bool result = true)
        {
            if (_controller != null)
            {
                _controller.Invalidate();
                if (notify)
                    _controller.OnSourceChange?.Invoke(_controller.ItemCount());
            }

            return result;
        }
    }

    /// <summary>
    /// This is an implementation of <see cref="SimpleSource{TData,TView}"/> that also includes a prefab. Prefabs set
    /// this way will override what is serialized with the list component.
    /// </summary>
    /// <typeparam name="TData">Any data container that can be passed to your prefab component through an <see cref="IViewFor{TData}"/> interface.</typeparam>
    /// <typeparam name="TView">The prefab component which must implement <see cref="IViewFor{TData}"/></typeparam>
    public class SimpleSourceWithPrefab<TData, TView> : SimpleSource<TData, TView>, IPrefabSource where TView : Component, IViewFor<TData>
    {
        private readonly GameObject _prefab;

        public SimpleSourceWithPrefab(IList<TData> list, GameObject prefab) : base(list)
        {
            _prefab = prefab;
        }

        public GameObject PrefabAt(int index)
        {
            return _prefab;
        }
    }
}
