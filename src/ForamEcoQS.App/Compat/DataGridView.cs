//MIT License
// DataGridView.cs - An Eto.Forms control that keeps the WinForms DataGridView object model.
//
// See DataGridViewTypes.cs for the rationale. This control owns an Eto GridView and projects
// the rows/columns/cells model onto it, translating styles into Eto's CellFormatting event.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;

namespace ForamEcoQS.Compat
{
    public class DataGridView : Panel
    {
        private readonly GridView _grid;
        private readonly DataGridViewColumnCollection _columns;
        private readonly DataGridViewRowCollection _rows;

        private object _dataSource;
        private DataView _boundView;
        private readonly Dictionary<DataRow, DataGridViewRow> _boundRowCache = new Dictionary<DataRow, DataGridViewRow>();

        private bool _rebuildingColumns;
        private DataGridViewCell _currentCell;

        private int _updateDepth;
        private bool _pendingRebuild;

        public DataGridView()
        {
            _columns = new DataGridViewColumnCollection(this);
            _rows = new DataGridViewRowCollection(this);

            _grid = new GridView
            {
                ShowHeader = true,
                AllowMultipleSelection = true,
                GridLines = GridLines.Both,
                DataStore = _rows.Items
            };

            _grid.CellFormatting += OnGridCellFormatting;
            _grid.CellEdited += OnGridCellEdited;
            _grid.CellClick += OnGridCellClick;
            _grid.SelectionChanged += (s, e) => SelectionChanged?.Invoke(this, EventArgs.Empty);
            _grid.MouseDown += OnGridMouseDown;
            _grid.MouseUp += OnGridMouseUp;

            Content = _grid;
        }

        /// <summary>The underlying Eto grid, for the rare case a caller needs native behaviour.</summary>
        public GridView InnerGrid => _grid;

        #region WinForms-shaped properties

        public DataGridViewColumnCollection Columns => _columns;

        public DataGridViewRowCollection Rows => _rows;

        public DataGridViewCellStyle DefaultCellStyle { get; } = new DataGridViewCellStyle();

        public bool AutoGenerateColumns { get; set; } = true;

        public bool ReadOnly { get; set; }

        public bool AllowUserToAddRows { get; set; } = true;

        public bool AllowUserToDeleteRows { get; set; } = true;

        public bool RowHeadersVisible { get; set; } = true;

        public int RowHeadersWidth { get; set; } = 40;

        public DataGridViewEditMode EditMode { get; set; } = DataGridViewEditMode.EditOnKeystrokeOrF2;

        public DataGridViewAutoSizeColumnsMode AutoSizeColumnsMode { get; set; } = DataGridViewAutoSizeColumnsMode.None;

        public object ColumnHeadersHeightSizeMode { get; set; }

        public DataGridViewRowTemplate RowTemplate { get; } = new DataGridViewRowTemplate();

        public bool MultiSelect
        {
            get => _grid.AllowMultipleSelection;
            set => _grid.AllowMultipleSelection = value;
        }

        private DataGridViewSelectionMode _selectionMode = DataGridViewSelectionMode.CellSelect;

        public DataGridViewSelectionMode SelectionMode
        {
            get => _selectionMode;
            set => _selectionMode = value;
        }

        public object DataSource
        {
            get => _dataSource;
            set => SetDataSource(value);
        }

        public DataGridViewCell CurrentCell => _currentCell;

        public IReadOnlyList<DataGridViewRow> SelectedRows =>
            _grid.SelectedRows.Where(i => i >= 0 && i < _rows.Count).Select(i => _rows[i]).ToList();

