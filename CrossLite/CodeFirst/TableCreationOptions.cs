using System;

namespace CrossLite.CodeFirst
{
    [Flags]
    public enum TableCreationOptions
    {
        None = 0x0000,
        IfNotExists = 0x0010,
        Temporary = 0x0020
    }
}
