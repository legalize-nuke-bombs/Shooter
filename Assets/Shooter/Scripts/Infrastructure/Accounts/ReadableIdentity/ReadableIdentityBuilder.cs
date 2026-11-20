using System;
using System.Collections.Generic;
using System.Text;

namespace Shooter.Infrastructure;

public sealed class ReadableIdentityBuilder
{
    public static ReadableIdentityBuilder Instance { get; } = new();

    public string Build(ReadOnlySpan<byte> bytes)
    {
        StringBuilder sb = new StringBuilder();
        int iterator = 0;
        foreach (ReadableIdentityVirtualDict dict in dicts)
        {
            dict.NextWord(bytes, iterator, out string word, out int next);
            iterator = next;
            if (word == null)
            {
                throw new ArgumentException("Byte stream is too short");
            }
            sb.Append(word);
            sb.Append(' ');
        }
        return sb.ToString();
    }

    private readonly List<ReadableIdentityVirtualDict> dicts = new List<ReadableIdentityVirtualDict>()
    {
        new ReadableIdentityMemoryColorsDict(),
        new ReadableIdentityMemoryAdjectivesDict(),
        new ReadableIdentityMemoryAnimalsDict(),
        new ReadableIdentityVirtualBase32Dict(6)
    };

    private ReadableIdentityBuilder()
    {
    }
}
