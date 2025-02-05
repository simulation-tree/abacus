using Collections;
using System;
using System.Diagnostics;
using Unmanaged;

namespace Abacus.Manager
{
    public readonly struct Arguments : IDisposable
    {
        private readonly Text text;
        private readonly Array<URange> arguments;

        public readonly uint Count => arguments.Length;
        public readonly bool IsEmpty => text.IsEmpty;
        public readonly USpan<char> RawText => text.AsSpan();

        public readonly USpan<char> this[uint index]
        {
            get
            {
                ThrowIfOutOfRange(index);

                return text.AsSpan().Slice(arguments[index]);
            }
        }

        public Arguments(USpan<char> arguments)
        {
            uint start = 0;
            uint index = 0;
            bool insideQuotes = false;
            USpan<URange> argumentsBuffer = stackalloc URange[64];
            uint argumentCount = 0;
            while (arguments.Length > 0)
            {
                bool atEnd = index == arguments.Length - 1;
                if (atEnd)
                {
                    URange range;
                    if (insideQuotes)
                    {
                        range = new(start + 1, index);
                    }
                    else
                    {
                        range = new(start, index + 1);
                    }

                    USpan<char> argument = arguments.Slice(range);
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
                            URange range = new(start + 1, index);
                            USpan<char> argument = arguments.Slice(range);
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
                            URange range = new(start, index);
                            USpan<char> argument = arguments.Slice(range);
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

        public readonly bool Contains(USpan<char> argument)
        {
            for (uint i = 0; i < Count; i++)
            {
                Unmanaged.URange range = arguments[i];
                USpan<char> current = text.AsSpan().Slice(range);
                if (current.SequenceEqual(argument))
                {
                    return true;
                }
            }

            return false;
        }

        public readonly bool Contains(string argument)
        {
            return Contains(argument.AsSpan());
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfOutOfRange(uint index)
        {
            if (index >= Count)
            {
                throw new($"Index {index} is out of range");
            }
        }
    }
}