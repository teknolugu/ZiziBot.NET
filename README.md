# Zizi Bot .NET

Official repository WinTenDev Zizi Bot, written in .NET

### Status

[![CodeFactor](https://www.codefactor.io/repository/github/wintendev/ZiziBot.net/badge)](https://www.codefactor.io/repository/github/wintendev/ZiziBot.net)
[![License](https://img.shields.io/github/license/WinTenDev/ZiziBot.NET?label=License&color=brightgreen&cacheSeconds=3600)](./LICENSE)
![Lines of code](https://img.shields.io/tokei/lines/github/WinTenDev/ZiziBot.NET?style=flat-square)
![GitHub repo size](https://img.shields.io/github/repo-size/WinTenDev/ZiziBot.NET?style=flat-square)

[![GitHub last commit](https://img.shields.io/github/last-commit/WinTenDev/ZiziBot.NET?style=flat-square)](https://github.com/WinTenDev/ZiziBot.NET)
![GitHub commit activity](https://img.shields.io/github/commit-activity/m/WinTenDev/ZiziBot.NET?style=flat-square)
![GitHub commit activity](https://img.shields.io/github/commit-activity/w/WinTenDev/ZiziBot.NET?style=flat-square)

### CI/CD

[![Zizi Bot Dev](https://github.com/WinTenDev/ZiziBot.NET/actions/workflows/zizibot-dev-build.yml/badge.svg)](https://github.com/WinTenDev/ZiziBot.NET/actions/workflows/zizibot-dev-build.yml)
[![Zizi Bot Alpha](https://github.com/WinTenDev/ZiziBot.NET/actions/workflows/zizibot-alpha-build.yml/badge.svg)](https://github.com/WinTenDev/ZiziBot.NET/actions/workflows/zizibot-alpha-build.yml)

# Preparation

1. .NET 6 SDK
2. MySQL/MariaDB
3. Nginx or OpenLiteSpeed for reverse proxy (Optional)
4. ClickHouse (Optional, for analytic)
5. Google Cloud API (Optional, for OCR, Drive, etc.)
6. Uptobox Token (Optional, for Mirror)
7. Datadog (Optional, for logging)
8. Exceptionless (Optional, trace error)
9. Sentry (Optional, trace error)

# Keys of feature

- Realtime SpamCheck. Powered by ES2, SpamWatch and CAS.
- Realtime inspect changing about username, first Name and last Name (called Zizi Mata).
-

# Run Development

- Clone this repo and open .sln using your favorite IDE or Text Editor.
- Install MySQL/MariaDB and create database e.g. `zizibot_data`.
- Copy appsettings.example.json to appsettings.json and fill some property.
- Press Start in your IDE to start debugging or via CLI.
- Your bot has ran local as Development using Poll mode.

# Currently, available on following bot

- [Zizi Bot](https://t.me/MissZiziBot) (Stable)
- [Zizi Beta](https://t.me/MissZiziBetaBot) (Beta)
- [Zizi Dev](https://t.me/MissZiziDevBot) (Dev)
