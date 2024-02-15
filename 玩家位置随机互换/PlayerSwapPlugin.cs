using System;
using System.Linq;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using System.Timers;
using TShockAPI.Hooks;
using Microsoft.Xna.Framework;

namespace PlayerSwapPlugin
{
    [ApiVersion(2, 1)]
    public class PlayerSwapPlugin : TerrariaPlugin
    {
        private System.Timers.Timer timer;
        private Random random;
        private bool pluginEnabled = true;

        public override string Author => "肝帝熙恩";
        public override string Description => "一个插件，用于在指定时间后随机交换玩家位置。";
        public override string Name => "PlayerSwapPlugin";
        public override Version Version => new Version(1, 0, 5);
        public static Configuration Config;
        public PlayerSwapPlugin(Main game) : base(game)
        {
            LoadConfig();
        }

        private static void LoadConfig()
        {
            Config = Configuration.Read(Configuration.FilePath);
            Config.Write(Configuration.FilePath);
        }

        private static void ReloadConfig(ReloadEventArgs args)
        {
            LoadConfig();
            args.Player?.SendSuccessMessage("[{0}] 重新加载配置完毕。", typeof(PlayerSwapPlugin).Name);
        }

        public override void Initialize()
        {
            GeneralHooks.ReloadEvent += ReloadConfig;
            random = new Random();
            timer = new System.Timers.Timer();
            timer.Interval = TimeSpan.FromSeconds(Config.IntervalSeconds).TotalMilliseconds; // 将此值更改为所需的间隔时间（以秒为单位）
            timer.AutoReset = true; 
            timer.Elapsed += Timer_Elapsed;
            timer.Start();

            TShockAPI.Commands.ChatCommands.Add(new Command("swapplugin.toggle", SwapToggle, "swaptoggle","更改随机互换"));
        }

        private int remainingTime; // 添加一个变量来保存剩余时间

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!pluginEnabled)
                return;

