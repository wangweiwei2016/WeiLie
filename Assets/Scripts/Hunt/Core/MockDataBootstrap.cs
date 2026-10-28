using System.Collections.Generic;
using UnityEngine;
using Hunt.Config;

namespace Hunt.Core
{
    /// <summary>
    /// Mock 配置数据注入器
    /// [RuntimeInitializeOnLoadMethod] 自动跑，Play 即有数据
    /// 正式项目替换为 Addressables / CDN 加载
    /// </summary>
    public static class MockDataBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Init()
        {
            // 避免重复注入（Editor 域重载等情况）
            if (ConfigLoader.Instance.AllMonsterCfgs.Count > 0) return;

            InjectMonsterCfgs();
            InjectMultipleCfgs();
            InjectDigimonRateCfgs();
            InjectLineCfgs();
            InjectScoreRewardCfgs();
            InjectRankRewardCfgs();

            Debug.Log("[MockDataBootstrap] 围猎 Mock 配置已注入");
        }

        private static void InjectMonsterCfgs()
        {
            ConfigLoader.Instance.AllMonsterCfgs = new List<HuntMonsterCfg>
            {
                new HuntMonsterCfg { id = 101, type = 1, scale = 1.0f, killRate = 2000, score = 100,
                    dropNumber = "0#1", dropWeight = "80#20", drop = "9001_1#8001_10" },
                new HuntMonsterCfg { id = 102, type = 2, scale = 1.5f, killRate = 1000, score = 300,
                    dropNumber = "0#1#2", dropWeight = "50#40#10", drop = "8002_5#8003_1" },
                new HuntMonsterCfg { id = 103, type = 3, scale = 0.8f, killRate = 5000, score = 50,
                    dropNumber = "0", dropWeight = "1", drop = "" },
                new HuntMonsterCfg { id = 104, type = 2, scale = 2.0f, killRate = 300, score = 1000,
                    dropNumber = "1#2", dropWeight = "70#30", drop = "8004_1#8005_1" },
            };
        }

        private static void InjectMultipleCfgs()
        {
            ConfigLoader.Instance.AllMultipleCfgs = new List<HuntMultipleCfg>
            {
                new HuntMultipleCfg { id = 1, multiple = 1,   name = "新手猎场",
                    monsList = "101,103", MaxNum = "5,8", MinNum = "2,3", BornWeight = "7,3",
                    BornNumArea = "1_2,2_4", unlock = 0, showMonster = 101,
                    platform = "Res/hunt/platform_1", numBG = "Res/hunt/bg_1" },
                new HuntMultipleCfg { id = 2, multiple = 10,  name = "普通猎场",
                    monsList = "101,102,103", MaxNum = "5,3,8", MinNum = "2,1,3", BornWeight = "5,2,3",
                    BornNumArea = "1_2,1_1,2_4", unlock = 500, showMonster = 102,
                    platform = "Res/hunt/platform_2", numBG = "Res/hunt/bg_2" },
                new HuntMultipleCfg { id = 3, multiple = 50,  name = "进阶猎场",
                    monsList = "101,102,103,104", MaxNum = "5,3,8,1", MinNum = "2,1,3,0", BornWeight = "4,3,2,1",
                    BornNumArea = "1_2,1_2,2_4,1_1", unlock = 5000, showMonster = 104,
                    platform = "Res/hunt/platform_3", numBG = "Res/hunt/bg_3" },
                new HuntMultipleCfg { id = 4, multiple = 100, name = "传说猎场",
                    monsList = "102,104", MaxNum = "3,2", MinNum = "1,0", BornWeight = "6,4",
                    BornNumArea = "1_2,1_1", unlock = 20000, showMonster = 104,
                    platform = "Res/hunt/platform_4", numBG = "Res/hunt/bg_4" },
            };
        }

