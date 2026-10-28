using System;

namespace Hunt.Config
{
    /// <summary>
    /// 怪物移动路线配置表（本地）
    /// </summary>
    [Serializable]
    public class HuntLineCfg
    {
        public int id;      // 路线唯一ID
        public int type;     // 路线适用的怪物类型
        public int life;     // 路线存活时间（按坐标步数描述）
        public string line;  // 路线坐标序列 格式: x1_y1#x2_y2#x3_y3
        public int speed;    // 每秒移动的坐标数
    }
}
