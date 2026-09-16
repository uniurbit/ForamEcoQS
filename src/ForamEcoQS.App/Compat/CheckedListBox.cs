//MIT License
// CheckedListBox.cs - Eto has no checked list box, so this rebuilds one on top of a GridView.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Eto.Forms;

namespace ForamEcoQS.Compat
{
    public class CheckedListBox : Panel
    {
        private readonly GridView _grid;
        private readonly List<CheckedItem> _items = new List<CheckedItem>();
        private readonly ItemCollection _itemsFacade;

        public CheckedListBox()
        {
            _itemsFacade = new ItemCollection(this);

            _grid = new GridView
            {
                ShowHeader = false,
                AllowMultipleSelection = false,
                GridLines = GridLines.None,
                DataStore = _items
            };

            _grid.Columns.Add(new GridColumn
            {
                DataCell = new CheckBoxCell { Binding = Binding.Property((CheckedItem i) => i.IsChecked) },
                Editable = true,
                Width = 28,
                AutoSize = false
            });

            _grid.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property((CheckedItem i) => i.Text) },
                Editable = false,
                Expand = true
            });

            _grid.CellEdited += (s, e) => ItemCheck?.Invoke(this, EventArgs.Empty);

            // CheckOnClick behaviour: clicking anywhere on the row toggles the item.
            _grid.CellClick += (s, e) =>
            {
                if (e.Column == 0 || e.Row < 0 || e.Row >= _items.Count)
                {
                    return;
                }
                _items[e.Row].IsChecked = !(_items[e.Row].IsChecked ?? false);
                _grid.ReloadData(e.Row);
                ItemCheck?.Invoke(this, EventArgs.Empty);
            };

            Content = _grid;
        }

        public event EventHandler ItemCheck;

        /// <summary>Kept for source compatibility; this control always toggles on click.</summary>
        public bool CheckOnClick { get; set; } = true;

        public ItemCollection Items => _itemsFacade;

        public IReadOnlyList<object> CheckedItems =>
            _items.Where(i => i.IsChecked == true).Select(i => (object)i.Text).ToList();

        public bool GetItemChecked(int index) =>
            index >= 0 && index < _items.Count && _items[index].IsChecked == true;

        public void SetItemChecked(int index, bool value)
        {
            if (index < 0 || index >= _items.Count)
            {
                return;
            }
            _items[index].IsChecked = value;
            _grid.ReloadData(index);
        }

        private void Reload()
        {
            _grid.DataStore = null;
            _grid.DataStore = _items;
        }

        private class CheckedItem
        {
            public bool? IsChecked { get; set; }
            public string Text { get; set; }
        }

        public class ItemCollection : IEnumerable<object>
        {
            private readonly CheckedListBox _owner;

            internal ItemCollection(CheckedListBox owner)
            {
                _owner = owner;
            }

            public int Count => _owner._items.Count;

            public object this[int index] => _owner._items[index].Text;

            public void Add(object item) => Add(item, false);

            public void Add(object item, bool isChecked)
            {
                _owner._items.Add(new CheckedItem { Text = item?.ToString() ?? string.Empty, IsChecked = isChecked });
                _owner.Reload();
            }

            public void AddRange(IEnumerable<string> items)
            {
                foreach (var item in items)
                {
                    _owner._items.Add(new CheckedItem { Text = item, IsChecked = false });
                }
                _owner.Reload();
            }

            public void AddRange(params string[] items) => AddRange((IEnumerable<string>)items);

            public void Clear()
            {
                _owner._items.Clear();
                _owner.Reload();
            }

            public int IndexOf(object item)
            {
                string text = item?.ToString();
                return _owner._items.FindIndex(i => string.Equals(i.Text, text, StringComparison.Ordinal));
            }

            public bool Contains(object item) => IndexOf(item) >= 0;

            public IEnumerator<object> GetEnumerator() => _owner._items.Select(i => (object)i.Text).GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
