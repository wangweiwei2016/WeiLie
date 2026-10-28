using System.Collections.Generic;
using Hunt.Model;

namespace Hunt.Core
{
    /// <summary>
    /// 玩家活动数据轻量封装（对接玩家基础系统前的 Mock 层）
    /// </summary>
    public class PlayerDataManager
    {
        public static PlayerDataManager Instance { get; } = new PlayerDataManager();

        private readonly Dictionary<long, HuntPlayer> _players = new Dictionary<long, HuntPlayer>();
        private long _currentPlayerId;

        public long CurrentPlayerId => _currentPlayerId;

        public HuntPlayer GetPlayer(long playerId)
            => _players.TryGetValue(playerId, out var p) ? p : null;

        public void SetCurrentPlayer(long playerId, string nickname = "", string avatar = "")
        {
            _currentPlayerId = playerId;
            EnsurePlayer(playerId, nickname, avatar);
        }

        public void EnsurePlayer(long playerId, string nickname = "", string avatar = "")
        {
            if (!_players.ContainsKey(playerId))
            {
                _players[playerId] = new HuntPlayer
                {
                    PlayerId = playerId,
                    Nickname = nickname,
                    Avatar = avatar,
                    JoinTime = ServerTime.NowMs
                };
            }
        }

        public void SelectDigimon(long playerId, int digimonId, int quality)
        {
            if (!_players.TryGetValue(playerId, out var p)) return;
            p.DigimonId = digimonId;
            p.DigimonQuality = quality;
        }

        public int GetCurrentDigimonQuality(long playerId)
            => _players.TryGetValue(playerId, out var p) ? p.DigimonQuality : 0;

        public bool ConsumeTokens(long playerId, int cost)
            => ScoreManager.Instance.ConsumeTokens(playerId, cost);
    }
}
