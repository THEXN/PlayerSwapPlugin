using System;
using System.IO;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using TShockAPI;

namespace PlayerSwapPlugin
{
    public class Configuration
    {
        public static readonly string FilePath = Path.Combine(TShock.SavePath, "玩家位置随机互换.json");
        [JsonProperty("允许玩家和自己传送")]
        public bool allowSamePlayerSwap = true;
        [JsonProperty("传送间隔秒")]
        public int times = 300;
        public bool broadcastRemainingTimeEnabled = true;
        public int broadcastRemainingTimeThreshold = 10; // 剩余传送时间小于等于10秒时广播
        public bool broadcastPlayerSwapEnabled = true;

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
