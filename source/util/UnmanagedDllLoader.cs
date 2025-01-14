using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Maestro;

public static class UnmanagedDllLoader
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LoadLibrary(string dllToLoad);

    /// <summary>
    /// Loads an unmanaged DLL from embedded resources.
    /// </summary>
    /// <param name="namespace">The namespace where the embedded DLL is located.</param>
    /// <param name="dllNameBase">The base name of the DLL (without architecture suffix or extension).</param>
    public static void LoadUnmanagedDll(string @namespace, string dllName)
    {
        // Build the full resource name
        string resourceName = $"{@namespace}.{dllName}";

        // Temporary path for the extracted DLL
        string tempPath = Path.Combine(Path.GetTempPath(), dllName);

        // Extract the embedded resource
        using (Stream resourceStream = Assembly.GetExecutingAssembly()
                                                .GetManifestResourceStream(resourceName))
        {
            if (resourceStream == null)
            {
                throw new Exception($"Embedded DLL '{dllName}' not found in namespace '{@namespace}'.");
            }

            using (FileStream fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
            {
                resourceStream.CopyTo(fileStream);
            }
        }

        // Load the unmanaged DLL
        IntPtr handle = LoadLibrary(tempPath);
        if (handle == IntPtr.Zero)
        {
            throw new Exception($"Failed to load unmanaged DLL '{dllName}' from namespace '{@namespace}'.");
        }

        Logger.Verbose($"Unmanaged DLL '{dllName}' loaded successfully.");
    }
}
