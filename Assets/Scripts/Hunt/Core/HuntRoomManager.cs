using System;
using System.Collections.Generic;
using Hunt.Config;
using Hunt.Model;
using UnityEngine;
using Vector2Int = Hunt.Model.Vector2Int;

namespace Hunt.Core
{
    /// <summary>
    /// 猎场房间管理器
    /// 负责：猎场分配、怪物按规则刷新、怪物路线移动、攻击判定与结算
    /// </summary>
    public class HuntRoomManager
    {
        public static HuntRoomManager Instance { get; } = new HuntRoomManager();

        private readonly Dictionary<int, HuntRoom> _rooms = new Dictionary<int, HuntRoom>();
        private readonly Dictionary<long, int> _playerRoomMap = new Dictionary<long, int>();
        private long _nextMonsterInstanceId = 1;
        private int _nextRoomId = 1;

        public event Action<int> OnMonsterBorn;      // RoomId
        public event Action<int, long> OnMonsterDied; // RoomId, monsterInstanceId
        public event Action<KillResult> OnKillResult;

        public void OnHuntingStart()
        {
            _rooms.Clear();
            _playerRoomMap.Clear();
        }

        // ============ 猎场分配 ============

        public HuntRoom EnterRoom(long playerId, int multipleId, out string errorMsg)
        {
            ScoreManager.Instance.AddTokens(playerId, 10000000);
            errorMsg = "";
            var globalCfg = HuntActivityManager.Instance.GlobalCfg;
            if (globalCfg == null) { errorMsg = "活动未开启"; return null; }

            // 1. 检查累计代币解锁
            int totalTokens = ScoreManager.Instance.GetTotalTokensEarned(playerId);
            if (!ConfigLoader.Instance.IsUnlocked(multipleId, totalTokens))
            {
                errorMsg = "猎场未解锁";
                return null;
            }

            // 2. 优先分配已有空位的同倍率房间
            HuntRoom target = null;
            foreach (var kv in _rooms)
            {
                if (kv.Value.MultipleId == multipleId &&
                    kv.Value.PlayerIds.Count < globalCfg.maxPlayersPerRoom)
                {
                    target = kv.Value;
                    break;
                }
            }

            // 3. 无可用则新建
            if (target == null)
            {
                target = new HuntRoom
                {
                    RoomId = _nextRoomId++,
                    MultipleId = multipleId,
                    LastRefreshTime = ServerTime.NowMs
                };
                _rooms[target.RoomId] = target;
            }

            target.PlayerIds.Add(playerId);
            _playerRoomMap[playerId] = target.RoomId;
            return target;
        }

        public void ExitRoom(long playerId)
        {
            if (!_playerRoomMap.TryGetValue(playerId, out var rid)) return;
            if (!_rooms.TryGetValue(rid, out var room)) return;
            room.PlayerIds.Remove(playerId);
            _playerRoomMap.Remove(playerId);
            if (room.PlayerIds.Count == 0)
                _rooms.Remove(rid);
        }

        public HuntRoom GetRoom(int roomId)
            => _rooms.TryGetValue(roomId, out var r) ? r : null;

        public HuntRoom GetPlayerRoom(long playerId)
        {
            if (_playerRoomMap.TryGetValue(playerId, out var rid)) return GetRoom(rid);
            return null;
        }

        public IEnumerable<HuntRoom> GetAllActiveRooms() => _rooms.Values;

        // ============ 刷怪 & 移动 ============

        private const long RefreshIntervalMs = 300; // 刷怪最小间隔，避免每帧跑权重随机拖慢移动

        public void RoomUpdate(HuntRoom room, float deltaTime)
        {
            var multipleCfg = ConfigLoader.Instance.GetMultipleCfg(room.MultipleId);
            if (multipleCfg == null) return;

            // 刷怪加间隔：每次刷怪后至少等 RefreshIntervalMs 再尝试。
            // 怪物移动/移除每帧都要跑，保证"一直在移动"。
            long now = ServerTime.NowMs;
            if (now - room.LastRefreshTime >= RefreshIntervalMs)
            {
                TryRefreshMonsters(room, multipleCfg);
                room.LastRefreshTime = now;
            }
            UpdateMonsterMovement(room, deltaTime);
            RemoveExpiredMonsters(room);
        }

