using System;

namespace Acklann.Powerbar
{
    [Flags]
    public enum ShellOptions
    {
        None = 0,
        PipeContext = 1,
        CreateWindow = 2,
        CreateNewFile = 4
    }
}