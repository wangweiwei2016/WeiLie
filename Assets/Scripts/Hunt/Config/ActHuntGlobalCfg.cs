using System;
using System.Collections.Generic;

namespace Hunt.Config
{
    /// <summary>
    /// 围猎全局配置表（CDN）- 活动编号: 8001
    /// </summary>
    [Serializable]
    public class ActHuntGlobalCfg
    {
        public int id;                  // 配置ID
        public int gameType;            // 1=单人模式 2=组队模式
        public int teamMateNum;         // 单支队伍允许的玩家数量（含队长）
        public int maxPlayersPerRoom;   // 单个猎场最多容纳玩家数
        public long teamStartTime;      // 组队阶段开始时间（Unix时间戳 毫秒）
        public long teamEndTime;        // 组队阶段结束时间
        public long activityStartTime;  // 正式活动开始时间
        public long activityEndTime;     // 正式活动结束时间
        public int BulletCost;          // 每次攻击的代币基础消耗
        public int attackInterval;      // 基础攻击间隔（毫秒）
        public int minAttackInterval;   // 最小攻击间隔（毫秒）
        public int fireItemID;          // 狂暴道具ID
        public int lockItemID;          // 锁定道具ID
        public int itemDuration;        // 道具持续时间（毫秒）
        public int finalCycleScore;     // 循环奖励每轮所需增加的积分
        public string finalCycleReward; // 每轮循环奖励内容 格式: 道具ID_数量#道具ID_数量
        public string initItems;        // 活动初始发放道具 格式: 道具ID_数量#道具ID_数量
        public int rankMailId;          // 排行奖励补发邮件ID
        public int scoreMailId;         // 积分奖励补发邮件ID
    }

    /// <summary>活动阶段枚举</summary>
    public enum HuntActivityStage
    {
        None = 0,          // 未开启
        TeamBuilding = 1,  // 组队阶段
        Hunting = 2,       // 正式围猎阶段
        Settlement = 3,    // 结算阶段
        Ended = 4          // 已结束
    }
}
