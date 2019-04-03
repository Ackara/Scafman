using System;

namespace Acklann.Powerbar
{
    [Flags]
    public enum Switch
    {
        None = 0,
        AddFile = 1,
        RunCommand = 2,
        PipeContext = 4,
        RunCommandInWindow = 8,
        Force = 16
    }
}