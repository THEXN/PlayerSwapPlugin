using Microsoft.Xna.Framework;
using System.Timers;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;


namespace PlayerSwapPlugin
{
    [ApiVersion(2, 1)]
    public class PlayerSwapPlugin : TerrariaPlugin
    {
        private System.Timers.Timer timer;
        private Random random;
        public override string Author => "肝帝熙恩,少司命";
        public override string Description => "一个插件，用于在指定时间后随机交换玩家位置。";
        public override string Name => "PlayerSwapPlugin";
        public override Version Version => new Version(1, 0, 5);
        public static Configuration Config;
        // 在 PlayerSwapPlugin 类中定义一个实例字段
        private System.Threading.Timer broadcastTimer;

        public PlayerSwapPlugin(Main game) : base(game)
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
            args.Player?.SendSuccessMessage("[{0}] 重新加载配置完毕。", typeof(PlayerSwapPlugin).Name);
            // 在 ReloadConfig 方法中更新 broadcastTimer 的状态
            this.broadcastTimer.Change(0, 1000);
        }

        // 在方法外面定义一个TimerState类，用于存储倒计时的状态
        public class TimerState
        {
            public int Countdown { get; set; } // 倒计时的秒数
            public int Threshold { get; set; } // 广播的阈值
            public TimerState(int countdown, int threshold)
            {
                Countdown = countdown;
                Threshold = threshold;
            }
        }
        public override void Initialize()
        {
            GeneralHooks.ReloadEvent += ReloadConfig;
            random = new Random();
            timer = new System.Timers.Timer();
            timer.Interval = TimeSpan.FromSeconds(Config.IntervalSeconds).TotalMilliseconds;
            timer.AutoReset = true;
            timer.Elapsed += Timer_Elapsed;
            if (Config.pluginEnabled) // 检查插件是否启用
            {
                timer.Start(); // 如果启用，则启动定时器
            }

            TShockAPI.Commands.ChatCommands.Add(new Command("swapplugin.toggle", SwapToggle, "swaptoggle", "随机互换"));
            // 在 Initialize 方法中创建和初始化 broadcastTimer
            broadcastTimer = new System.Threading.Timer(BroadcastMessage, new TimerState(Config.IntervalSeconds, Config.BroadcastRemainingTimeThreshold), Timeout.Infinite, 1000);
        }


        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var players = TShock.Players.Where(p => p != null && p.Active).ToList();
            // 获取没有特定权限的玩家列表
            var eligiblePlayers = players.Where(p => !p.HasPermission("noPlayerSwap")).ToList();

            if (eligiblePlayers.Count < 2)
                return;

            if (!Config.pluginEnabled)
                return;

            SwapPlayers(); // 执行交换玩家位置的操作
            timer.Interval = TimeSpan.FromSeconds(Config.IntervalSeconds).TotalMilliseconds;
            // 重置System.Threading.Timer对象，让它开始倒计时
            broadcastTimer.Change(0, 1000);
        }//倒计时逻辑

        private void BroadcastMessage(object state)
        {
            // 将state参数转换为TimerState对象
            TimerState timerState = state as TimerState;
            // 判断倒计时是否小于或等于阈值
            if (timerState.Countdown <= timerState.Threshold && Config.BroadcastRemainingTimeEnabled)
            {
                // 向所有玩家发送一条消息，格式化倒计时
                TShockAPI.TSPlayer.All.SendMessage($"注意：还有{timerState.Countdown}秒就要交换位置了！", Color.Yellow);
            }
            // 判断倒计时是否等于0
            if (timerState.Countdown == 0)
            {
                // 停止计时器
                broadcastTimer.Change(Timeout.Infinite, 1000);
                // 重置倒计时
                timerState.Countdown = Config.IntervalSeconds;
                // 发送一条不同的消息，表示已经交换了所有玩家的位置
                if (Config.BroadcastPlayerSwapEnabled)
                {
                    TShockAPI.TSPlayer.All.SendMessage($"已经交换所有玩家位置！", Color.Green);
                }
            }
            else
            {
                // 将倒计时减1
                timerState.Countdown--;
            }
        }//倒计时广播

        private void SwapPlayers()
        {
            var players = TShock.Players.Where(p => p != null && p.Active).ToList();
            // 获取没有特定权限的玩家列表
            var eligiblePlayers = players.Where(p => !p.HasPermission("noPlayerSwap")).ToList();

            if (eligiblePlayers.Count < 2)
                return;

            if (Config.MultiPlayerMode)
            {
                // 多人打乱模式逻辑
                PlayerRandPos3(eligiblePlayers);
            }
            else
            {
                // 双人模式逻辑
                SwapTwoPlayers(eligiblePlayers);

            }
        }//根据配置文件选择模式

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
                TShockAPI.TSPlayer.All.SendMessage($"玩家 {player1.Name} 和玩家 {player2.Name} 交换了位置！", Color.Green);
            }
        }//仅交换双人模式
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
        }//双人模式,交换逻辑

        public void PlayerRandPos3(List<TSPlayer> players)
        {
            // 接受一个玩家列表作为参数，而不是直接获取所有玩家
            if (players.Count < 2)
                return;

            // 随机打乱玩家列表的顺序
            var sp = players.OrderBy(c => Guid.NewGuid()).ToList();

            // 用一个for循环，每次取出两个玩家，交换他们的位置
            for (var i = 1; i < players.Count; i++)
            {
                (sp[i - 1].TPlayer.position, sp[i].TPlayer.position) =
                (sp[i].TPlayer.position, sp[i - 1].TPlayer.position);
            }

            // 对每个玩家，传送他们到新的位置
            foreach (var player in players)
            {
                player.Teleport(player.TPlayer.position.X, player.TPlayer.position.Y);
            }
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

    }
}