        /// <summary>
        /// The WinForms grid exposes a cell-level selection. Eto selects whole rows, so this
        /// returns the cell the user last interacted with followed by the first cell of every
        /// selected row - enough for the "which sample column is selected?" questions the
        /// application asks.
        /// </summary>
        public IReadOnlyList<DataGridViewCell> SelectedCells
        {
            get
            {
                var cells = new List<DataGridViewCell>();
                if (_currentCell != null && _currentCell.OwningRow?.Grid == this && _currentCell.RowIndex >= 0)
                {
                    cells.Add(_currentCell);
                }
                int column = _currentCell?.ColumnIndex ?? 0;
                if (column >= _columns.Count)
                {
                    column = Math.Max(0, _columns.Count - 1);
                }
                foreach (var row in SelectedRows)
                {
                    var cell = row.Cells[column];
                    if (!cells.Contains(cell))
                    {
                        cells.Add(cell);
                    }
                }
                return cells;
            }
        }

        public void ClearSelection() => _grid.UnselectAll();

        public void SelectRow(int index) => _grid.SelectRow(index);

        public DataGridViewHitTestInfo HitTest(PointF location)
        {
            var cell = _grid.GetCellAt(location);
            if (cell == null || cell.RowIndex < 0)
            {
                return DataGridViewHitTestInfo.Nowhere;
            }
            return new DataGridViewHitTestInfo(DataGridViewHitTestType.Cell, cell.ColumnIndex, cell.RowIndex);
        }

        public DataGridViewHitTestInfo HitTest(float x, float y) => HitTest(new PointF(x, y));

        /// <summary>Repaints the grid; call after changing cell values or styles in bulk.</summary>
        public void Refresh()
        {
            if (_updateDepth > 0)
            {
                _pendingRebuild = true;
                return;
            }
            _grid.DataStore = null;
            _grid.DataStore = _rows.Items;
        }

        /// <summary>Repaints one row without rebuilding the whole data store.</summary>
        public void InvalidateRow(int index)
        {
            if (index >= 0 && index < _rows.Count)
            {
                _grid.ReloadData(index);
            }
        }

        #endregion

        #region Events

        public event EventHandler<DataGridViewCellEventArgs> CellValueChanged;
        public event EventHandler<DataGridViewCellFormattingEventArgs> CellFormatting;
        public event EventHandler<DataGridViewCellEventArgs> CellContentClick;
        public event EventHandler<DataGridViewCellEventArgs> CellClick;
        public event EventHandler<DataGridViewCellEventArgs> CellRightClick;
        public event EventHandler SelectionChanged;
        public event EventHandler DataBindingComplete;

        #endregion

        #region Column plumbing

        internal DataGridViewColumn ColumnAt(int index) =>
            index >= 0 && index < _columns.Count ? _columns[index] : null;

        internal void OnColumnsChanged()
        {
            foreach (var row in _rows.Items)
            {
                row.EnsureCapacity(_columns.Count);
            }
            RebuildGridColumns();
            Refresh();
        }

        internal void OnColumnInserted(int index)
        {
            foreach (var row in _rows.Items)
            {
                row.InsertValueAt(index, null);
            }
            RebuildGridColumns();
            Refresh();
        }

        internal void OnColumnRemoved(int index)
        {
            foreach (var row in _rows.Items)
            {
                row.RemoveValueAt(index);
            }
            if (_currentCell != null && _currentCell.ColumnIndex >= _columns.Count)
            {
                _currentCell = null;
            }
            RebuildGridColumns();
            Refresh();
        }

        private void RebuildGridColumns()
        {
            _rebuildingColumns = true;
            try
            {
                _grid.Columns.Clear();
                for (int i = 0; i < _columns.Count; i++)
                {
                    _grid.Columns.Add(CreateGridColumn(_columns[i], i));
                }
            }
            finally
            {
                _rebuildingColumns = false;
            }
        }

