using Collections;
using System;
using System.Diagnostics;
using Unmanaged;

namespace Abacus.Manager
{
    public readonly struct TableBuilder : IDisposable
    {
        private readonly List<Row> rows;

        public readonly USpan<Row> Rows => rows.AsSpan();

        [Obsolete("Default constructor not supported", true)]
        public TableBuilder()
        {
            throw new NotSupportedException();
        }

        public TableBuilder(string first)
        {
            rows = new();
            AddRow(first);
        }

        public TableBuilder(string first, string second)
        {
            rows = new();
            AddRow(first, second);
        }

        public TableBuilder(string first, string second, string third)
        {
            rows = new();
            AddRow(first, second, third);
        }

        public TableBuilder(string first, string second, string third, string fourth)
        {
            rows = new();
            AddRow(first, second, third, fourth);
        }

        public readonly void Dispose()
        {
            foreach (Row row in rows)
            {
                row.Dispose();
            }

            rows.Dispose();
        }

        public readonly override string ToString()
        {
            const string Separator = " | ";
            Row header = rows[0];
            using Array<uint> columnLengths = new(header.Columns);
            for (uint y = 0; y < rows.Count; y++)
            {
                Row row = rows[y];
                for (uint x = 0; x < row.Columns; x++)
                {
                    USpan<char> column = row[x];
                    ref uint columnLength = ref columnLengths[x];
                    if (column.Length > columnLength)
                    {
                        columnLength = column.Length;
                    }
                }
            }

            using Text builder = new();
            for (uint x = 0; x < header.Columns; x++)
            {
                USpan<char> column = header[x];
                uint columnLength = columnLengths[x];
                uint remainingLength = columnLength - column.Length;
                builder.Append(column);
                builder.Append(' ', remainingLength);
                builder.Append(Separator);
            }

            builder.SetLength(builder.Length - (uint)Separator.Length);
            uint headerLength = builder.Length;
            builder.Append('\n');
            builder.Append('-', headerLength);
            builder.Append('\n');
            if (Rows.Length > 1)
            {
                for (uint y = 1; y < rows.Count; y++)
                {
                    Row row = rows[y];
                    for (uint x = 0; x < row.Columns; x++)
                    {
                        USpan<char> column = row[x];
                        uint columnLength = columnLengths[x];
                        uint remainingLength = columnLength - column.Length;
                        builder.Append(column);
                        builder.Append(' ', remainingLength);
                        builder.Append(Separator);
                    }

                    builder.SetLength(builder.Length - (uint)Separator.Length);
                    builder.Append('\n');
                }

                builder.SetLength(builder.Length - 1);
            }
            else
            {
                builder.Append("Empty");
            }

            return builder.ToString();
        }

        public readonly void AddRow(string first)
        {
            ThrowIfRowLengthMismatch(1);

            Row row = new();
            row.Add(first);
            rows.Add(row);
        }

        public readonly void AddRow(string first, string second)
        {
            ThrowIfRowLengthMismatch(2);

            Row row = new();
            row.Add(first);
            row.Add(second);
            rows.Add(row);
        }

        public readonly void AddRow(string first, string second, string third)
        {
            ThrowIfRowLengthMismatch(3);

            Row row = new();
            row.Add(first);
            row.Add(second);
            row.Add(third);
            rows.Add(row);
        }

        public readonly void AddRow(string first, string second, string third, string fourth)
        {
            ThrowIfRowLengthMismatch(4);

            Row row = new();
            row.Add(first);
            row.Add(second);
            row.Add(third);
            row.Add(fourth);
            rows.Add(row);
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfRowLengthMismatch(uint length)
        {
            if (Rows.Length == 0)
            {
                return;
            }

            Row header = Rows[0];
            if (header.Columns != length)
            {
                throw new InvalidOperationException("Row length mismatch");
            }
        }

        public readonly struct Row : IDisposable
        {
            private readonly List<Text> row;

            public readonly uint Columns => row.Count;

            public readonly uint Length
            {
                get
                {
                    uint length = 0;
                    foreach (Text cell in row)
                    {
                        length += cell.Length;
                    }

                    return length;
                }
            }

            public readonly USpan<char> this[uint index] => row[index].AsSpan();

            public Row()
            {
                row = new();
            }

            public readonly void Dispose()
            {
                foreach (Text cell in row)
                {
                    cell.Dispose();
                }

                row.Dispose();
            }

            public readonly void Add(USpan<char> cell)
            {
                row.Add(new(cell));
            }

            public readonly void Add(string cell)
            {
                row.Add(new(cell));
            }
        }
    }
}