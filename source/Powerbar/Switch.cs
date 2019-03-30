using System;

namespace Acklann.Powerbar
{
    [Flags]
    public enum Switch
    {
        None = 0,
        PipeContext = 1,
        CreateWindow = 2,
        CreateNewFile = 4
    }
}