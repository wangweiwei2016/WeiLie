using System;
using System.Collections.Generic;
using Hunt.Config;

namespace Hunt.Core
{
    /// <summary>
    /// 结算管理器 - 活动结束时的最终结算
    /// （实际在服务端完成，客户端仅接收结果）
    /// </summary>
    public class SettlementManager
    {
        public static SettlementManager Instance { get; } = new SettlementManager();

        public event Action OnSettlementComplete;

        public void DoSettlement()
        {
            // 1. 冻结最终分数（客户端做本地快照）
            // 2. 计算最终排名（客户端本地模拟）
            foreach (HuntRankType t in System.Enum.GetValues(typeof(HuntRankType)))
                RankManager.Instance.RequestRank((int)t);

            // 3. 补发未领取积分奖励（邮件）- 客户端提示用

            OnSettlementComplete?.Invoke();
        }

        public void Finalize()
        {
            DoSettlement();
        }

        /// <summary>
        /// 按 rankNumb 区间匹配排名奖励
        /// 返回: rankType -> (minRank, maxRank) -> 奖励描述
        /// </summary>
        public List<(int rankType, int start, int end, string reward)>
            MatchRankRewards(int rankType, int myRank)
        {
            var rewards = ConfigLoader.Instance.GetRankRewards(rankType);
            var result = new List<(int, int, int, string)>();
            foreach (var rw in rewards)
            {
                var parts = rw.rankNumb.Split('#');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out var start) &&
                    int.TryParse(parts[1], out var end) &&
                    myRank >= start && myRank <= end)
                    result.Add((rankType, start, end, rw.reward));
            }
            return result;
        }
    }
}
