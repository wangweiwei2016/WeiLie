using System;
using System.Collections.Generic;
using Hunt.Config;

namespace Hunt.Core
{
    /// <summary>
    /// 配置加载器 - 对外统一配置查询入口
    /// 正式项目从 CDN / Addressables / Resources 加载后注入
    /// </summary>
    public class ConfigLoader
    {
        public static ConfigLoader Instance { get; } = new ConfigLoader();

        public List<HuntMonsterCfg> AllMonsterCfgs       = new();
        public List<HuntMultipleCfg> AllMultipleCfgs    = new();
        public List<HuntDigimonRateCfg> AllDigimonRateCfgs = new();
        public List<HuntLineCfg> AllLineCfgs            = new();
        public List<ActHuntScoreRewardCfg> AllScoreRewardCfgs = new();
        public List<ActHuntRankRewardCfg> AllRankRewardCfgs   = new();

        public HuntMonsterCfg GetMonsterCfg(int id)
            => AllMonsterCfgs.Find(c => c.id == id);

        public HuntMultipleCfg GetMultipleCfg(int id)
            => AllMultipleCfgs.Find(c => c.id == id);

        public int GetDigimonKillRate(int unitQuality)
        {
            var cfg = AllDigimonRateCfgs.Find(c => c.unitQuality == unitQuality);
            return cfg?.killRate ?? 0;
        }

        public HuntLineCfg GetLineCfg(int id)
            => AllLineCfgs.Find(c => c.id == id);

        public List<HuntLineCfg> GetLinesByType(int type)
            => AllLineCfgs.FindAll(c => c.type == type);

        public List<ActHuntScoreRewardCfg> GetScoreRewards() => AllScoreRewardCfgs;

        public List<ActHuntRankRewardCfg> GetRankRewards(int rankType)
            => AllRankRewardCfgs.FindAll(c => c.type == rankType);

        /// <summary>解锁进度 0-1</summary>
        public float GetUnlockProgress(int multipleId, int totalTokens)
        {
            var cfg = GetMultipleCfg(multipleId);
            if (cfg == null || cfg.unlock <= 0) return 1f;
            return Math.Min(1f, (float)totalTokens / cfg.unlock);
        }

        public bool IsUnlocked(int multipleId, int totalTokens)
        {
            var cfg = GetMultipleCfg(multipleId);
            return cfg != null && totalTokens >= cfg.unlock;
        }

        public static string FormatMultiple(int multiple) => $"{multiple}x";
    }
}
