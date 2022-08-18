![CodeQL](https://github.com/MultifactorLab/MultiFactor.SelfService.Windows.Portal/workflows/CodeQL/badge.svg)

# MultiFactor.SelfService.Windows.Portal

_Also available in other languages: [Русский](README.ru.md)_

MultiFactor SelfService Portal is a website developed and maintained by MultiFactor for self-enrollment of a second authentication factor by users within an Active Directory corporate network.

The portal is a part of <a href="https://multifactor.pro/" target="_blank">MultiFactor</a> 2FA hybrid solution.

* <a href="https://github.com/MultifactorLab/MultiFactor.SelfService.Windows.Portal" target="_blank">Source code</a>
* <a href="https://github.com/MultifactorLab/MultiFactor.SelfService.Windows.Portal/releases" target="_blank">Build</a>

See documentation at https://multifactor.pro/docs/multifactor-selfservice-windows-portal/ for additional guidance on  Self-Service Portal deployment.

## Table of Contents

- [Features](#features)
- [First Steps](#first-steps)
- [Prerequisites](#prerequisites)
- [Configuration](#configuration)
- [Installation](#installation)
- [Logs](#logs)
- [Access Portal](#access-portal)
- [Use Cases](#use-cases)
- [License](#license)

## Features

- User's login and password verification in an Active Directory domain. Multiple domains are supported if a trust relationship is configured between them;
- Configuration of the second authentication factor by the end-user;
- User's password change (available only after the second-factor confirmation);
- Single Sign-On for corporate applications.

The portal is designed to be installed and operated within the corporate network perimeter.

## First Steps

1. Navigate to your Multifactor <a href="https://multifactor.pro/login" target="_blank">personal profile</a> , and under the **Resources** section create a new website with the following paramteres:
  - `Name and address:` `<could be any>`;
  - `Token format:` `JwtRS256`.
2. Upon creation you should receive **ApiKey** and **ApiSecret** parameters. You will need these parameters to complete the installation.

## Prerequisites

- Installation requires Windows server starting from 2012 R2;
- The server with the installed portal requires access to the ```api.multifactor.ru``` host via TCP port 443 (TLS);
- You need to have a Web Server (IIS) role with Application Development -> ASP.NET 4.6 component installed on the server;

<img src="https://multifactor.pro/img/self-service-portal/windows-server-asp-net-role.png" width="600"/>

## Configuration

Portal settings are stored in the ```web.config``` file in XML format.

```xml
<portalSettings>
  <!--Name of your organization-->
  <add key="company-name" value="ACME" />
  <!--Name of your Active Directory domain to verify username and password -->
  <add key="company-domain" value="domain.local" />
  <!--Company logo URL address-->
  <add key="company-logo-url" value="/mfa/content/images/logo.svg" />
  
  <!-- [Optional] Require second factor for users in specified group only (Single Sign-On users). Second-factor will be required for all users by default if setting is deleted. -->
  <!--add key="active-directory-2fa-group" value="2FA Users"/-->

  <!-- [Optional] Use your users' phone numbers contained in Active Directory to automatically enroll your users and start send one-time SMS codes. Option is not used if settings are removed. -->
  <!--add key="use-active-directory-user-phone" value="true"/-->
  <!--add key="use-active-directory-mobile-user-phone" value="true"/-->

  <!-- Multifactor API Address-->
  <add key="multifactor-api-url" value="https://api.multifactor.ru" />
  <!-- API KEY parameter from the Multifactor personal account -->
  <add key="multifactor-api-key" value="" />
  <!-- API Secret parameter from the Multifactor personal account -->
  <add key="multifactor-api-secret" value="" />

  <!-- [Optional] Access the Multifactor API via the HTTP proxy -->
  <!--add key="multifactor-api-proxy" value="http://proxy:3128"/-->

  <!-- Logging level: 'Debug', 'Info', 'Warn', 'Error' -->
  <add key="logging-level" value="Info" />

  <!-- Enbale captcha validation -->
  <add key="enable-google-re-captcha" value="false"/>
  <!-- Site Key from https://www.google.com/recaptcha/admin -->
  <add key="google-re-captcha-key" value="key"/>
  <!-- Secret Key from https://www.google.com/recaptcha/admin -->
  <add key="google-re-captcha-secret" value="secret"/>
</portalSettings>
```

If the ```use-active-directory-user-phone``` option is enabled, the component will use the phone stored in the **General** tab. All phone number formats are supported.

<img src="https://multifactor.pro/img/radius-adapter/ra-ad-phone-source.png" width="400">

If the ```use-active-directory-mobile-user-phone``` option is enabled, the component will use the phone stored in the **Telephones** tab in the **Mobile** field. All phone number formats are supported.

<img src="https://multifactor.pro/img/radius-adapter/ra-ad-mobile-phone-source.png" width="400">

## Installation

1. Launch ```Server Manager``` -> ```Tools``` -> ```Internet Information Services (IIS) Manager```;

2. Right-click **Default Web Site** and select **Add Application**;
<img src="https://multifactor.pro/img/self-service-portal/windows-iis-add-app.png" width="600"/>

3. Create a new application:
  * Alias: ```mfa```
  * Physical path: ```path to the Self-Service Portal directory```

4. Save and close.

## Logs

The Self-Service Portal logs are located in the ```Logs``` directory. If they are not there, make sure that the directory is writable by the ```IIS AppPool\DefaultAppPool``` local user.

## Access Portal

The portal can be accessed at ```https://yourdomain.com/mfa```

## Use Cases

The portal is used for self-enrollment and registration of the second authentication factor by users within the corporate network. It also acts as a Single Sign-On entry point for corporate SSO applications.

Once the second-factor is configured, users can securely connect via VPN, VDI, or Remote Desktop.

- Two Factor Authentication [Windows VPN with Routing and Remote Access Service (RRAS)](https://multifactor.pro/docs/windows-2fa-rras-vpn/)
- Two-factor authentication for [Microsoft Remote Desktop Gateway](https://multifactor.pro/docs/windows-2fa-remote-desktop-gateway/)

## License

MultiFactor SelfService Portal is distributed under the [MIT License](LICENSE.md).
The component is a part of <a href="https://multifactor.pro/" target="_blank">MultiFactor</a> 2FA hybrid solution.