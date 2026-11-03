using System.Diagnostics;
using System.Runtime.InteropServices;
using Nox.Core.Abstractions;
using static Nox.Windows.Shell.NativeMethods;

namespace Nox.Windows.Memory;

/// <summary>
/// RAM del PC (<c>GlobalMemoryStatusEx</c>) y «Liberar RAM» (<c>EmptyWorkingSet</c>): pide a cada app
/// que devuelva a Windows la memoria que tiene cargada. No cierra nada ni borra datos; lo que una app
/// vuelva a necesitar, Windows se lo carga otra vez. Sin permisos de administrador solo llega a las
/// apps del propio usuario; las del sistema se saltan.
/// </summary>
public sealed class SystemMemory : ISystemMemory
{
    public MemoryStatus GetStatus()
    {
        var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        return GlobalMemoryStatusEx(ref status)
            ? new MemoryStatus((long)status.ullTotalPhys, (long)status.ullAvailPhys)
            : default;
    }

    public int TrimWorkingSets()
    {
        int trimmed = 0;
        foreach (Process process in Process.GetProcesses())
        {
            using (process)
            {
                nint handle = OpenProcess(PROCESS_SET_QUOTA | PROCESS_QUERY_LIMITED_INFORMATION, false, (uint)process.Id);
                if (handle == 0)
                    continue; // sin acceso (del sistema o de otro usuario)
                try
                {
                    if (EmptyWorkingSet(handle))
                        trimmed++;
                }
                finally
                {
                    CloseHandle(handle);
                }
            }
        }
        return trimmed;
    }
}
