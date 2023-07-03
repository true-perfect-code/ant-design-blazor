﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AntDesign
{
    internal class ColumnContext
    {
        public IReadOnlyList<IColumn> Columns => _columns.Where(x => !x.Hidden).ToList();

        private IList<IColumn> _columns = new List<IColumn>();

        private int _currentColIndex;

        private int[] _colIndexOccupied;

        private ITable _table;

        private bool _collectingColumns;

        public ColumnContext(ITable table)
        {
            _table = table;
        }

        public void AddColumn(IColumn column)
        {
            if (column == null)
            {
                return;
            }

            column.Table = _table;
            column.ColIndex = _currentColIndex++;
            _columns.Add(column);
        }

        public void RemoveColumn(IColumn column)
        {
            _columns.Remove(column);
        }

        public void AddHeaderColumn(IColumn column)
        {
            if (column == null)
            {
                return;
            }

            var columnSpan = column.HeaderColSpan;
            if (column.RowSpan == 0) columnSpan = 0;

            do
            {
                if (++_currentColIndex >= _columns.Count)
                {
                    _currentColIndex = 0;
                    if (_colIndexOccupied != null)
                    {
                        foreach (ref var item in _colIndexOccupied.AsSpan())
                        {
                            if (item > 0) item--;
                        }
                    }
                }
            }
            while (_colIndexOccupied != null && _colIndexOccupied[_currentColIndex] > 0);

            column.ColIndex = _currentColIndex;
            _currentColIndex += columnSpan - 1;

            if (column.RowSpan > 1)
            {
                _colIndexOccupied ??= new int[_columns.Count];
                for (var i = column.ColIndex; i <= _currentColIndex; i++)
                {
                    _colIndexOccupied[i] = column.RowSpan;
                }
            }
        }

        public void AddRowColumn(IColumn column)
        {
            if (column == null)
            {
                return;
            }

            var columnSpan = column.ColSpan;
            if (column.RowSpan == 0) columnSpan = 0;

            do
            {
                if (++_currentColIndex >= _columns.Count)
                {
                    _currentColIndex = 0;
                    if (_colIndexOccupied != null)
                    {
                        foreach (ref var item in _colIndexOccupied.AsSpan())
                        {
                            if (item > 0) item--;
                        }
                    }
                }
            }
            while (_colIndexOccupied != null && _colIndexOccupied[_currentColIndex] > 0);

            column.ColIndex = _currentColIndex;
            _currentColIndex += columnSpan - 1;

            if (column.RowSpan > 1)
            {
                _colIndexOccupied ??= new int[_columns.Count];
                for (var i = column.ColIndex; i <= _currentColIndex; i++)
                {
                    _colIndexOccupied[i] = column.RowSpan;
                }
            }
        }

        internal void StartCollectingColumns()
        {
            if (!_collectingColumns)
            {
                _collectingColumns = true;
                _columns.Clear();
            }
        }

        internal void HeaderColumnInitialed()
        {
            if (_table.ScrollX != null && _columns.Any(x => x.Width == null))
            {
                var zeroWidthCols = _columns.Where(x => x.Width == null).ToArray();
                var totalWidth = string.Join(" + ", _columns.Where(x => x.Width != null).Select(x => (CssSizeLength)x.Width));
                foreach (var col in zeroWidthCols)
                {
                    col.Width = $"calc(({(CssSizeLength)_table.ScrollX} - ({totalWidth}) + 3px) / {zeroWidthCols.Length})";
                }
            }

            _collectingColumns = false;
            // Header columns have all been initialized, then we can invoke the first change.
            _table.OnColumnInitialized();
        }
    }
}
