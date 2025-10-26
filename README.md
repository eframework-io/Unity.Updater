# Unity.Updater

[![NPM](https://img.shields.io/npm/v/io.eframework.unity.updater?label=NPM&logo=npm)](https://www.npmjs.com/package/io.eframework.unity.updater)
[![UPM](https://img.shields.io/npm/v/io.eframework.unity.updater?label=UPM&logo=unity&registry_uri=https://package.openupm.com)](https://openupm.com/packages/io.eframework.unity.updater)
[![DeepWiki](https://img.shields.io/badge/DeepWiki-Explore-blue)](https://deepwiki.com/eframework-io/Unity.Updater)
[![Discord](https://img.shields.io/discord/1422114598835851286?label=Discord&logo=discord)](https://discord.gg/XMPx2wXSz3)

Unity.Updater 提供了一套完整的 Unity 应用更新解决方案，支持补丁包与安装包的统一管理，通过业务接口控制更新流程，并以事件机制驱动状态变化。

## 功能特性

- [XUpdater](Documentation~/XUpdater.md) 提供了更新流程控制和事件通知，实现了可扩展的业务处理接口
- [XPatcher](Documentation~/XPatcher.md) 实现了补丁包的提取、校验和下载功能，采用并发任务提升更新效率
- [XInstaller](Documentation~/XInstaller.md) 提供了安装包的更新功能，支持自动下载并解压安装

## 常见问题

更多问题，请查阅[问题反馈](CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](CHANGELOG.md)
- [贡献指南](CONTRIBUTING.md)
- [许可协议](LICENSE.md)