        private GridColumn CreateGridColumn(DataGridViewColumn column, int index)
        {
            bool editable = !ReadOnly && !column.ReadOnly;

            Cell dataCell;
            if (column is DataGridViewComboBoxColumn combo && combo.Items.Count > 0)
            {
                dataCell = new ComboBoxCell
                {
                    // ComboBoxCell stores its value as object, unlike the text cell.
                    Binding = new DelegateBinding<DataGridViewRow, object>(
                        row => DataGridViewFormatting.Format(row.GetValue(index), column),
                        (row, value) => SetCellFromEditor(row, index, value?.ToString())),
                    DataStore = combo.Items.Select(i => i?.ToString() ?? string.Empty).Cast<object>().ToList()
                };
            }
            else
            {
                dataCell = new TextBoxCell
                {
                    Binding = new DelegateBinding<DataGridViewRow, string>(
                        row => DataGridViewFormatting.Format(row.GetValue(index), column),
                        (row, value) => SetCellFromEditor(row, index, value)),
                    TextAlignment = column.DefaultCellStyle.Alignment == DataGridViewContentAlignment.MiddleRight
                        ? TextAlignment.Right
                        : TextAlignment.Left
                };
            }

            var gridColumn = new GridColumn
            {
                HeaderText = string.IsNullOrEmpty(column.HeaderText) ? column.Name : column.HeaderText,
                DataCell = dataCell,
                Editable = editable,
                Sortable = column.SortMode != DataGridViewColumnSortMode.NotSortable,
                Visible = column.Visible,
                Resizable = true,
                AutoSize = AutoSizeColumnsMode != DataGridViewAutoSizeColumnsMode.None
                           && AutoSizeColumnsMode != DataGridViewAutoSizeColumnsMode.Fill,
                Expand = AutoSizeColumnsMode == DataGridViewAutoSizeColumnsMode.Fill,
                CellToolTipBinding = new DelegateBinding<DataGridViewRow, string>(
                    row => row.Cells[index].ToolTipText)
            };

            if (!gridColumn.AutoSize && column.Width > 0)
            {
                gridColumn.Width = column.Width;
            }

            return gridColumn;
        }

        private void SetCellFromEditor(DataGridViewRow row, int columnIndex, string text)
        {
            var column = ColumnAt(columnIndex);
            object value = text;

            Type target = column?.ValueType;
            if (row.DataBoundItem is DataRowView view && !string.IsNullOrEmpty(column?.DataPropertyName)
                && view.Row.Table.Columns.Contains(column.DataPropertyName))
            {
                target = view.Row.Table.Columns[column.DataPropertyName].DataType;
            }

            if (target != null && target != typeof(string) && !string.IsNullOrWhiteSpace(text))
            {
                try
                {
                    value = Convert.ChangeType(text, target, System.Globalization.CultureInfo.CurrentCulture);
                }
                catch (FormatException)
                {
                    value = text;
                }
                catch (InvalidCastException)
                {
                    value = text;
                }
                catch (OverflowException)
                {
                    value = text;
                }
            }

            row.SetValue(columnIndex, value);
        }

        #endregion

        #region Row plumbing

        internal bool IsRowSelected(DataGridViewRow row)
        {
            int index = _rows.IndexOf(row);
            return index >= 0 && _grid.SelectedRows.Contains(index);
        }

        internal void SetRowSelected(DataGridViewRow row, bool selected)
        {
            int index = _rows.IndexOf(row);
            if (index < 0)
            {
                return;
            }
            if (selected)
            {
                _grid.SelectRow(index);
            }
            else
            {
                _grid.UnselectRow(index);
            }
        }

        internal void NotifyRowsChanged()
        {
            Refresh();
        }

        #endregion

        #region Data binding

        private void SetDataSource(object value)
        {
            DetachBoundView();
            _dataSource = value;

            DataView view = value switch
            {
                DataTable table => table.DefaultView,
                DataView dataView => dataView,
                _ => null
            };

            if (view == null)
            {
                _rows.ClearInternal();
                _boundRowCache.Clear();
                Refresh();
                DataBindingComplete?.Invoke(this, EventArgs.Empty);
                return;
            }

            _boundView = view;
            _boundView.ListChanged += OnBoundViewListChanged;

            if (AutoGenerateColumns && _columns.Count == 0)
            {
                GenerateColumnsFrom(view.Table);
            }

            RebuildBoundRows();
            DataBindingComplete?.Invoke(this, EventArgs.Empty);
        }

