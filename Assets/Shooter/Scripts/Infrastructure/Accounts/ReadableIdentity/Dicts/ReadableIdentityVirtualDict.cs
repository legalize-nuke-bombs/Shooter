using System;

namespace Shooter.Infrastructure;

public abstract class ReadableIdentityVirtualDict
{
    public abstract void NextWord(ReadOnlySpan<byte> bytes, int begin, out string word, out int next);
}