        private void TryRefreshMonsters(HuntRoom room, HuntMultipleCfg multipleCfg)
        {
            var monsList   = ParseIds(multipleCfg.monsList);
            var maxNums    = ParseInts(multipleCfg.MaxNum);
            var weights    = ParseInts(multipleCfg.BornWeight);
            var bornAreas  = ParseAreas(multipleCfg.BornNumArea);

            var currentCount = new Dictionary<int, int>();
            foreach (var m in room.Monsters)
                currentCount[m.Type] = currentCount.GetValueOrDefault(m.Type, 0) + 1;

            // 候选类型：未达上限的
            var candidates = new List<int>();
            int totalWeight = 0;
            for (int i = 0; i < monsList.Count; i++)
            {
                var monsterCfg = ConfigLoader.Instance.GetMonsterCfg(monsList[i]);
                if (monsterCfg == null) continue;
                int type = monsterCfg.type;
                if (currentCount.GetValueOrDefault(type, 0) >= maxNums[i]) continue;
                candidates.Add(i);
                totalWeight += weights[i];
            }
            if (candidates.Count == 0 || totalWeight == 0) return;

            // 权重抽取
            int rnd = UnityEngine.Random.Range(0, totalWeight);
            int cum = 0;
            int chosen = candidates[0];
            foreach (var i in candidates)
            {
                cum += weights[i];
                if (rnd <= cum) { chosen = i; break; }
            }

            // 抽取数量
            var area = bornAreas[chosen];
            int bornCount = UnityEngine.Random.Range(area.min, area.max + 1);

            int mId = monsList[chosen];
            var mCfg = ConfigLoader.Instance.GetMonsterCfg(mId);
            int cur = currentCount.GetValueOrDefault(mCfg.type, 0);
            bornCount = Math.Min(bornCount, maxNums[chosen] - cur);

            for (int i = 0; i < bornCount; i++)
                SpawnMonster(room, mCfg);
        }

        private void SpawnMonster(HuntRoom room, HuntMonsterCfg cfg)
        {
            var lines = ConfigLoader.Instance.GetLinesByType(cfg.type);
            if (lines.Count == 0) return;

            var lineCfg = lines[UnityEngine.Random.Range(0, lines.Count)];
            var points = ParsePoints(lineCfg.line);

            var inst = new HuntMonsterInstance
            {
                InstanceId    = _nextMonsterInstanceId++,
                ConfigId      = cfg.id,
                Type          = cfg.type,
                BornTime      = ServerTime.NowMs,
                CurrentLineId = lineCfg.id,
                LinePoints    = points,
                MoveProgress  = 0f,
                Speed         = lineCfg.speed,
                Scale         = cfg.scale,
                BaseKillRate  = cfg.killRate,
                BaseScore     = cfg.score
            };

            room.Monsters.Add(inst);
            OnMonsterBorn?.Invoke(room.RoomId);
        }

        private void UpdateMonsterMovement(HuntRoom room, float deltaTime)
        {
            foreach (var m in room.Monsters)
            {
                if (!m.IsAlive || m.LinePoints.Count == 0) continue;
                // 连续推进：按速度累加浮点进度，而不是跳到下一个整数航点
                m.MoveProgress += m.Speed * deltaTime;
            }
        }

        private void RemoveExpiredMonsters(HuntRoom room)
        {
            var toRemove = new List<HuntMonsterInstance>();
            foreach (var m in room.Monsters)
            {
                // 连续进度走到路线末尾（最后一个航点）即离场
                if (m.LinePoints.Count == 0 ||
                    m.MoveProgress >= m.LinePoints.Count - 1)
                    toRemove.Add(m);
            }
            foreach (var m in toRemove)
            {
                room.Monsters.Remove(m);
                OnMonsterDied?.Invoke(room.RoomId, m.InstanceId);
            }
        }

        // ============ 攻击与击杀 ============

