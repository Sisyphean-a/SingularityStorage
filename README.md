# 奇点存储 (Singularity Storage) - 开发文档

## 简介

奇点存储是一个为《星露谷物语》设计的高级存储模组，旨在解决游戏后期的库存管理难题。
本模组采用了“虚拟化存储”架构，将物品数据与游戏对象分离，实现了理论上的无限容量、极高的网络同步效率以及流畅的 UI 体验。

## 核心功能

- **无限存储**：基于 JSON 的独立数据存储，突破原版 `NetList` 限制。
- **虚拟化 UI**：支持分页和实时搜索，即使存有数万物品也能保持 60 帧流畅渲染。
- **工作台兼容**：自动兼容原版工作台（Workbench），无需复杂连线。
- **多人联机**：支持主机-客户端模式，数据安全同步。
- **多语言支持**：内置简繁中文与英语。

## 架构概览

### 项目结构

- `ModEntry.cs`: 模组入口，初始化 Harmony 和管理器。
- `StorageManager.cs`: 核心数据管理器，负责 JSON IO 和内存缓存。
- `Data/SingularityInventoryData.cs`: 数据模型。
- `UI/SingularityMenu.cs`: 虚拟化背包菜单，实现前端逻辑。
- `Network/NetworkManager.cs`: 处理多人游戏的数据包同步。
- `Patches/WorkbenchPatcher.cs`: Harmony 补丁，注入工作台逻辑。

### 关键技术点

1.  **QualifiedItemId**: 全面适配 Stardew Valley 1.6 的字符串 ID 系统。
2.  **Harmony Patch**: 拦截 `CraftingPage` 构造函数，动态注入代理箱子（Proxy Chest）。
3.  **On-Demand Networking**: 客户端仅请求当前页面的 36 个物品，极大降低带宽压力。

## 安装指南

1.  安装 [SMAPI](https://smapi.io/).
2.  安装 [Content Patcher](https://www.nexusmods.com/stardewvalley/mods/1915).
3.  解压本模组到 `Mods` 文件夹。

## 开发日志

- [x] 完成基础设施搭建 (csproj, manifest)
- [x] 完成核心数据层 (StorageManager)
- [x] 完成游戏内对象与交互 (InteractionHandler)
- [x] 完成虚拟化 UI (SingularityMenu)
- [x] 完成工作台兼容性补丁
- [x] 完成多人联机同步框架
- [x] 完成国际化支持 (i18n)

## 待办事项 (TODO)

- [ ] 更加精细的升级系统 (T1/T2/T3 升级组件)。
- [ ] 远程终端访问 (Wireless Terminal)。
- [ ] 自动化 (Automate) 模组深度集成。