        private void DetachBoundView()
        {
            if (_boundView != null)
            {
                _boundView.ListChanged -= OnBoundViewListChanged;
                _boundView = null;
            }
        }

        private void GenerateColumnsFrom(DataTable table)
        {
            _columns.SuspendNotifications();
            try
            {
                foreach (DataColumn dataColumn in table.Columns)
                {
                    _columns.AddInternal(new DataGridViewTextBoxColumn
                    {
                        Name = dataColumn.ColumnName,
                        HeaderText = dataColumn.ColumnName,
                        DataPropertyName = dataColumn.ColumnName,
                        ValueType = dataColumn.DataType
                    });
                }
            }
            finally
            {
                _columns.ResumeNotifications();
            }
            RebuildGridColumns();
        }

        private void OnBoundViewListChanged(object sender, ListChangedEventArgs e)
        {
            if (_updateDepth > 0)
            {
                _pendingRebuild = true;
                return;
            }

            // Editing one cell must not rebuild the whole row list: normalising a large sheet
            // writes every cell, and a full rebuild per write would make that quadratic.
            if (e.ListChangedType == ListChangedType.ItemChanged)
            {
                InvalidateRow(e.NewIndex);
                return;
            }

            RebuildBoundRows();
        }

        /// <summary>
        /// Batches a run of edits: row rebuilds and repaints are deferred until the matching
        /// <see cref="EndUpdate"/>. Calls nest.
        /// </summary>
        public void BeginUpdate()
        {
            _updateDepth++;
        }

        public void EndUpdate()
        {
            if (_updateDepth == 0)
            {
                return;
            }

            _updateDepth--;
            if (_updateDepth > 0)
            {
                return;
            }

            if (_pendingRebuild)
            {
                _pendingRebuild = false;
                if (IsBound)
                {
                    RebuildBoundRows();
                    return;
                }
            }

            Refresh();
        }

        private void RebuildBoundRows()
        {
            if (_boundView == null)
            {
                return;
            }

            var newRows = new List<DataGridViewRow>(_boundView.Count);
            var live = new HashSet<DataRow>();

            for (int i = 0; i < _boundView.Count; i++)
            {
                DataRowView rowView = _boundView[i];
                live.Add(rowView.Row);

                // Reuse the wrapper so row colours and tooltips survive filtering and edits.
                if (!_boundRowCache.TryGetValue(rowView.Row, out var row))
                {
                    row = new DataGridViewRow(this);
                    _boundRowCache[rowView.Row] = row;
                }
                row.Grid = this;
                row.DataBoundItem = rowView;
                row.EnsureCapacity(_columns.Count);
                newRows.Add(row);
            }

            foreach (var stale in _boundRowCache.Keys.Where(k => !live.Contains(k)).ToList())
            {
                _boundRowCache.Remove(stale);
            }

            _rows.ReplaceInternal(newRows);
            Refresh();
            DataBindingComplete?.Invoke(this, EventArgs.Empty);
        }

        internal DataTable BoundTable => _boundView?.Table;

        internal bool IsBound => _boundView != null;

        #endregion

        #region Eto event translation

