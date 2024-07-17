# NginxPanel

## Table of Contents

* [Introduction](#introduction)
* [Requirements](#requirements)
* [Important](#important)
* [Install](#install)
* [Running as a service](#running-as-a-service)
* [Configuration settings](#configuration-settings)
* [Reporting issues](#reporting-issues)
* [Submitting fixes or additions](#submitting-fixes-or-additions)

## Introduction

NginxPanel is a **front-end** for Ubuntu 22.04 *only* (for now). This currently requires it to be ran with sudo (ideally in an LXD/Incus container for ease of setting it up) as it must control services.

## Requirements

* Ubuntu 22.04 (may work on other versions)
* .NET 8

*Optional:*

* *OpenSSL for PFX Export (Only when installing certificates)*
* *LXD/Incus for Deployment*

### Important

 This is currently in a very **beta** state!  Some things may not function at all yet!

## Install

1. Install .NET 8:

    ```bash
    sudo apt-get update && sudo apt-get install -y aspnetcore-runtime-8.0
    ```

2. Download publish.tar from releases page.

    ```bash
    wget <url to asset on releases page>
    ```

3. Extract to folder:

    ```bash
    mkdir -p /opt/nginxpanel && tar -xvf publish.tar -C /opt/nginxpanel
    ```

4. Give execute rights:

    ```bash
    chmod +x /opt/nginxpanel/NginxPanel
    ```

5. Run binary (must run from the same folder so the working directory is correct):

    ```bash
    cd /opt/nginxpanel
    /opt/nginxpanel/NginxPanel
    ```

6. On first launch the self-signed PFX and app.config files will be generated in /etc/nginxpanel, modify the config to customize port and PFX/password.
7. Nginx can be installed from the application itself, it will give you the option of using the package manager, stable, or mainline versions.
8. ACME.sh can (and should) be installed from the application itself.

## Running as a service

Run the following command and paste the systemd content to create a service for this application (modify paths as needed):

1. Command to create service:

    ```bash
    systemctl edit --force --full nginxpanel
    ```

2. Service file content:

    ```bash
    [Unit]
    Description=NginxPanel Service
    Wants=network-online.target
    After=network-online.target

    [Service]
    WorkingDirectory=/opt/nginxpanel
    ExecStart=/opt/nginxpanel/NginxPanel
    Restart=always
    RestartSec=10
    User=root

    [Install]
    WantedBy=multi-user.target
    ```

## Configuration settings

You can find the generated config file after first run at /etc/nginxpanel/app.conf.
> NOTE: This file is currently loaded AND resaved upon each run, so unmatched settings/comments will be removed! (This behavior will change at a later date.)

* Port: Port that the application will listen on.

* PFXPath: Full path to where the **self-signed PFX** or **preferred PFX** certificate lives. Auto-generated on first start if none exists.

* PFXPassword: Password for the specificed PFX certificate. Randomly generated on first start for new self-signed certificates.

* DisableAuthWarningOnStart: Used for disabling the toast on startup about not having any authentication settings specified yet (if you wish to run this with no auth).

* Username: Username to use for login (login is not case sensitive).

* Password: Password for your application, 15 characters or more is best (do not reuse your passwords).

* DUOEnabled: Used for turning on DUO push 2FA, must also specify other DUO settings.

* DUOClientID/DUOSecretKey/DUOAPIHostname: Values from the DUO admin portal when you create a new web SDK application.

* DUOUsername: Optional if basic auth username has been set and you are okay with that, otherwise this is required (or can be used as an override).

> [!IMPORTANT]
> In order for DUO to function correctly, your instance must be DNS resolvable as the DUO redirect will be to the hostname of the instance NOT the IP!

## Reporting issues

Issues can be reported via the [Github issue tracker](https://github.com/SoulSeekkor/NginxPanel/issues).

Please take the time to review existing issues before submitting your own to prevent duplicates.

## Submitting fixes or additions

Fixes are submitted as pull requests via Github.

> **License: GNU GPLv3** read [here](https://www.gnu.org/licenses/agpl-3.0.en.html).
