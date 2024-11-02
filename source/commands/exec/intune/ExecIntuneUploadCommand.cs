using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maestro
{
    public class ExecIntuneUploadCommand
    {
        public static async Task Execute(CommandLineOptions options, LiteDBHandler database)
        {
            // Validate device ID
            if (string.IsNullOrEmpty(options.Device))
            {
                Logger.Error("Device ID is required.");
                return;
            }

            // Validate URL
            if (!Uri.IsWellFormedUriString(options.Url, UriKind.Absolute))
            {
                Logger.Error("Invalid URL.");
                return;
            }

            // Validate optional parameters
            if (options.RegistryKeys != null)
            {
                foreach (var key in options.RegistryKeys)
                {
                    if (!key.StartsWith("HKLM\\") && !key.StartsWith("HKCR\\"))
                    {
                        Logger.Error("RegistryKey must start with HKLM\\ or HKCR\\.");
                        return;
                    }
                }
            }

            if (options.Events != null)
            {
                foreach (var eventLog in options.Events)
                {
                    if (string.IsNullOrEmpty(eventLog))
                    {
                        Logger.Error("Event log name cannot be empty.");
                        return;
                    }
                }
            }

            if (options.Commands != null)
            {
                var allowedCommands = new HashSet<string>
                {
                    "%windir%\\system32\\certutil.exe",
                    "%windir%\\system32\\dxdiag.exe",
                    "%windir%\\system32\\gpresult.exe",
                    "%windir%\\system32\\msinfo32.exe",
                    "%windir%\\system32\\netsh.exe",
                    "%windir%\\system32\\nltest.exe",
                    "%windir%\\system32\\ping.exe",
                    "%windir%\\system32\\powercfg.exe",
                    "%windir%\\system32\\w32tm.exe",
                    "%windir%\\system32\\wpr.exe",
                    "%windir%\\system32\\dsregcmd.exe",
                    "%windir%\\system32\\dispdiag.exe",
                    "%windir%\\system32\\ipconfig.exe",
                    "%windir%\\system32\\logman.exe",
                    "%windir%\\system32\\tracelog.exe",
                    "%programfiles%\\windows defender\\mpcmdrun.exe",
                    "%windir%\\system32\\MdmDiagnosticsTool.exe",
                    "%windir%\\system32\\pnputil.exe"
                };

                foreach (var command in options.Commands)
                {
                    bool isAllowed = false;
                    foreach (var allowedCommand in allowedCommands)
                    {
                        if (command.StartsWith(allowedCommand, StringComparison.OrdinalIgnoreCase))
                        {
                            isAllowed = true;
                            break;
                        }
                    }

                    if (!isAllowed)
                    {
                        Logger.Error("Command is not allowed. A list of allowed commands is available here: https://learn.microsoft.com/en-us/windows/client-management/mdm/diagnosticlog-csp#diagnosticarchivearchivedefinition");
                        return;
                    }
                }
            }

            if (options.FolderFiles != null)
            {
                var allowedRoots = new HashSet<string>
                {
                    "%PROGRAMFILES%",
                    "%PROGRAMDATA%",
                    "%PUBLIC%",
                    "%WINDIR%",
                    "%TEMP%",
                    "%TMP%"
                };

                var allowedExtensions = new HashSet<string>
                {
                    ".log",
                    ".txt",
                    ".dmp",
                    ".cab",
                    ".zip",
                    ".xml",
                    ".html",
                    ".evtx",
                    ".etl"
                };

                foreach (var file in options.FolderFiles)
                {
                    var root = file.Split('\\')[0];
                    var extension = System.IO.Path.GetExtension(file);

                    if (!allowedRoots.Contains(root) || !allowedExtensions.Contains(extension))
                    {
                        Logger.Error("FolderFiles path or extension is not allowed. A list of allowed paths and extensions is available here: https://learn.microsoft.com/en-us/windows/client-management/mdm/diagnosticlog-csp#diagnosticarchivearchivedefinition");
                        return;
                    }
                }
            }

            if (options.OutputFileFormat != null && options.OutputFileFormat != "Flattened")
            {
                Logger.Error("OutputFileFormat must be null or 'Flattened'.");
                return;
            }

            // Authenticate and get an access token for Intune
            _ = new IntuneClient();
            IntuneClient intuneClient = await IntuneClient.InitAndGetAccessToken(options, database);
            if (intuneClient is null) return;

            // Create a filter for the device
            string filterId = await intuneClient.NewDeviceAssignmentFilter(options.Device);
            if (filterId is null) return;

            // Create a custom config policy using the Graph API
            string[] policyIds = await intuneClient.CreateDCv1DiagnosticLogPolicy(options.Device, options.Url, options.RegistryKeys, options.Events, options.Commands, options.FolderFiles, options.OutputFileFormat);
            if (policyIds is null) return;

            // Assign the policy with the filter
            bool success = await intuneClient.AssignPolicyWithFilter(policyIds[0], filterId);
            if (!success) return;

            Logger.Info($"Successfully created and assigned diagnostic logs policy with request ID {policyIds[1]}.");
        }
    }
}
