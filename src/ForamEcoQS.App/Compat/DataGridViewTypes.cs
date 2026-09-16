//MIT License
// DataGridViewTypes.cs - Supporting types for the Eto.Forms grid compatibility control.
//
// The original application drove a WinForms DataGridView directly: it added and removed
// columns at runtime, wrote cell values, coloured whole rows and attached per-cell tooltips.
// Re-expressing all of that as Eto data bindings would have meant rewriting several thousand
// lines of working, peer-reviewed analysis code. Instead the grid keeps the same object model
// (rows, columns, cells, styles) and renders it through an Eto GridView.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Eto.Drawing;

namespace ForamEcoQS.Compat
{
    public enum DataGridViewSelectionMode
    {
        CellSelect,
        FullRowSelect,
        FullColumnSelect,
        RowHeaderSelect,
        ColumnHeaderSelect
    }

    public enum DataGridViewColumnSortMode
    {
        NotSortable,
        Automatic,
        Programmatic
    }

    public enum DataGridViewAutoSizeColumnsMode
    {
        None,
        ColumnHeader,
        AllCells,
        DisplayedCells,
        Fill
    }

    public enum DataGridViewContentAlignment
    {
        NotSet,
        MiddleLeft,
        MiddleCenter,
        MiddleRight
    }

    public enum DataGridViewEditMode
    {
        EditOnEnter,
        EditOnKeystroke,
        EditOnKeystrokeOrF2,
        EditOnF2,
        EditProgrammatically
    }

    public enum DataGridViewHitTestType
    {
        None,
        Cell,
        ColumnHeader,
        RowHeader,
        TopLeftHeader,
        HorizontalScrollBar,
        VerticalScrollBar
    }

    /// <summary>
    /// Mirrors the small slice of <c>DataGridViewCellStyle</c> the application actually uses.
    /// An unset colour is <see cref="Eto.Drawing.Colors.Transparent"/> so "no colour" and
    /// "explicitly transparent" stay distinguishable from a real colour.
    /// </summary>
    public class DataGridViewCellStyle
    {
        public Color BackColor { get; set; } = Color.FromArgb(0, 0, 0, 0);
        public Color ForeColor { get; set; } = Color.FromArgb(0, 0, 0, 0);
        public string Format { get; set; }
        public DataGridViewContentAlignment Alignment { get; set; } = DataGridViewContentAlignment.NotSet;
        public Font Font { get; set; }

        public bool HasBackColor => BackColor.Ab != 0;
        public bool HasForeColor => ForeColor.Ab != 0;

        public DataGridViewCellStyle Clone()
        {
            return new DataGridViewCellStyle
            {
                BackColor = BackColor,
                ForeColor = ForeColor,
                Format = Format,
                Alignment = Alignment,
                Font = Font
            };
        }
    }

    public class DataGridViewCell
    {
        private DataGridViewCellStyle _style;

        internal DataGridViewCell(DataGridViewRow row, int columnIndex)
        {
            OwningRow = row;
            ColumnIndex = columnIndex;
        }

        public DataGridViewRow OwningRow { get; }

        public int ColumnIndex { get; internal set; }

        public int RowIndex => OwningRow?.Index ?? -1;

        public string ToolTipText { get; set; } = string.Empty;

        /// <summary>Lazily created so untouched cells cost nothing.</summary>
        public DataGridViewCellStyle Style => _style ??= new DataGridViewCellStyle();

        internal DataGridViewCellStyle StyleOrNull => _style;

        public object Value
        {
            get => OwningRow?.GetValue(ColumnIndex);
            set => OwningRow?.SetValue(ColumnIndex, value);
        }

        public object FormattedValue => DataGridViewFormatting.Format(Value, OwningRow?.Grid?.ColumnAt(ColumnIndex));
    }

    public class DataGridViewCellCollection : IEnumerable<DataGridViewCell>
    {
        private readonly DataGridViewRow _row;
        private readonly List<DataGridViewCell> _cells = new List<DataGridViewCell>();

        internal DataGridViewCellCollection(DataGridViewRow row)
        {
            _row = row;
        }

