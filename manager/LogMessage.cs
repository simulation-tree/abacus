using Unmanaged;

namespace Abacus.Manager
{
    public readonly struct LogMessage
    {
        public readonly Category category;
        public readonly URange range;
        public readonly bool appendLine;

        private readonly Text text;

        public readonly USpan<char> Message => text.AsSpan().Slice(range);

        public LogMessage(Text text, Category category, URange range, bool appendLine)
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