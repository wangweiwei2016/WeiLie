using System;

namespace Hunt.Config
{
    /// <summary>
    /// 围猎怪物配置表（本地）
    /// </summary>
    [Serializable]
    public class HuntMonsterCfg
    {
        public int id;           // 怪物配置ID
        public int type;          // 怪物类型
        public float scale;       // 场景显示缩放比例
        public int killRate;      // 基础击杀概率 万分比（0-10000）
        public int score;         // 基础击杀积分
        public string dropNumber; // 掉落次数区间 格式: 次数1#次数2#...
        public string dropWeight; // 对应次数的权重 格式: 权重1#权重2#...
        public string drop;       // 可掉落道具列表 格式: 道具ID_数量#道具ID_数量
    }
}
