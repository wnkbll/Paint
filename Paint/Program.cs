﻿using System;
using System.Windows.Forms;

namespace Paint;

internal static class Program {
    [STAThread]
    private static void Main() {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new UiMainWindow());
    }
}
