using System;
using System.Collections.Generic;
using Hunt.Config;

namespace Hunt.Core
{
    /// <summary>
    /// 排行榜管理器
    /// 客户端：请求→缓存→UI 刷新；实际排名由服务端下发
    /// </summary>
    public class RankManager
    {
        public static RankManager Instance { get; } = new RankManager();

        // rankType -> 列表（客户端本地模拟）
        private readonly Dictionary<int, List<RankEntry>> _rankCache = new();

        public event Action<int> OnRankUpdated;

        public void RequestRank(int rankType)
        {
            // TODO: 服务端请求接口
            RefreshLocalRank(rankType);
        }

        public List<RankEntry> GetRank(int rankType)
        {
            return _rankCache.TryGetValue(rankType, out var list)
                ? list : new List<RankEntry>();
        }

        public RankEntry GetMyRank(int rankType, long myId)
        {
            var list = GetRank(rankType);
            for (int i = 0; i < list.Count; i++)
                if (list[i].Id == myId) return list[i];
            return null;
        }

        private void RefreshLocalRank(int rankType)
        {
            var list = new List<RankEntry>();

            switch ((HuntRankType)rankType)
            {
                case HuntRankType.TeamServer:
                case HuntRankType.TeamAllServer:
                    foreach (var (teamId, score) in ScoreManager.Instance.GetTeamRank())
                        list.Add(new RankEntry { Id = teamId, Score = score });
                    break;
                case HuntRankType.PersonalServer:
                case HuntRankType.PersonalAllServer:
                    foreach (var (playerId, score) in ScoreManager.Instance.GetPersonalRank())
                        list.Add(new RankEntry { Id = playerId, Score = score });
                    break;
            }

            int rank = 1;
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0 && list[i].Score != list[i - 1].Score) rank = i + 1;
                list[i].Rank = rank;
            }

            _rankCache[rankType] = list;
            OnRankUpdated?.Invoke(rankType);
        }

        /// <summary>按 rankNumb 区间匹配排名奖励描述</summary>
        public string GetRankRewardDesc(int rankType, int myRank)
        {
            var rewards = ConfigLoader.Instance.GetRankRewards(rankType);
            foreach (var rw in rewards)
            {
                var parts = rw.rankNumb.Split('#');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out var start) &&
                    int.TryParse(parts[1], out var end) &&
                    myRank >= start && myRank <= end)
                    return rw.reward;
            }
            return "";
        }
    }

    public class RankEntry
    {
        public long Id;    // PlayerId 或 TeamId
        public int Score;
        public int Rank;
    }
}
