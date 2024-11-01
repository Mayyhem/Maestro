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

# Syntax
`Maestro.exe <command> [subcommand] [options]`

All commands and subcommands have a help page that is generated using a custom command line parser to keep the size of the binary to a minimum. Help pages can be accessed by entering any Maestro command followed by `-h` or `--help`.

Please refer to the output of the `--help` option for each command for the most up-to-date usage information.

# Features
- Real-time PowerShell script execution (via Proactive Remediations)
- Application execution
- Real-time Device Query execution
- Force device check-in and sync
- Intune and Entra object enumeration
- Local database to store credentials and query results

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

If you're interested in collaborating, please hit me up on Twitter ([@_Mayyhem](https://twitter.com/_Mayyhem)) or in the [BloodHoundGang Slack](http://ghst.ly/BHSlack)!
