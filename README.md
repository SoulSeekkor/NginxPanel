# NginxPanel

#### Table of Contents
* [Introduction](#introduction)
* [Requirements](#requirements)
* [Install](#install)
* [Reporting issues](#reporting-issues)
* [Submitting fixes or additions](#submitting-fixes-or-additions)

#### Introduction
NginxPanel is a **front-end** for Ubuntu 22.04 *only* (for now). This currently requires it to be ran with sudo (ideally in an LXD/Incus container for ease of setting it up) as it must control services.

# Requirements
* Ubuntu 22.04 (may work on other versions)
* .NET 8
* *Optionally LXD/Incus*
 
 ### Important
 This is currently in a very **beta** state!  Some things may not function at all yet!

# Install
1) Install .NET 8:
```
sudo apt-get update && sudo apt-get install -y aspnetcore-runtime-8.0
```
2) Download publish.tar from releases page.
```
wget <url to asset on releases page>
```
3) Extract to folder:
```
tar -xvf publish.tar -C /opt/nginxpanel
```
4) Give execute rights:
```
chmod +x /opt/nginxpanel/NginxPanel
```
5) Run binary:
```
/opt/nginxpanel/NginxPanel
```

# Running as a service
Run the following command and paste the systemd content to create a service for this application (modify paths as needed):
1) Command to create service:
```
systemctl edit --full nginxpanel
```
2) Service file content:
```
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
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

# Reporting issues
Issues can be reported via the [Github issue tracker](https://github.com/SoulSeekkor/NginxPanel/issues).

Please take the time to review existing issues before submitting your own to prevent duplicates.

# Submitting fixes or additions
Fixes are submitted as pull requests via Github.

> **License: GNU GPLv3** read [here](https://www.gnu.org/licenses/agpl-3.0.en.html).