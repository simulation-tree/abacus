using System;
using Unmanaged;

namespace Abacus.Manager
{
    public readonly struct LogMessage
    {
        public readonly Category category;
        public readonly Range range;
        public readonly bool appendLine;

        private readonly Text text;

        public readonly ReadOnlySpan<char> Message => text.AsSpan()[range];

        public LogMessage(Text text, Category category, Range range, bool appendLine)
        {
            this.category = category;
            this.range = range;
            this.text = text;
            this.appendLine = appendLine;
        }

        public override string ToString()
        {
            return $"[{category}] {Message.ToString()}";
        }

        public enum Category : byte
        {
            Info,
            Error
        }
    }
}