        private void OnGridCellFormatting(object sender, GridCellFormatEventArgs e)
        {
            if (e.Item is not DataGridViewRow row)
            {
                return;
            }

            int columnIndex = _grid.Columns.IndexOf(e.Column);
            if (columnIndex < 0)
            {
                return;
            }

            // Resolve the effective style: grid default, then column, then row, then cell.
            var style = DefaultCellStyle.Clone();

            var column = ColumnAt(columnIndex);
            if (column != null && column.DefaultCellStyle.HasBackColor)
            {
                style.BackColor = column.DefaultCellStyle.BackColor;
            }
            if (column != null && column.DefaultCellStyle.HasForeColor)
            {
                style.ForeColor = column.DefaultCellStyle.ForeColor;
            }

            var rowStyle = row.DefaultCellStyleOrNull;
            if (rowStyle != null && rowStyle.HasBackColor)
            {
                style.BackColor = rowStyle.BackColor;
            }
            if (rowStyle != null && rowStyle.HasForeColor)
            {
                style.ForeColor = rowStyle.ForeColor;
            }

            var cellStyle = row.Cells[columnIndex].StyleOrNull;
            if (cellStyle != null && cellStyle.HasBackColor)
            {
                style.BackColor = cellStyle.BackColor;
            }
            if (cellStyle != null && cellStyle.HasForeColor)
            {
                style.ForeColor = cellStyle.ForeColor;
            }
            if (cellStyle?.Font != null)
            {
                style.Font = cellStyle.Font;
            }

            if (CellFormatting != null)
            {
                var args = new DataGridViewCellFormattingEventArgs(
                    columnIndex, _rows.IndexOf(row), row.GetValue(columnIndex), style);
                CellFormatting(this, args);
            }

            if (style.HasBackColor)
            {
                e.BackgroundColor = style.BackColor;
            }
            if (style.HasForeColor)
            {
                e.ForegroundColor = style.ForeColor;
            }
            if (style.Font != null)
            {
                e.Font = style.Font;
            }
        }

        private void OnGridCellEdited(object sender, GridViewCellEventArgs e)
        {
            if (_rebuildingColumns)
            {
                return;
            }
            CellValueChanged?.Invoke(this, new DataGridViewCellEventArgs(e.Column, e.Row));
        }

        private void OnGridCellClick(object sender, GridCellMouseEventArgs e)
        {
            if (e.Row < 0 || e.Row >= _rows.Count)
            {
                return;
            }

            int column = Math.Max(0, e.Column);
            _currentCell = _rows[e.Row].Cells[column];

            var args = new DataGridViewCellEventArgs(column, e.Row);
            CellClick?.Invoke(this, args);
            CellContentClick?.Invoke(this, args);

            if (e.Buttons == MouseButtons.Alternate)
            {
                CellRightClick?.Invoke(this, args);
            }
        }

        private void OnGridMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Buttons != MouseButtons.Alternate)
            {
                return;
            }

            var hit = HitTest(e.Location);
            if (hit.Type != DataGridViewHitTestType.Cell)
            {
                return;
            }