        internal void EnsureCount(int count)
        {
            while (_cells.Count < count)
            {
                _cells.Add(new DataGridViewCell(_row, _cells.Count));
            }
            while (_cells.Count > count)
            {
                _cells.RemoveAt(_cells.Count - 1);
            }
            for (int i = 0; i < _cells.Count; i++)
            {
                _cells[i].ColumnIndex = i;
            }
        }

        internal void InsertAt(int index)
        {
            _cells.Insert(index, new DataGridViewCell(_row, index));
            for (int i = 0; i < _cells.Count; i++)
            {
                _cells[i].ColumnIndex = i;
            }
        }

        internal void RemoveAt(int index)
        {
            if (index < 0 || index >= _cells.Count)
            {
                return;
            }
            _cells.RemoveAt(index);
            for (int i = 0; i < _cells.Count; i++)
            {
                _cells[i].ColumnIndex = i;
            }
        }

        public int Count => _cells.Count;

        public DataGridViewCell this[int index]
        {
            get
            {
                EnsureCount(Math.Max(_cells.Count, index + 1));
                return _cells[index];
            }
        }

        public DataGridViewCell this[string columnName]
        {
            get
            {
                int index = _row?.Grid?.Columns.IndexOf(columnName) ?? -1;
                if (index < 0)
                {
                    throw new ArgumentException($"Column '{columnName}' does not exist.", nameof(columnName));
                }
                return this[index];
            }
        }

        public IEnumerator<DataGridViewCell> GetEnumerator() => _cells.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class DataGridViewRow
    {
        private readonly List<object> _values = new List<object>();
        private DataGridViewCellStyle _defaultCellStyle;

        internal DataGridViewRow(DataGridView grid)
        {
            Grid = grid;
            Cells = new DataGridViewCellCollection(this);
        }

        internal DataGridView Grid { get; set; }

        /// <summary>The bound <see cref="DataRowView"/> when the grid has a DataTable source.</summary>
        public object DataBoundItem { get; internal set; }

        public DataGridViewCellCollection Cells { get; }

        public DataGridViewCellStyle DefaultCellStyle => _defaultCellStyle ??= new DataGridViewCellStyle();

        internal DataGridViewCellStyle DefaultCellStyleOrNull => _defaultCellStyle;

        public bool Selected
        {
            get => Grid != null && Grid.IsRowSelected(this);
            set => Grid?.SetRowSelected(this, value);
        }

        /// <summary>
        /// Always false: the compatibility grid never shows the WinForms "new row" placeholder,
        /// but the original code guards against it in several loops.
        /// </summary>
        public bool IsNewRow => false;

        public int Index => Grid?.Rows.IndexOf(this) ?? -1;

        internal void EnsureCapacity(int count)
        {
            while (_values.Count < count)
            {
                _values.Add(null);
            }
            Cells.EnsureCount(count);
        }

        internal void InsertValueAt(int index, object value)
        {
            EnsureCapacity(Math.Max(_values.Count, index));
            _values.Insert(index, value);
            Cells.InsertAt(index);
        }

        internal void RemoveValueAt(int index)
        {
            if (index >= 0 && index < _values.Count)
            {
                _values.RemoveAt(index);
            }
            Cells.RemoveAt(index);
        }

        internal object GetValue(int columnIndex)
        {
            // A bound row still keeps local storage for columns the application added at
            // runtime (e.g. a new sample), which have no counterpart in the DataTable.
            if (DataBoundItem is DataRowView view)
            {
                string property = Grid?.ColumnAt(columnIndex)?.DataPropertyName;
                if (!string.IsNullOrEmpty(property) && view.Row.Table.Columns.Contains(property))
                {
                    object raw = view[property];
                    return raw == DBNull.Value ? null : raw;
                }
            }

            return columnIndex >= 0 && columnIndex < _values.Count ? _values[columnIndex] : null;
        }

        internal void SetValue(int columnIndex, object value)
        {
            if (DataBoundItem is DataRowView view)
            {
                string property = Grid?.ColumnAt(columnIndex)?.DataPropertyName;
                if (!string.IsNullOrEmpty(property) && view.Row.Table.Columns.Contains(property))
                {
                    try
                    {
                        view[property] = ConvertForColumn(value, view.Row.Table.Columns[property].DataType);
                    }
                    catch (FormatException)
                    {
                        // Keep the previous value when the user types something the column cannot hold.
                    }
                    catch (InvalidCastException)
                    {
                    }
                    return;
                }
            }

            EnsureCapacity(columnIndex + 1);
            _values[columnIndex] = value;
        }

        private static object ConvertForColumn(object value, Type targetType)
        {
            if (value == null)
            {
                return DBNull.Value;
            }
            if (targetType == typeof(string))
            {
                return value.ToString();
            }
            if (value is string s && string.IsNullOrWhiteSpace(s))
            {
                return DBNull.Value;
            }
            if (targetType.IsInstanceOfType(value))
            {
                return value;
            }
            return Convert.ChangeType(value, targetType, System.Globalization.CultureInfo.CurrentCulture);
        }
    }

