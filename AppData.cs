﻿using System;
using System.IO;

namespace PFSoftware.Finances
{
    public static class AppData
    {
        internal static string Location = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PF Software", "Finances");
    }
}