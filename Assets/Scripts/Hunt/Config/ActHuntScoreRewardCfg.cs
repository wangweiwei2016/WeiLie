using System;

namespace Hunt.Config
{
    /// <summary>
    /// 围猎积分奖励配置表（CDN）
    /// </summary>
    [Serializable]
    public class ActHuntScoreRewardCfg
    {
        public int id;      // 奖励配置唯一ID
        public string desc; // 奖励描述
        public int score;   // 达成奖励所需的团队累计积分
        public string reward; // 奖励内容 格式: 道具ID_数量#道具ID_数量
    }
}
