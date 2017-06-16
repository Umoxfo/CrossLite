using System;

namespace CrossLite
{
    [Flags]
    public enum IndexCreationOptions
    {
        None = 0x0000,
        IfNotExists = 0x0010,
        Unique = 0x0020
    }
}
