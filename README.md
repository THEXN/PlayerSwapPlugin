 # PlayerSwapPlugin 使用文档

## 简介

PlayerSwapPlugin 是一个为 Terraria 游戏服务器设计的插件，它允许玩家在指定的时间间隔后随机交换位置，增加了游戏的趣味性和互动性。插件支持双人交换和多人混乱模式，并且提供了丰富的配置选项，让服务器管理员可以根据需要调整插件的行为。

## 使用方法

### 基本命令

- **启用/禁用插件**：
  `/swaptoggle enable` 或 `/swaptoggle 开关` - 开关插件。

- **设置交换间隔**：
  `/swaptoggle interval <秒数>` 或 `/swaptoggle 传送间隔 <秒数>` - 设置玩家交换位置的时间间隔。

- **允许/禁止玩家与自己交换**：
  `/swaptoggle allowself` 或 `/swaptoggle 和自己交换` - 开启或关闭玩家与自己交换位置的权限。

- **广播剩余传送时间**：
  `/swaptoggle timer` 或 `/swaptoggle 广播时间` - 开启或关闭在玩家交换前广播剩余传送时间的功能。

- **广播玩家交换信息**：
  `/swaptoggle swap` 或 `/swaptoggle 广播交换` - 开启或关闭在玩家交换位置后广播交换信息的功能。

- **多人打乱模式**：
  `/swaptoggle allowmulti` 或 `/swaptoggle 允许多人` - 开启或关闭多人打乱模式。

### 权限

- **免检测权限**：
  `noPlayerSwap`  - 有该权限的玩家不被传送。

  - **指令**：
  `swapplugin.toggle`  - swaptoggle指令的权限

### 高级配置

- **配置文件**：
  `玩家位置随机互换配置.json` - 位于 Terraria 服务器的 `TShockh` 目录下。这个文件包含了所有插件的设置，包括交换间隔、广播选项等。你可以通过编辑配置文件来调整这些设置，或者使用 `/swaptoggle` 命令进行实时更改。

### 注意事项

- 在修改服务器设置前，请确保你有足够权限。
- 插件可能会在某些情况下导致游戏不稳定，建议在测试环境中先行测试。

## 支持与反馈
- 如果您在使用过程中遇到问题或有任何建议，欢迎在官方论坛或社区中提出issues或pr。
- github仓库：https://github.com/THEXN/PlayerSwapPlugin
