using System;
using System.Collections.Generic;

namespace Hunt.Core
{
    /// <summary>
    /// 积分 & 代币 & 排行榜数据管理器
    /// 服务端权威 - 客户端做本地缓存/模拟
    /// </summary>
    public class ScoreManager
    {
        public static ScoreManager Instance { get; } = new ScoreManager();

        private readonly Dictionary<long, int> _personalScores = new Dictionary<long, int>();
        private readonly Dictionary<long, int> _teamScores     = new Dictionary<long, int>();
        private readonly Dictionary<long, int> _totalTokens   = new Dictionary<long, int>();
        private readonly Dictionary<long, int> _currentTokens = new Dictionary<long, int>();

        public event Action<long, int> OnPersonalScoreChanged;  // playerId, newScore
        public event Action<long, int> OnTeamScoreChanged;      // teamId, newScore

        public void OnHuntingStart()
        {
            _personalScores.Clear();
            _teamScores.Clear();
        }

        public int GetPersonalScore(long playerId)
            => _personalScores.TryGetValue(playerId, out var s) ? s : 0;

        public int GetTeamScore(long teamId)
            => _teamScores.TryGetValue(teamId, out var s) ? s : 0;

        public int GetTeamScoreByPlayer(long playerId)
        {
            var team = TeamManager.Instance.GetPlayerTeam(playerId);
            return team == null ? 0 : GetTeamScore(team.TeamId);
        }

        public int GetTotalTokensEarned(long playerId)
            => _totalTokens.TryGetValue(playerId, out var t) ? t : 0;

        public int GetCurrentTokens(long playerId)
            => _currentTokens.TryGetValue(playerId, out var t) ? t : 0;

        public void AddPlayerScore(long playerId, int delta)
        {
            int cur = GetPersonalScore(playerId) + delta;
            _personalScores[playerId] = cur;
            OnPersonalScoreChanged?.Invoke(playerId, cur);
        }

        public void AddTeamScore(long playerId, int delta)
        {
            var team = TeamManager.Instance.GetPlayerTeam(playerId);
            if (team == null) return;
            int cur = GetTeamScore(team.TeamId) + delta;
            _teamScores[team.TeamId] = cur;
            OnTeamScoreChanged?.Invoke(team.TeamId, cur);
        }

        /// <summary>增加累计获得代币（同时增加当前余额）</summary>
        public void AddTokens(long playerId, int delta)
        {
            _totalTokens[playerId]   = GetTotalTokensEarned(playerId) + delta;
            _currentTokens[playerId] = GetCurrentTokens(playerId) + delta;
        }

        public bool ConsumeTokens(long playerId, int cost)
        {
            int cur = GetCurrentTokens(playerId);
            if (cur < cost) return false;
            _currentTokens[playerId] = cur - cost;
            return true;
        }

        // ============ 排行榜（客户端本地模拟）============

        public List<(long id, int score)> GetPersonalRank()
        {
            var list = new List<(long, int)>();
            foreach (var kv in _personalScores)
                list.Add((kv.Key, kv.Value));
            list.Sort((a, b) => b.Item2.CompareTo(a.Item2));
            return list;
        }

        public List<(long teamId, int score)> GetTeamRank()
        {
            var list = new List<(long, int)>();
            foreach (var kv in _teamScores)
                list.Add((kv.Key, kv.Value));
            list.Sort((a, b) => b.Item2.CompareTo(a.Item2));
            return list;
        }
    }
}
