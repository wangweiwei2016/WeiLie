using System;

namespace Hunt.Config
{
    /// <summary>
    /// 围猎数码兽增加概率配置表（本地）
    /// </summary>
    [Serializable]
    public class HuntDigimonRateCfg
    {
        public int id;          // 配置唯一ID
        public int unitQuality; // 对应 DigimonCfg.quality
        public int killRate;    // 数码兽品质提供的额外击杀概率 万分比
    }
}