        private static void InjectDigimonRateCfgs()
        {
            ConfigLoader.Instance.AllDigimonRateCfgs = new List<HuntDigimonRateCfg>
            {
                new HuntDigimonRateCfg { id = 1, unitQuality = 1, killRate = 100 },
                new HuntDigimonRateCfg { id = 2, unitQuality = 2, killRate = 300 },
                new HuntDigimonRateCfg { id = 3, unitQuality = 3, killRate = 800 },
                new HuntDigimonRateCfg { id = 4, unitQuality = 4, killRate = 1500 },
                new HuntDigimonRateCfg { id = 5, unitQuality = 5, killRate = 3000 },
                new HuntDigimonRateCfg { id = 6, unitQuality = 6, killRate = 5000 },
            };
        }

        private static void InjectLineCfgs()
        {
            ConfigLoader.Instance.AllLineCfgs = new List<HuntLineCfg>
            {
                new HuntLineCfg { id = 1, type = 1, life = 21, speed = 5,
                    line = "0_39#50_30#100_40#150_35#200_50#250_45#300_55#350_50#400_45#450_30#480_40" },
                new HuntLineCfg { id = 2, type = 1, life = 21, speed = 5,
                    line = "480_30#430_40#380_35#330_50#280_55#230_45#180_40#130_50#80_35#30_30#0_40" },
                new HuntLineCfg { id = 3, type = 2, life = 15, speed = 3,
                    line = "0_100#100_110#200_90#300_110#400_100#480_110" },
                new HuntLineCfg { id = 4, type = 2, life = 15, speed = 3,
                    line = "0_200#100_190#200_210#300_190#400_210#480_200" },
                new HuntLineCfg { id = 5, type = 3, life = 30, speed = 8,
                    line = "0_250#120_240#240_260#360_250#480_260" },
                new HuntLineCfg { id = 6, type = 3, life = 30, speed = 8,
                    line = "480_250#360_260#240_240#120_260#0_250" },
                new HuntLineCfg { id = 7, type = 4, life = 20, speed = 2,
                    line = "0_150#240_160#480_150" },
            };
        }

        private static void InjectScoreRewardCfgs()
        {
            ConfigLoader.Instance.AllScoreRewardCfgs = new List<ActHuntScoreRewardCfg>
            {
                new ActHuntScoreRewardCfg { id = 1, desc = "首次奖励", score = 500,   reward = "8001_5" },
                new ActHuntScoreRewardCfg { id = 2, desc = "成长奖励", score = 2000,  reward = "8001_20#8002_2" },
                new ActHuntScoreRewardCfg { id = 3, desc = "精英奖励", score = 5000,  reward = "8001_50#8003_5" },
                new ActHuntScoreRewardCfg { id = 4, desc = "大师奖励", score = 10000, reward = "8004_10" },
            };
        }

        private static void InjectRankRewardCfgs()
        {
            ConfigLoader.Instance.AllRankRewardCfgs = new List<ActHuntRankRewardCfg>
            {
                new ActHuntRankRewardCfg { id = 1, type = 1, rankNumb = "1#1",  reward = "8004_100" },
                new ActHuntRankRewardCfg { id = 2, type = 1, rankNumb = "2#10", reward = "8004_50" },
                new ActHuntRankRewardCfg { id = 3, type = 1, rankNumb = "11#50", reward = "8003_20" },
                new ActHuntRankRewardCfg { id = 4, type = 2, rankNumb = "1#1",  reward = "8004_500" },
                new ActHuntRankRewardCfg { id = 5, type = 2, rankNumb = "2#10", reward = "8004_200" },
                new ActHuntRankRewardCfg { id = 6, type = 2, rankNumb = "11#100", reward = "8003_50" },
                new ActHuntRankRewardCfg { id = 7, type = 3, rankNumb = "1#1",  reward = "8004_50" },
                new ActHuntRankRewardCfg { id = 8, type = 3, rankNumb = "2#10", reward = "8004_20" },
                new ActHuntRankRewardCfg { id = 9, type = 4, rankNumb = "1#1",  reward = "8004_300" },
                new ActHuntRankRewardCfg { id = 10, type = 4, rankNumb = "2#10", reward = "8004_100" },
            };
        }
    }
}