        public KillResult ExecuteAttack(
            long playerId, int multipleId, HuntMonsterInstance monster, int digimonQuality)
        {
            var globalCfg  = HuntActivityManager.Instance.GlobalCfg;
            var multipleCfg = ConfigLoader.Instance.GetMultipleCfg(multipleId);
            if (globalCfg == null || multipleCfg == null || monster == null || !monster.IsAlive)
                return null;

            // 1. 扣代币
            int cost = globalCfg.BulletCost * multipleCfg.multiple;
            if (!PlayerDataManager.Instance.ConsumeTokens(playerId, cost)) return null;

            // 2. 击杀概率：怪物基础 + 数码兽加成
            int killRate = Math.Clamp(
                monster.BaseKillRate + ConfigLoader.Instance.GetDigimonKillRate(digimonQuality),
                0, 10000);

            // 3. 判定（万分比）
            int rnd = UnityEngine.Random.Range(0, 10000);
            if (rnd >= killRate) return null;

            // 4. 结算
            monster.IsAlive = false;
            int finalScore = monster.BaseScore * multipleCfg.multiple;
            var drops = RollDrop(monster.ConfigId);

            var result = new KillResult
            {
                IsKilled = true,
                MonsterConfigId = monster.ConfigId,
                KillerId = playerId,
                FinalScore = finalScore,
                DropItems = drops
            };

            ScoreManager.Instance.AddPlayerScore(playerId, finalScore);
            ScoreManager.Instance.AddTeamScore(playerId, finalScore);
            ScoreManager.Instance.AddTokens(playerId, finalScore);

            OnKillResult?.Invoke(result);
            return result;
        }

        private List<DropItem> RollDrop(int monsterConfigId)
        {
            var cfg = ConfigLoader.Instance.GetMonsterCfg(monsterConfigId);
            var list = new List<DropItem>();
            if (cfg == null || string.IsNullOrEmpty(cfg.drop)) return list;

            var nums    = ParseInts(cfg.dropNumber);
            var weights = ParseInts(cfg.dropWeight);
            var drops   = ParseDrop(cfg.drop);
            if (nums.Count == 0 || weights.Count == 0) return list;

            // 权重抽掉落次数
            int totalWeight = 0;
            foreach (var w in weights) totalWeight += w;
            int rnd = UnityEngine.Random.Range(0, totalWeight);
            int cum = 0, chosen = 0;
            for (int i = 0; i < weights.Count; i++)
            {
                cum += weights[i];
                if (rnd <= cum) { chosen = i; break; }
            }
            int dropCount = nums[chosen];

            if (dropCount > 0)
            {
                foreach (var d in drops)
                    list.Add(new DropItem { ItemId = d.ItemId, Count = d.Count * dropCount });
            }
            return list;
        }

        // ============ 配置字符串解析 ============

        private static List<int> ParseIds(string s)
        {
            var list = new List<int>();
            foreach (var p in s.Split(','))
                if (int.TryParse(p.Trim(), out var v)) list.Add(v);
            return list;
        }

        private static List<int> ParseInts(string s)
        {
            var list = new List<int>();
            foreach (var p in s.Split(','))
                if (int.TryParse(p.Trim(), out var v)) list.Add(v);
            return list;
        }

        private static List<(int min, int max)> ParseAreas(string s)
        {
            var list = new List<(int, int)>();
            foreach (var area in s.Split(','))
            {
                var parts = area.Trim().Split('_');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out var mn) &&
                    int.TryParse(parts[1], out var mx))
                    list.Add((mn, mx));
            }
            return list;
        }

        private static List<Vector2Int> ParsePoints(string s)
        {
            var list = new List<Vector2Int>();
            foreach (var p in s.Split('#'))
                list.Add(Vector2Int.Parse(p.Trim()));
            return list;
        }

        private static List<(int ItemId, int Count)> ParseDrop(string s)
        {
            var list = new List<(int, int)>();
            foreach (var item in s.Split('#'))
            {
                var parts = item.Split('_');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out var id) &&
                    int.TryParse(parts[1], out var cnt))
                    list.Add((id, cnt));
            }
            return list;
        }
    }
}
