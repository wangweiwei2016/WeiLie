using System;
using System.Collections.Generic;

namespace Hunt.Model
{
    /// <summary>活动中的玩家数据</summary>
    public class HuntPlayer
    {
        public long PlayerId;
        public string Nickname;
        public string Avatar;
        public int DigimonId;           // 当前选择的参战数码兽ID
        public int DigimonQuality;      // 数码兽品质（用于计算击杀概率加成）
        public int PersonalScore;       // 个人积分
        public int PersonalTokens;      // 当前代币余额（已弃用，用 ScoreManager）
        public int TotalTokensEarned;   // 累计获得代币（用于解锁高倍率猎场）
        public long JoinTime;           // 加入时间（用于同分排序）
        public bool IsOnline;
    }

    /// <summary>队伍数据</summary>
    public class HuntTeam
    {
        public long TeamId;
        public long LeaderId;
        public List<long> MemberIds = new List<long>();
        public int TeamScore;           // 团队累计积分（所有成员共享）
        public long ScoreUpdateTime;    // 团队积分最后更新时间
    }

    /// <summary>运行时怪物实例</summary>
    public class HuntMonsterInstance
    {
        public long InstanceId;          // 运行时唯一ID
        public int ConfigId;            // HuntMonsterCfg.id
        public int Type;                // 怪物类型
        public long BornTime;           // 生成时间
        public int CurrentLineId;       // 当前路线ID
        public List<Vector2Int> LinePoints = new List<Vector2Int>(); // 路线坐标点
        public int CurrentPointIndex;   // 当前目标坐标点索引
        public float Speed;             // 每秒移动的坐标数
        public float Scale;             // 缩放比例（float 以匹配 HuntMonsterCfg.scale）
        public bool IsAlive = true;

        // 运行时数据 - 不存配置直接算
        public int BaseKillRate;        // 基础击杀概率（万分比）
        public int BaseScore;           // 基础积分
    }

    /// <summary>猎场实例（房间）</summary>
    public class HuntRoom
    {
        public int RoomId;
        public int MultipleId;          // 倍率配置ID
        public List<long> PlayerIds = new List<long>();
        public List<HuntMonsterInstance> Monsters = new List<HuntMonsterInstance>();
        public long LastRefreshTime;    // 上次刷怪时间
    }

    public struct Vector2Int
    {
        public int x;
        public int y;
        public Vector2Int(int x, int y) { this.x = x; this.y = y; }

        public static Vector2Int Parse(string s)
        {
            var parts = s.Split('_');
            return new Vector2Int(int.Parse(parts[0]), int.Parse(parts[1]));
        }

        public override string ToString() => $"({x},{y})";
    }

    /// <summary>击杀结算结果</summary>
    public class KillResult
    {
        public bool IsKilled;
        public int MonsterConfigId;
        public long KillerId;
        public int FinalScore;          // baseScore × multiple
        public List<DropItem> DropItems = new List<DropItem>();
    }

    public class DropItem
    {
        public int ItemId;
        public int Count;
    }

    /// <summary>攻击请求（预留服务端接口）</summary>
    public class AttackRequest
    {
        public long PlayerId;
        public int RoomId;
        public int MonsterInstanceId;
        public int BulletCost;
    }
}
