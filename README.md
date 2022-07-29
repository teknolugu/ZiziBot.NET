# Zizi Bot .NET

<img align="right" src="https://komarev.com/ghpvc/?username=WinTenDev&style=flat&color=d83a7c&label=Views" alt="viewer" />

Official repository WinTenDev Zizi Bot, written in .NET

### Status

[![CodeFactor](https://www.codefactor.io/repository/github/wintendev/ZiziBot.net/badge)](https://www.codefactor.io/repository/github/wintendev/ZiziBot.net)
[![License](https://img.shields.io/github/license/WinTenDev/ZiziBot.NET?label=License&color=brightgreen&cacheSeconds=3600)](./LICENSE)

### Statistic

![Lines of code](https://img.shields.io/tokei/lines/github/WinTenDev/ZiziBot.NET?style=flat-square)
![GitHub repo file count](https://img.shields.io/github/directory-file-count/WinTenDev/ZiziBot.NET?style=flat-square)
![GitHub repo size](https://img.shields.io/github/repo-size/WinTenDev/ZiziBot.NET?style=flat-square)

### Activity

![GitHub last commit](https://img.shields.io/github/last-commit/WinTenDev/ZiziBot.NET?style=flat-square)
![GitHub commit activity](https://img.shields.io/github/commit-activity/w/WinTenDev/ZiziBot.NET?style=flat-square)
![GitHub commit activity](https://img.shields.io/github/commit-activity/m/WinTenDev/ZiziBot.NET?style=flat-square)

### Build

[![Zizi Bot Dev](https://github.com/WinTenDev/ZiziBot.NET/actions/workflows/zizibot-dev-build.yml/badge.svg)](https://github.com/WinTenDev/ZiziBot.NET/actions/workflows/zizibot-dev-build.yml)
[![Zizi Bot Alpha](https://github.com/WinTenDev/ZiziBot.NET/actions/workflows/zizibot-alpha-build.yml/badge.svg)](https://github.com/WinTenDev/ZiziBot.NET/actions/workflows/zizibot-alpha-build.yml)

[![Build status](https://ci.appveyor.com/api/projects/status/3q9x4lpy0w81bvdb/branch/main?svg=true)](https://ci.appveyor.com/project/Azhe403/zizibot-net/branch/main)
[![Build status](https://ci.appveyor.com/api/projects/status/3q9x4lpy0w81bvdb/branch/stable?svg=true)](https://ci.appveyor.com/project/Azhe403/zizibot-net/branch/stable)
[![Build status](https://ci.appveyor.com/api/projects/status/3q9x4lpy0w81bvdb?svg=true)](https://ci.appveyor.com/project/Azhe403/zizibot-net)

### Binary

| Source         | URL                                                                 |
|----------------|---------------------------------------------------------------------|
| Github Release | https://github.com/WinTenDev/ZiziBot.NET/releases                   |

### Deployment

[![Deploy to Zizi Beta](https://github.com/WinTenDev/ZiziBot.NET/actions/workflows/deploy-zizibeta-to-linux.yml/badge.svg)](https://github.com/WinTenDev/ZiziBot.NET/actions/workflows/deploy-zizibeta-to-linux.yml)
[![Deploy to Zizi Bot](https://github.com/WinTenDev/ZiziBot.NET/actions/workflows/deploy-zizibot-to-linux.yml/badge.svg)](https://github.com/WinTenDev/ZiziBot.NET/actions/workflows/deploy-zizibot-to-linux.yml)

### Workspace

[![Open in Gitpod](https://gitpod.io/button/open-in-gitpod.svg)](https://gitpod.io/#https://github.com/WinTenDev/ZiziBot.NET)

# Preparation

1. .NET 6 SDK
2. MySQL/MariaDB (we under plan to migrate to MongoDB)
3. MongoDB (Some data has stored to MongoDB)
4. Bot Token (required)
5. Nginx or OpenLiteSpeed for reverse proxy (Optional)
6. ClickHouse (Optional, for analytic)
7. Google Cloud API (Optional, for OCR, Drive, etc.)
8. [OptiicDev API](https://optiic.dev) (optional, for OCR)
9. GitHub PAT (optional, for Advanced RSS for GitHub release)
10. Uptobox Token (Optional, for Mirror)
11. Datadog (Optional, for logging)
12. Exceptionless (Optional, trace error)
13. Sentry (Optional, trace error)
14. EventLog (Optional, send error to ChatId)

# Main feature

Here some main features, and some features is enabled by default.

- Realtime AntiSpam check for Member. Powered by ES2, SpamWatch, UserGe and CAS.
- Watching username, first Name and last Name changes (called Zizi Mata).
- Scan Message for prevent message contains spam, badwords or unattended strings.
- Force subscription into Linked and Added channel.
- Flood Detector for reducing message sent by member (BETA).
- Anti-CapsLock for reducing to many uppercase characters (BETA).
- Anti-Spam detector based on User activity (for Public Group only) (BETA).
- AutoAnswer for Chat Join Request. As per feature above if enabled, some member check above will be ran and action will
  be
  executed on the fly (Check Username, Profile Photo, Force Subscription etc.)

# Additional feature

- ShalatTime for Indonesia (can multiple city for one Private/Group).
- OCR powered by OptiicDev API.
- Generate and Read QR for replied Message.
- RSS Feed (and Advanced RSS for source like GitHub release).
- Translate message text for replied Message.
- Generate random number with some expression.
- Get random Cat images.
- EGS Free Weekly.
- Subtitle search and Download, powered by subscene.com
- Search word on KBBI.
- And many others.

# Run Development

- Clone this repo and open .sln using your favorite IDE or Text Editor.
- Install MySQL/MariaDB and create database e.g. `zizibot_data`.
- Copy appsettings.example.json to appsettings.json and fill some property.
- check an optional .json under Storage/AppSettings for some feature if you want, copy from Examples to Current and fill
  some property.
- Press Start in your IDE to start debugging or via CLI.
- Your bot has ran local as Development using Poll mode.

# Run Production

Make sure you have fill appsettings.json and setup any requirement at Preparation section.
For quick deployment you can get prebuilt binary, go to Releases tab or AppVeyor at artifacts section.

# Currently, available on following bot

- [Zizi Bot](https://t.me/MissZiziBot) from stable branch (Stable)
- [Zizi Beta](https://t.me/MissZiziBetaBot) from main branch (Beta)
- [Zizi Dev](https://t.me/MissZiziDevBot) from local development (Dev)
