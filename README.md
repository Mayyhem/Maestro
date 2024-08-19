<p align="center">
    <img src="https://img.shields.io/endpoint?url=https%3A%2F%2Fraw.githubusercontent.com%2Fspecterops%2F.github%2Fmain%2Fconfig%2Fshield.json&style=flat"
        alt="Sponsored by SpecterOps"/></a>
    <a href="https://twitter.com/_Mayyhem">
        <img src="https://img.shields.io/twitter/follow/_Mayyhem?style=social"
        alt="@_Mayyhem on Twitter"/></a>
</p>

---

# Maestro
Maestro is a post-exploitation tool designed to interact with Intune/EntraID from a C2 agent on a user’s workstation without requiring knowledge of the user’s password or Azure authentication flows, token manipulation, and web-based administration console. Maestro makes interacting with Intune and EntraID (and potentially other Azure services) from C2 much easier, as the operator does not need to obtain the user’s cleartext password, extract primary refresh token (PRT) cookies from the system, run additional tools or a browser session over a SOCKS proxy, or deal with Azure authentication flows, tokens, or conditional access policies in order to execute actions in Azure on behalf of the logged-in user.

[DEF CON 32 Demo Labs slides](https://docs.google.com/presentation/d/1TGl-ASNo-1jXMOha9yd1CdPI-zCMt2UP/edit?usp=sharing&ouid=114582824289521319309&rtpof=true&sd=true)
