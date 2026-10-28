using System;
using System.Collections.Generic;
using Hunt.Config;

namespace Hunt.Core
{
    /// <summary>
    /// 积分奖励管理器
    /// - 普通奖励：团队累计积分解锁档位，各队员独立领取
    /// - 循环奖励：达到最高档后每 finalCycleScore 分无限循环
    /// </summary>
    public class ScoreRewardManager
    {
        public static ScoreRewardManager Instance { get; } = new ScoreRewardManager();

        // PlayerId -> 已领取普通档位 Id 集合
        private readonly Dictionary<long, HashSet<int>> _claimedNormal   = new();
        // PlayerId -> 已领取循环轮次集合（轮次从1开始）
        private readonly Dictionary<long, HashSet<int>> _claimedCycles   = new();

        public event Action<long> OnRewardClaimed;

        // ============ 查询 ============

        public List<int> GetClaimedNormalSet(long playerId)
        {
            if (!_claimedNormal.TryGetValue(playerId, out var set))
            {
                set = new HashSet<int>();
                _claimedNormal[playerId] = set;
            }
            return new List<int>(set);
        }

        public HashSet<int> GetClaimedCycles(long playerId)
        {
            if (!_claimedCycles.TryGetValue(playerId, out var set))
            {
                set = new HashSet<int>();
                _claimedCycles[playerId] = set;
            }
            return set;
        }

        public List<ActHuntScoreRewardCfg> GetClaimableRewards(long playerId)
        {
            var teamScore = ScoreManager.Instance.GetTeamScoreByPlayer(playerId);
            var claimed   = GetClaimedNormalSet(playerId);
            var all       = ConfigLoader.Instance.GetScoreRewards();
            var result    = new List<ActHuntScoreRewardCfg>();

            foreach (var rw in all)
                if (teamScore >= rw.score && !claimed.Contains(rw.id))
                    result.Add(rw);

            return result;
        }

        public int GetMaxNormalRewardScore()
        {
            var all = ConfigLoader.Instance.GetScoreRewards();
            int max = 0;
            foreach (var rw in all) if (rw.score > max) max = rw.score;
            return max;
        }

        /// <summary>
        /// 循环奖励进度：(已完成轮次数, 当前轮次进度0~1)
        /// 未进入循环时返回 (0, 0)
        /// </summary>
        public (int completed, float progress) GetCycleProgress(long playerId)
        {
            var globalCfg = HuntActivityManager.Instance.GlobalCfg;
            if (globalCfg == null || globalCfg.finalCycleScore <= 0) return (0, 0);

            int teamScore = ScoreManager.Instance.GetTeamScoreByPlayer(playerId);
            int maxNormal = GetMaxNormalRewardScore();
            if (teamScore <= maxNormal) return (0, 0);

            int over         = teamScore - maxNormal;
            int completed    = over / globalCfg.finalCycleScore;
            float progress   = (float)(over % globalCfg.finalCycleScore) / globalCfg.finalCycleScore;
            return (completed, progress);
        }

        // ============ 领取 ============

        public (bool success, string reason) ClaimNormalReward(long playerId, int rewardId)
        {
            var all = ConfigLoader.Instance.GetScoreRewards();
            var cfg = all.Find(r => r.id == rewardId);
            if (cfg == null) return (false, "奖励不存在");

            int teamScore = ScoreManager.Instance.GetTeamScoreByPlayer(playerId);
            if (teamScore < cfg.score) return (false, "未达成条件");

            var claimed = GetClaimedNormalSet(playerId);
            if (claimed.Contains(rewardId)) return (false, "已领取");

            // TODO: 服务端发奖（道具入库）- 客户端只更新本地状态
            OnRewardClaimed?.Invoke(playerId);
            return (true, "领取成功");
        }

        public (bool success, string reason) ClaimCycleReward(long playerId, int round)
        {
            var globalCfg = HuntActivityManager.Instance.GlobalCfg;
            if (globalCfg == null) return (false, "配置缺失");

            int maxNormal = GetMaxNormalRewardScore();
            int needScore = maxNormal + round * globalCfg.finalCycleScore;
            int teamScore = ScoreManager.Instance.GetTeamScoreByPlayer(playerId);
            if (teamScore < needScore) return (false, "未达成条件");

            var cycles = GetClaimedCycles(playerId);
            if (!cycles.Add(round)) return (false, "已领取");

            OnRewardClaimed?.Invoke(playerId);
            return (true, "领取成功");
        }
    }
}
