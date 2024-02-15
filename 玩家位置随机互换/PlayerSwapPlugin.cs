using System;
using System.Linq;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using System.Threading;
using TShockAPI.Hooks;
using Microsoft.Xna.Framework;

namespace PlayerSwapPlugin
{
    [ApiVersion(2, 1)]
    public class PlayerSwapPlugin : TerrariaPlugin
    {
        private System.Threading.Timer timer;
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


        private bool isCountingDown = false;

        private long lastSwapTime = 0; // 上次交换的时间

        public override void Initialize()
        {
            GeneralHooks.ReloadEvent += ReloadConfig;
            random = new Random();
            // 创建一个定时器，指定回调方法、状态对象、延迟时间和周期时间
            timer = new System.Threading.Timer(Timer_Elapsed, null, TimeSpan.FromSeconds(Config.IntervalSeconds), TimeSpan.FromMilliseconds(-1));
            TShockAPI.Commands.ChatCommands.Add(new Command("swapplugin.toggle", SwapToggle, "swaptoggle", "更改随机互换"));
        }

        private void Timer_Elapsed(object state)
        {
            if (!pluginEnabled)
                return;
            SwapPlayers(); // 执行交换玩家位置的操作
                           // 更新 lastSwapTime 为当前时间
            lastSwapTime = DateTime.Now.Ticks / TimeSpan.TicksPerSecond;
            // 计算剩余时间
            long currentTime = DateTime.Now.Ticks / TimeSpan.TicksPerSecond;
            long elapsedTime = currentTime - lastSwapTime;
            int remainingTime = Config.IntervalSeconds - (int)elapsedTime;
            TShock.Log.ConsoleInfo($"1处剩余时间：{remainingTime}秒");

            if (Config.BroadcastRemainingTimeEnabled)
            {
                TShock.Log.ConsoleInfo($"3处剩余时间：{remainingTime}秒");
                if (remainingTime <= Config.BroadcastRemainingTimeThreshold && remainingTime >= 0)
                {
                    // 在一个新的任务中执行异步方法
                    Task.Run(async () =>
                    {
                        await StartCountdown(remainingTime);
                        // 释放定时器
                        timer.Dispose();
                    });
                }
            }
        }




        private async Task StartCountdown(int remainingTime)
        {
            while (remainingTime > 0)
            {
                TShock.Log.ConsoleInfo($"剩余时间：{remainingTime}秒");
                await Task.Delay(1000);
                remainingTime--;
            }
        }


        private void BroadcastRemainingTime(int remainingTime)
        {
            if (Config.BroadcastRemainingTimeEnabled)
            {
                TShock.Utils.Broadcast($"剩余传送时间：{remainingTime}秒", Color.Yellow);
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
                // 更新 lastSwapTime
                lastSwapTime = DateTime.Now.Ticks / TimeSpan.TicksPerSecond;

                // 多人打乱模式逻辑
                PlayerRandPos();
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

        private void PlayerRandPos()
        {
            var players = TShock.Players.Where(p => p != null && p.Active && !p.Dead).ToList();
            if (players.Count() > 1)
            {
                var pos = SpwanPlayerPos(players);
                for (var i = 0; i < players.Count(); i++)
                {
                    var player = players[i];
                    var vec = pos[i];
                    player.Teleport(vec.X, vec.Y);
                }
            }
        }

        private List<Microsoft.Xna.Framework.Vector2> SpwanPlayerPos(IEnumerable<TSPlayer> players)
        {
            List<Microsoft.Xna.Framework.Vector2> v = new();
            var playerPos = players.Select(p => p.TPlayer.position).ToList();
            players.ForEach(p =>
            {
                var pos = playerPos.OrderBy(x => Guid.NewGuid()).FirstOrDefault(x => x != p.TPlayer.position);
                if (pos == Microsoft.Xna.Framework.Vector2.Zero)
                {
                    v = SpwanPlayerPos(players);
                }
                else
                {
                    v.Add(pos);
                    playerPos.Remove(pos);
                }

            });
            return v;
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
                    // 删除这一行
                    // timer.Stop();
                    // 只保留这一行
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