    public class DataGridViewColumn
    {
        public string Name { get; set; } = string.Empty;
        public string HeaderText { get; set; } = string.Empty;
        public string DataPropertyName { get; set; }
        public Type ValueType { get; set; }
        public int Width { get; set; } = 100;
        public bool ReadOnly { get; set; }
        public bool Visible { get; set; } = true;
        public DataGridViewColumnSortMode SortMode { get; set; } = DataGridViewColumnSortMode.NotSortable;
        public DataGridViewCellStyle DefaultCellStyle { get; } = new DataGridViewCellStyle();

        /// <summary>Accepted for source compatibility; Eto sizes columns itself.</summary>
        public float FillWeight { get; set; } = 100f;

        internal DataGridView Owner { get; set; }

        public int Index => Owner?.Columns.IndexOf(this) ?? -1;
    }

    public class DataGridViewTextBoxColumn : DataGridViewColumn
    {
    }

    public class DataGridViewComboBoxColumn : DataGridViewColumn
    {
        public ComboBoxItemCollection Items { get; } = new ComboBoxItemCollection();

        /// <summary>Accepted for source compatibility with the WinForms original.</summary>
        public object FlatStyle { get; set; }
    }

    public class DataGridViewLinkColumn : DataGridViewColumn
    {
        public object LinkBehavior { get; set; }
    }

    public class ComboBoxItemCollection : List<object>
    {
        public void AddRange(params object[] items) => base.AddRange(items);
    }

    public class DataGridViewCellEventArgs : EventArgs
    {
        public DataGridViewCellEventArgs(int columnIndex, int rowIndex)
        {
            ColumnIndex = columnIndex;
            RowIndex = rowIndex;
        }

        public int ColumnIndex { get; }
        public int RowIndex { get; }
    }

    public class DataGridViewCellFormattingEventArgs : EventArgs
    {
        public DataGridViewCellFormattingEventArgs(int columnIndex, int rowIndex, object value, DataGridViewCellStyle cellStyle)
        {
            ColumnIndex = columnIndex;
            RowIndex = rowIndex;
            Value = value;
            CellStyle = cellStyle;
        }

        public int ColumnIndex { get; }
        public int RowIndex { get; }
        public object Value { get; set; }
        public DataGridViewCellStyle CellStyle { get; }
        public bool FormattingApplied { get; set; }
    }

    public class DataGridViewHitTestInfo
    {
        public static readonly DataGridViewHitTestInfo Nowhere =
            new DataGridViewHitTestInfo(DataGridViewHitTestType.None, -1, -1);

        public DataGridViewHitTestInfo(DataGridViewHitTestType type, int columnIndex, int rowIndex)
        {
            Type = type;
            ColumnIndex = columnIndex;
            RowIndex = rowIndex;
        }

        public DataGridViewHitTestType Type { get; }
        public int ColumnIndex { get; }
        public int RowIndex { get; }
    }

    internal static class DataGridViewFormatting
    {
        /// <summary>
        /// Applies the column's .NET format string, matching how the WinForms grid rendered
        /// "N2" / "N4" numeric columns.
        /// </summary>
        public static string Format(object value, DataGridViewColumn column)
        {
            if (value == null || value == DBNull.Value)
            {
                return string.Empty;
            }

            string format = column?.DefaultCellStyle?.Format;
            if (!string.IsNullOrEmpty(format) && value is IFormattable formattable)
            {
                try
                {
                    return formattable.ToString(format, System.Globalization.CultureInfo.CurrentCulture);
                }
                catch (FormatException)
                {
                    return value.ToString();
                }
            }

            return value.ToString();
        }
    }
}
