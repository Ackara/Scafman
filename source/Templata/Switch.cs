﻿using System;

namespace Acklann.Templata
{
    [Flags]
    public enum Switch
    {
        None = 0,
        AddFile = 1,
        AddFolder = 2,
        Rename = 4,
        NugetPackage = 8,
        NPMPackage = 16
    }
}