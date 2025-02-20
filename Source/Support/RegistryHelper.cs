using System;
using System.Reflection;
using Microsoft.Win32;

namespace UI_Demo;

public static class RegistryHelper
{
    static readonly string ExePath = Environment.ProcessPath!;
    const string RegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>
    ///   Controls whether the application should run on startup via the Win32 registry key.
    /// </summary>
    /// <remarks>
    ///   Registry key => "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run"
    /// </remarks>
    /// <param name="value"><c>true</c> to run the current assembly on startup, <c>false</c> to remove the key and not run on startup.</param>
    /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
    public static bool RunOnStartup(bool value) => SetRegistryRunOnStartup(value);

    static bool SetRegistryRunOnStartup(bool value, bool repeat = true)
    {
        if (!OperatingSystem.IsWindows()) { return false; }

        try
        {
            var rk = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
            if (rk == null) { return false; }

            if (value)
                rk.SetValue(App.GetCurrentAssemblyName() ?? "WinUI3", ExePath);
            else
                rk.DeleteValue(App.GetCurrentAssemblyName() ?? "WinUI3");
            
            return true;
        }
        catch (Exception ex)
        {
            App.DebugLog($"{MethodBase.GetCurrentMethod()?.Name}: {ex.Message}");
            return repeat && SetRegistryRunOnStartup(value, false);
        }
    }
}