            if (remainingTime <= 0)
            {
                SwapPlayers(); // 执行交换玩家位置的操作

                // 重置计时器间隔，延迟一段时间后再次交换
                timer.Interval = TimeSpan.FromSeconds(Config.IntervalSeconds).TotalMilliseconds; // 20 秒后再次交换
                remainingTime = (int)Config.IntervalSeconds; // 重置剩余时间
            }
            else
            {
                remainingTime--; // 减少剩余时间
                if (Config.BroadcastRemainingTimeEnabled)
                {
                    TShock.Utils.Broadcast($"剩余传送时间：{remainingTime}秒", Color.Yellow);
                }
            }
        }


        private void SwapPlayers()
        {
            var players = TShock.Players.Where(p => p != null && p.Active).ToList();
            if (players.Count < 2)
                return;

            // 获取没有特定权限的玩家列表
            var eligiblePlayers = players.Where(p => !p.HasPermission("noPlayerSwap")).ToList();

            if (eligiblePlayers.Count < 2)
                return;

            if (Config.MultiPlayerMode)
            {
                // 多人打乱模式逻辑
                ShufflePlayers(eligiblePlayers);
            }
            else
            {
                // 双人模式逻辑
                SwapTwoPlayers(eligiblePlayers);
            }
        }

        private void SwapTwoPlayers(List<TSPlayer> players)
        {
            int index1 = random.Next(players.Count);
            int index2 = random.Next(players.Count);

            while (!Config.AllowSamePlayerSwap && index2 == index1)
            {
                index2 = random.Next(players.Count);
            }

            var player1 = players[index1];
            var player2 = players[index2];

            // 检查是否为同一玩家
            if (!Config.AllowSamePlayerSwap && player1 == player2)
            {
                player1.SendInfoMessage("你尝试和自己交换位置，但没有发生任何变化。");
                return;
            }

            SwapPositions(player1, player2);

            // 发送交换提示
            player1.SendInfoMessage($"你已与玩家 {player2.Name} 交换了位置！");
            player2.SendInfoMessage($"你已与玩家 {player1.Name} 交换了位置！");

            // 广播玩家交换位置信息
            if (Config.BroadcastPlayerSwapEnabled)
            {
                TShock.Utils.Broadcast($"玩家 {player1.Name} 和玩家 {player2.Name} 交换了位置！", Color.Yellow);
            }
        }



        private class PlayerPosition
        {
            public TSPlayer Player { get; set; }
            public Vector2 Position { get; set; }
        }

        private void ShufflePlayers(List<TSPlayer> players)
        {
            // 记录玩家的位置
            List<Vector2> playerPositions = new List<Vector2>();
            foreach (var player in players)
            {
                playerPositions.Add(new Vector2(player.TileX, player.TileY));
            }

            // 混洗记录的玩家位置，确保每个位置只被一个玩家使用
            playerPositions = playerPositions.OrderBy(x => random.Next()).ToList();

            // 混乱玩家位置
            foreach (var player in players)
            {
                var newPosition = playerPositions.First();
                playerPositions.Remove(newPosition); // 移除已使用的位置

                // 如果当前位置是该玩家的位置，则再次尝试获取一个新的位置
                if (player.TileX == newPosition.X && player.TileY == newPosition.Y)
                {
                    playerPositions.Add(newPosition); // 将该位置添加回列表中
                    newPosition = playerPositions.First(p => p != new Vector2(player.TileX, player.TileY)); // 获取新的位置
                    playerPositions.Remove(newPosition); // 移除已使用的位置
                }

                player.Teleport((int)newPosition.X * 16, (int)newPosition.Y * 16);

                // 发送交换提示
                if (Config.BroadcastPlayerSwapEnabled)
                {
                    var playerName = player.Name;
                    var originalPlayerName = players.First(p => p.TileX == newPosition.X && p.TileY == newPosition.Y).Name;
                    var message = $"玩家 {playerName} 被传送到了玩家 {originalPlayerName} 的位置";
                    player.SendInfoMessage(message);
                }
            }
        }

        private void SwapPositions(TSPlayer player1, TSPlayer player2)
        {
            // 保存 player1 当前位置
            int tempX1 = player1.TileX;
            int tempY1 = player1.TileY;

            // 保存 player2 当前位置
            int tempX2 = player2.TileX;
            int tempY2 = player2.TileY;

            // 传送 player1 至 player2 的位置
            player1.Teleport(tempX2 * 16, tempY2 * 16);

            // 传送 player2 至 player1 的位置
            player2.Teleport(tempX1 * 16, tempY1 * 16);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (timer != null)
                {
                    timer.Stop();
                    timer.Dispose();
                    timer = null;
                }
            }
            base.Dispose(disposing);
        }

        private void SwapToggle(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage("用法: /swaptoggle <timer|swap|enable|interval|allowself|allowmulti|broadcasttime|broadcastswap>");
                return;
            }

            string subCommand = args.Parameters[0].ToLower();
            switch (subCommand)
            {
                case "timer":
                    Config.BroadcastRemainingTimeEnabled = !Config.BroadcastRemainingTimeEnabled;
                    args.Player.SendSuccessMessage($"广播剩余传送时间已{(Config.BroadcastRemainingTimeEnabled ? "启用" : "禁用")}。");
                    break;
                case "swap":
                    Config.BroadcastPlayerSwapEnabled = !Config.BroadcastPlayerSwapEnabled;
                    args.Player.SendSuccessMessage($"广播玩家交换位置信息已{(Config.BroadcastPlayerSwapEnabled ? "启用" : "禁用")}。");
                    break;
                case "enable":
                    pluginEnabled = !pluginEnabled;
                    args.Player.SendSuccessMessage($"随机位置互换已{(pluginEnabled ? "启用" : "禁用")}。");
                    break;
                case "interval":
                    if (args.Parameters.Count < 2 || !int.TryParse(args.Parameters[1], out int interval))
                    {
                        args.Player.SendErrorMessage("用法: /swaptoggle interval <传送间隔秒>");
                        return;
                    }
                    Config.IntervalSeconds = interval;
                    args.Player.SendSuccessMessage($"传送间隔已设置为 {interval} 秒。");
                    break;
                case "allowself":
                    Config.AllowSamePlayerSwap = !Config.AllowSamePlayerSwap;
                    args.Player.SendSuccessMessage($"允许玩家和自己交换位置已{(Config.AllowSamePlayerSwap ? "启用" : "禁用")}。");
                    break;
                case "allowmulti":
                    Config.AllowMultipleTeleportEnabled = !Config.AllowMultipleTeleportEnabled;
                    args.Player.SendSuccessMessage($"允许多个玩家传送到同一位置已{(Config.AllowMultipleTeleportEnabled ? "启用" : "禁用")}。");
                    break;
                case "broadcasttime":
                    Config.BroadcastRemainingTimeEnabled = !Config.BroadcastRemainingTimeEnabled;
                    args.Player.SendSuccessMessage($"广播剩余传送时间已{(Config.BroadcastRemainingTimeEnabled ? "启用" : "禁用")}。");
                    break;
                case "broadcastswap":
                    Config.BroadcastPlayerSwapEnabled = !Config.BroadcastPlayerSwapEnabled;
                    args.Player.SendSuccessMessage($"广播玩家交换位置信息已{(Config.BroadcastPlayerSwapEnabled ? "启用" : "禁用")}。");
                    break;
                case "help":
                    args.Player.SendInfoMessage("用法:");
                    args.Player.SendInfoMessage("/swaptoggle timer - 切换广播剩余传送时间的状态");
                    args.Player.SendInfoMessage("/swaptoggle swap - 切换广播玩家交换位置信息的状态");
                    args.Player.SendInfoMessage("/swaptoggle enable - 切换随机位置互换的状态");
                    args.Player.SendInfoMessage("/swaptoggle interval <传送间隔秒> - 设置传送间隔时间（秒）");
                    args.Player.SendInfoMessage("/swaptoggle allowself - 切换允许玩家和自己交换位置的状态");
                    args.Player.SendInfoMessage("/swaptoggle allowmulti - 切换允许多个玩家传送到同一位置的状态");
                    args.Player.SendInfoMessage("/swaptoggle broadcasttime - 切换广播剩余传送时间的状态");
                    args.Player.SendInfoMessage("/swaptoggle broadcastswap - 切换广播玩家交换位置信息的状态");
                    break;

                default:
                    args.Player.SendErrorMessage("用法: /swaptoggle <timer|swap|enable|interval|allowself|allowmulti|broadcasttime|broadcastswap>");
                    break;
            }
        }

    }
}
