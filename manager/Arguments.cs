using Collections.Generic;
using System;
using System.Diagnostics;
using Unmanaged;

namespace Abacus.Manager
{
    public readonly struct Arguments : IDisposable
    {
        private readonly Text text;
        private readonly Array<Range> arguments;

        public readonly int Count => arguments.Length;
        public readonly bool IsEmpty => text.IsEmpty;
        public readonly ReadOnlySpan<char> RawText => text.AsSpan();

        public readonly ReadOnlySpan<char> this[int index]
        {
            get
            {
                ThrowIfOutOfRange(index);

                return text.AsSpan()[arguments[index]];
            }
        }

        public Arguments(ReadOnlySpan<char> arguments)
        {
            int start = 0;
            int index = 0;
            bool insideQuotes = false;
            Span<Range> argumentsBuffer = stackalloc Range[64];
            int argumentCount = 0;
            while (arguments.Length > 0)
            {
                bool atEnd = index == arguments.Length - 1;
                if (atEnd)
                {
                    Range range;
                    if (insideQuotes)
                    {
                        range = new(start + 1, index);
                    }
                    else
                    {
                        range = new(start, index + 1);
                    }

                    ReadOnlySpan<char> argument = arguments[range];
                    argumentsBuffer[argumentCount++] = range;
                    break;
                }
                else
                {
                    char c = arguments[index];
                    if (c == '"')
                    {
                        insideQuotes = !insideQuotes;
                        if (!insideQuotes)
                        {
                            Range range = new(start + 1, index);
                            ReadOnlySpan<char> argument = arguments[range];
                            argumentsBuffer[argumentCount++] = range;
                        }

                        start = index;
                    }
                    else if (c == '\\')
                    {
                        //skip next char if its a quote or another backslash
                        if (index + 1 < arguments.Length)
                        {
                            char next = arguments[index + 1];
                            if (next == '"' || next == '\\')
                            {
                                index++;
                            }
                        }
                    }
                    else if (c == ' ')
                    {
                        if (!insideQuotes)
                        {
                            Range range = new(start, index);
                            ReadOnlySpan<char> argument = arguments[range];
                            argumentsBuffer[argumentCount++] = range;
                            start = index + 1;
                        }
                    }

                    index++;
                }
            }

            this.text = new(arguments);
            this.arguments = new(argumentsBuffer.Slice(0, argumentCount));
        }

        public readonly override string ToString()
        {
            return text.ToString();
        }

        public readonly void Dispose()
        {
            text.Dispose();
            arguments.Dispose();
        }

        public readonly bool Contains(ReadOnlySpan<char> argument)
        {
            for (int i = 0; i < Count; i++)
            {
                Range range = arguments[i];
                ReadOnlySpan<char> current = text.AsSpan()[range];
                if (current.SequenceEqual(argument))
                {
                    return true;
                }
            }

            return false;
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfOutOfRange(int index)
        {
            if (index >= Count)
            {
                throw new($"Index {index} is out of range");
            }
        }
    }
}