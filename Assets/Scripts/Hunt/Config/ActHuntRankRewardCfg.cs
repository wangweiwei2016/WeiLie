using System;

namespace Hunt.Config
{
    /// <summary>
    /// 围猎排行奖励配置表（CDN）
    /// </summary>
    [Serializable]
    public class ActHuntRankRewardCfg
    {
        public int id;          // 奖励配置唯一ID
        public int type;        // 排行榜类型 1团队区服 2团队全服 3个人区服 4个人全服
        public string rankNumb; // 排名区间 格式: 1#1 表示第1名 2#10 表示第2至第10名
        public string reward;   // 对应奖励内容 格式: 道具ID_数量#道具ID_数量
    }

    public enum HuntRankType
    {
        TeamServer = 1,      // 团队区服榜
        TeamAllServer = 2,   // 团队全服榜
        PersonalServer = 3,  // 个人区服榜
        PersonalAllServer = 4 // 个人全服榜
    }
}
