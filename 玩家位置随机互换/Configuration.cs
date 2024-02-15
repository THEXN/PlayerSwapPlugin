using System;
using System.IO;
using Newtonsoft.Json;
using TShockAPI;

namespace PlayerSwapPlugin
{
    public class Configuration
    {
        public static readonly string FilePath = Path.Combine(TShock.SavePath, "玩家位置随机互换配置.json");

        [JsonProperty("传送间隔秒")]
        public int IntervalSeconds { get; set; } = 10;

        [JsonProperty("双人模式允许玩家和自己交换")]
        public bool AllowSamePlayerSwap { get; set; } = true;

        [JsonProperty("多人打乱模式")]
        public bool MultiPlayerMode { get; set; } = false;

        [JsonProperty("广播剩余传送时间")]
        public bool BroadcastRemainingTimeEnabled { get; set; } = true;

        [JsonProperty("广播交换倒计时阈值（暂时无法热重载）")]
        public int BroadcastRemainingTimeThreshold { get; set; } = 10; // 剩余传送时间小于等于10秒时广播

        [JsonProperty("广播玩家交换位置信息")]
        public bool BroadcastPlayerSwapEnabled { get; set; } = true;



        public void Write(string path)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                var str = JsonConvert.SerializeObject(this, Formatting.Indented);
                using (var sw = new StreamWriter(fs))
                {
                    sw.Write(str);
                }
            }
        }

        public static Configuration Read(string path)
        {
            if (!File.Exists(path))
                return new Configuration();
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var sr = new StreamReader(fs))
                {
                    var cf = JsonConvert.DeserializeObject<Configuration>(sr.ReadToEnd());
                    return cf;
                }
            }
        }
    }
}
