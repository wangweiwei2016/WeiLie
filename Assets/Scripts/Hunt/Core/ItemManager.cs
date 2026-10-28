using System;
using System.Collections.Generic;

namespace Hunt.Core
{
    /// <summary>
    /// 活动内道具持有/消耗/初始发放
    /// （正式项目对接背包系统，此处为轻量独立实现）
    /// </summary>
    public class ItemManager
    {
        public static ItemManager Instance { get; } = new ItemManager();

        // PlayerId -> ItemId -> Count
        private readonly Dictionary<long, Dictionary<int, int>> _playerItems = new();

        public event Action<long, int, int> OnItemChanged; // playerId, itemId, newCount

        public void AddItem(long playerId, int itemId, int count)
        {
            var dict = GetOrCreateDict(playerId);
            dict[itemId] = dict.GetValueOrDefault(itemId, 0) + count;
            OnItemChanged?.Invoke(playerId, itemId, dict[itemId]);
        }

        public bool ConsumeItem(long playerId, int itemId, int count = 1)
        {
            var dict = GetOrCreateDict(playerId);
            int cur = dict.GetValueOrDefault(itemId, 0);
            if (cur < count) return false;
            dict[itemId] = cur - count;
            OnItemChanged?.Invoke(playerId, itemId, dict[itemId]);
            return true;
        }

        public int GetItemCount(long playerId, int itemId)
        {
            if (!_playerItems.TryGetValue(playerId, out var dict)) return 0;
            return dict.GetValueOrDefault(itemId, 0);
        }

        /// <summary>活动开始时按 initItems 配置发放初始道具</summary>
        public void GrantInitialItems(long playerId)
        {
            var globalCfg = HuntActivityManager.Instance.GlobalCfg;
            if (globalCfg == null || string.IsNullOrEmpty(globalCfg.initItems)) return;

            foreach (var pair in globalCfg.initItems.Split('#'))
            {
                var parts = pair.Split('_');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out var id) &&
                    int.TryParse(parts[1], out var cnt))
                    AddItem(playerId, id, cnt);
            }
        }

        private Dictionary<int, int> GetOrCreateDict(long playerId)
        {
            if (!_playerItems.TryGetValue(playerId, out var dict))
            {
                dict = new Dictionary<int, int>();
                _playerItems[playerId] = dict;
            }
            return dict;
        }
    }
}
