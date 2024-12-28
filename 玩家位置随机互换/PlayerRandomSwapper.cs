using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace PlayerSwapPlugin
{
    [ApiVersion(2, 1)]
    public class PlayerRandomSwapper : TerrariaPlugin
    {
        public override string Author => "肝帝熙恩,少司命";
        public override string Description => "一个插件，用于在指定时间后随机交换玩家位置。";
        public override string Name => "PlayerSwapPlugin";
        public override Version Version => new Version(1, 0, 5);

        private DateTime LastCheck { get; set; } = DateTime.UtcNow;
        private int RemainingSeconds { get; set; }
        public static Configuration Config;

        public PlayerRandomSwapper(Main game) : base(game)
        {
            LoadConfig();
        }

        private static void LoadConfig()
        {
            Config = Configuration.Read(Configuration.FilePath);
            Config.Write(Configuration.FilePath);
        }

        private void ReloadConfig(ReloadEventArgs args)
        {
            LoadConfig();
            args.Player?.SendSuccessMessage("[{0}] 重新加载配置完毕。", typeof(PlayerRandomSwapper).Name);
            RemainingSeconds = Config.IntervalSeconds;
        }

        public override void Initialize()
        {
            GeneralHooks.ReloadEvent += ReloadConfig;
            TShockAPI.Commands.ChatCommands.Add(new Command("swapplugin.toggle", SwapToggle, "swaptoggle", "随机互换"));

            ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);

            RemainingSeconds = Config.IntervalSeconds;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);
            }
            base.Dispose(disposing);
        }

        private void OnUpdate(EventArgs args)
        {
            if (!Config.pluginEnabled)
            {
                RemainingSeconds = Config.IntervalSeconds;
                return;
            }

            var players = TShock.Players.Where(p => p != null && p.Active && !p.Dead).ToList();
            var eligiblePlayers = players.Where(p => !p.HasPermission("noplayerswap")).ToList();

            if (eligiblePlayers.Count < 2)
            {
                RemainingSeconds = Config.IntervalSeconds;
                return;
            }

            if ((DateTime.UtcNow - this.LastCheck).TotalSeconds >= 1)
            {
                this.LastCheck = DateTime.UtcNow;
                RemainingSeconds--;

                if (RemainingSeconds <= 0)
                {
                    SwapPlayers(eligiblePlayers);
                    RemainingSeconds = Config.IntervalSeconds;
                }
                else if (Config.BroadcastRemainingTimeEnabled && RemainingSeconds <= Config.BroadcastRemainingTimeThreshold)
                {
                    TShockAPI.TSPlayer.All.SendMessage($"注意：还有{RemainingSeconds}秒就要交换位置了！", Color.Yellow);
                }
            }
        }
        private void SwapToggle(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage("用法: /swaptoggle <timer|swap|enable|interval|allowself>");
                return;
            }

            string subCommand = args.Parameters[0].ToLower();
            switch (subCommand)
            {
                case "timer":
                case "广播时间":
                    Config.BroadcastRemainingTimeEnabled = !Config.BroadcastRemainingTimeEnabled;
                    args.Player.SendSuccessMessage($"广播剩余传送时间已{(Config.BroadcastRemainingTimeEnabled ? "启用" : "禁用")}。");
                    break;
                case "swap":
                case "广播交换":
                    Config.BroadcastPlayerSwapEnabled = !Config.BroadcastPlayerSwapEnabled;
                    args.Player.SendSuccessMessage($"广播玩家交换位置信息已{(Config.BroadcastPlayerSwapEnabled ? "启用" : "禁用")}。");
                    break;
                case "enable":
                case "开关":
                    Config.pluginEnabled = !Config.pluginEnabled;
                    args.Player.SendSuccessMessage($"随机位置互换已{(Config.pluginEnabled ? "启用" : "禁用")}。");
                    break;
                case "interval":
                case "传送间隔":
                    if (args.Parameters.Count < 2 || !int.TryParse(args.Parameters[1], out int interval))
                    {
                        args.Player.SendErrorMessage("用法: /swaptoggle interval <传送间隔秒>");
                        return;
                    }
                    Config.IntervalSeconds = interval;
                    args.Player.SendSuccessMessage($"传送间隔已设置为 {interval} 秒。");
                    break;
                case "allowself":
                case "和自己交换":
                    Config.AllowSamePlayerSwap = !Config.AllowSamePlayerSwap;
                    args.Player.SendSuccessMessage($"允许玩家和自己交换位置已{(Config.AllowSamePlayerSwap ? "启用" : "禁用")}。");
                    break;
                case "help":
                    args.Player.SendInfoMessage("用法:");
                    args.Player.SendInfoMessage("/swaptoggle enable - 切换随机位置互换的状态");
                    args.Player.SendInfoMessage("/swaptoggle timer - 切换广播剩余传送时间的状态");
                    args.Player.SendInfoMessage("/swaptoggle swap - 切换广播玩家交换位置信息的状态");
                    args.Player.SendInfoMessage("/swaptoggle interval <传送间隔秒> - 设置传送间隔时间（秒）");
                    args.Player.SendInfoMessage("/swaptoggle allowself - 切换允许双人模式玩家和自己交换位置的状态");
                    break;

                default:
                    args.Player.SendErrorMessage("用法: /swaptoggle <timer|swap|enable|interval|allowself");
                    args.Player.SendErrorMessage("用法: /随机互换 <广播时间|广播交换|开关|传送间隔|和自己交换");
                    break;
            }
        }

        private void SwapPlayers(List<TSPlayer> players)
        {
            if (Config.MultiPlayerMode)
            {
                SwapMorePlayers(players);
            }
            else
            {
                SwapTwoPlayers(players);
            }
        }
        private void SwapTwoPlayers(List<TSPlayer> players)
        {
            Random random = new Random();
            int index1 = random.Next(players.Count);
            int index2 = random.Next(players.Count);

            while (!Config.AllowSamePlayerSwap && index2 == index1)
            {
                index2 = random.Next(players.Count);
            }

            var player1 = players[index1];
            var player2 = players[index2];

            if (!Config.AllowSamePlayerSwap && player1 == player2)
            {
                player1.SendMessage("你尝试和自己交换位置，但没有发生任何变化。", Color.Blue);
                return;
            }

            SwapPositions(player1, player2);

            player1.SendMessage($"你已与玩家 {player2.Name} 交换了位置！", Color.Blue);
            player2.SendMessage($"你已与玩家 {player1.Name} 交换了位置！", Color.Blue);

            if (Config.BroadcastPlayerSwapEnabled)
            {
                TShockAPI.TSPlayer.All.SendMessage($"玩家 {player1.Name} 和玩家 {player2.Name} 交换了位置！", Color.Yellow);
            }
        }

        private void SwapMorePlayers(List<TSPlayer> players)
        {
            if (players.Count < 2)
                return;

            var sp = players.OrderBy(c => Guid.NewGuid()).ToList();

            for (var i = 1; i < players.Count; i++)
            {
                (sp[i - 1].TPlayer.position, sp[i].TPlayer.position) =
                (sp[i].TPlayer.position, sp[i - 1].TPlayer.position);
            }

            foreach (var player in players)
            {
                player.Teleport(player.TPlayer.position.X, player.TPlayer.position.Y);
            }

            if (Config.BroadcastPlayerSwapEnabled)
            {
                TShockAPI.TSPlayer.All.SendMessage($"已经交换所有玩家位置！", Color.Green);
            }
        }
        private void SwapPositions(TSPlayer player1, TSPlayer player2)
        {
            int tempX1 = player1.TileX;
            int tempY1 = player1.TileY;

            int tempX2 = player2.TileX;
            int tempY2 = player2.TileY;

            player1.Teleport(tempX2 * 16, tempY2 * 16);
            player2.Teleport(tempX1 * 16, tempY1 * 16);
        }

    }
}
