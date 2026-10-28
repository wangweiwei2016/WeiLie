using System;
using System.Collections.Generic;

namespace Hunt.Config
{
    /// <summary>
    /// 围猎倍场配置表（本地）
    /// </summary>
    [Serializable]
    public class HuntMultipleCfg
    {
        public int id;              // 猎场唯一ID
        public int multiple;        // 猎场积分倍率
        public string name;         // 猎场名称
        public string monsList;     // 允许出现的怪物配置ID列表 格式: id1,id2,id3
        public string MaxNum;       // 各怪物类型最大数量 格式: n1,n2,n3
        public string MinNum;       // 各怪物类型最小数量 格式: n1,n2,n3
        public string BornWeight;   // 各怪物类型出生权重 格式: w1,w2,w3
        public string BornNumArea;  // 各怪物类型每次生成数量区间 格式: min1_max1,min2_max2
        public int unlock;          // 解锁所需的个人累计获得代币数量
        public int showMonster;     // 展示用怪物数据ID
        public string platform;     // 猎场场景资源路径
        public string numBG;        // 怪物数量背景资源路径
    }
}
