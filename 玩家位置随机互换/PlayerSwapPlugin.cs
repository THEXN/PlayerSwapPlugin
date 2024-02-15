using System;
using System.Linq;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using System.Timers;
using TShockAPI.Hooks;

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
            timer.Interval = TimeSpan.FromSeconds(Config.times).TotalMilliseconds; // 将此值更改为所需的间隔时间（以秒为单位）
            timer.AutoReset = true; // 设置为 false，以确保在一次触发后不会自动重置
            timer.Elapsed += Timer_Elapsed;
            timer.Start();

            TShockAPI.Commands.ChatCommands.Add(new Command("swapplugin.toggle", SwapToggle, "swaptoggle","更改随机互换"));
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!pluginEnabled)
                return;

            SwapPlayers();

            // 重置计时器间隔，延迟一段时间后再次交换
            timer.Interval = TimeSpan.FromSeconds(Config.times).TotalMilliseconds; // 20 秒后再次交换
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

            int index1 = random.Next(eligiblePlayers.Count);
            int index2 = random.Next(eligiblePlayers.Count);

            while (!Config.allowSamePlayerSwap && index2 == index1)
            {
                index2 = random.Next(eligiblePlayers.Count);
            }

            var player1 = eligiblePlayers[index1];
            var player2 = eligiblePlayers[index2];

            // 检查是否为同一玩家
            if (player1.Name == player2.Name)
            {
                player1.SendInfoMessage("你尝试和自己交换位置，但没有发生任何变化。");
                return;
            }

            SwapPositions(player1, player2);

            // 发送交换提示
            player1.SendInfoMessage($"你已与玩家 {player2.Name} 交换了位置！");
            player2.SendInfoMessage($"你已与玩家 {player1.Name} 交换了位置！");

            // 重置计时器
            timer.Start();
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
            GeneralHooks.ReloadEvent += ReloadConfig;
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
            pluginEnabled = !pluginEnabled;
            args.Player.SendSuccessMessage($"随机位置互换已{(pluginEnabled ? "启用" : "禁用")}。");
        }
    }
}