            _currentCell = _rows[hit.RowIndex].Cells[Math.Max(0, hit.ColumnIndex)];
            _grid.UnselectAll();
            _grid.SelectRow(hit.RowIndex);
            CellRightClick?.Invoke(this, new DataGridViewCellEventArgs(hit.ColumnIndex, hit.RowIndex));
            e.Handled = true;
        }

        private void OnGridMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Buttons == MouseButtons.Alternate)
            {
                e.Handled = true;
            }
        }

        #endregion

        #region Collections

        public class DataGridViewColumnCollection : IEnumerable<DataGridViewColumn>
        {
            private readonly DataGridView _owner;
            private readonly List<DataGridViewColumn> _items = new List<DataGridViewColumn>();
            private int _suspendCount;

            internal DataGridViewColumnCollection(DataGridView owner)
            {
                _owner = owner;
            }

            internal void SuspendNotifications() => _suspendCount++;

            internal void ResumeNotifications() => _suspendCount = Math.Max(0, _suspendCount - 1);

            internal void AddInternal(DataGridViewColumn column)
            {
                column.Owner = _owner;
                _items.Add(column);
            }

            public int Count => _items.Count;

            public DataGridViewColumn this[int index] => _items[index];

            public DataGridViewColumn this[string name] =>
                _items.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException($"Column '{name}' does not exist.", nameof(name));

            public bool Contains(string name) =>
                _items.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

            public int IndexOf(DataGridViewColumn column) => _items.IndexOf(column);

            public int IndexOf(string name) =>
                _items.FindIndex(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

            public int Add(DataGridViewColumn column)
            {
                AddInternal(column);
                if (_suspendCount == 0)
                {
                    _owner.OnColumnsChanged();
                }
                return _items.Count - 1;
            }

            public int Add(string name, string headerText)
            {
                return Add(new DataGridViewTextBoxColumn { Name = name, HeaderText = headerText });
            }

            public void Insert(int index, DataGridViewColumn column)
            {
                column.Owner = _owner;
                index = Math.Max(0, Math.Min(index, _items.Count));
                _items.Insert(index, column);
                if (_suspendCount == 0)
                {
                    _owner.OnColumnInserted(index);
                }
            }

            public void Remove(DataGridViewColumn column)
            {
                int index = _items.IndexOf(column);
                if (index < 0)
                {
                    return;
                }
                RemoveAt(index);
            }

            public void RemoveAt(int index)
            {
                if (index < 0 || index >= _items.Count)
                {
                    return;
                }
                _items[index].Owner = null;
                _items.RemoveAt(index);
                if (_suspendCount == 0)
                {
                    _owner.OnColumnRemoved(index);
                }
            }

            public void Clear()
            {
                foreach (var column in _items)
                {
                    column.Owner = null;
                }
                _items.Clear();
                if (_suspendCount == 0)
                {
                    _owner.OnColumnsChanged();
                }
            }

            public IEnumerator<DataGridViewColumn> GetEnumerator() => _items.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public class DataGridViewRowCollection : IEnumerable<DataGridViewRow>
        {
            private readonly DataGridView _owner;
            private readonly List<DataGridViewRow> _items = new List<DataGridViewRow>();

            internal DataGridViewRowCollection(DataGridView owner)
            {
                _owner = owner;
            }

            internal List<DataGridViewRow> Items => _items;

            internal void ClearInternal() => _items.Clear();

            internal void ReplaceInternal(IEnumerable<DataGridViewRow> rows)
            {
                _items.Clear();
                _items.AddRange(rows);
            }

            public int Count => _items.Count;

            public DataGridViewRow this[int index] => _items[index];

            public int IndexOf(DataGridViewRow row) => _items.IndexOf(row);

            public int Add(params object[] values)
            {
                if (_owner.IsBound)
                {
                    DataTable table = _owner.BoundTable;
                    DataRow dataRow = table.NewRow();
                    int count = Math.Min(values?.Length ?? 0, table.Columns.Count);
                    for (int i = 0; i < count; i++)
                    {
                        dataRow[i] = values[i] ?? (object)DBNull.Value;
                    }
                    table.Rows.Add(dataRow);
                    // The DataView's ListChanged rebuilds our rows.
                    return _items.Count - 1;
                }

                var row = new DataGridViewRow(_owner);
                row.EnsureCapacity(_owner.Columns.Count);
                if (values != null)
                {
                    for (int i = 0; i < values.Length && i < _owner.Columns.Count; i++)
                    {
                        row.SetValue(i, values[i]);
                    }
                }
                _items.Add(row);
                _owner.NotifyRowsChanged();
                return _items.Count - 1;
            }

            public void RemoveAt(int index)
            {
                if (index < 0 || index >= _items.Count)
                {
                    return;
                }

                if (_items[index].DataBoundItem is DataRowView view)
                {
                    view.Row.Delete();
                    _owner.BoundTable?.AcceptChanges();
                    return;
                }

                _items.RemoveAt(index);
                _owner.NotifyRowsChanged();
            }

            public void Remove(DataGridViewRow row)
            {
                RemoveAt(_items.IndexOf(row));
            }

            public void Clear()
            {
                if (_owner.IsBound)
                {
                    _owner.BoundTable?.Rows.Clear();
                    return;
                }
                _items.Clear();
                _owner.NotifyRowsChanged();
            }

            public IEnumerator<DataGridViewRow> GetEnumerator() => _items.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        #endregion
    }

    /// <summary>Accepted for source compatibility; Eto manages row height on the grid itself.</summary>
    public class DataGridViewRowTemplate
    {
        public int Height { get; set; } = 22;
    }
}
