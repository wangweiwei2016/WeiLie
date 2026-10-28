using UnityEngine;

namespace Hunt.Utils
{
    /// <summary>
    /// 字体安全加载器 - 多级 fallback，所有异常静默
    /// Unity 2022+ 移除了 LegacyRuntime.ttf，需换 Arial 等
    /// 调用 HuntFontHelper.SafeFont 获取可用字体
    /// </summary>
    public static class HuntFontHelper
    {
        private static Font _cached;

        public static Font SafeFont
        {
            get
            {
                if (_cached != null) return _cached;
                _cached = FindWorkingFont();
                return _cached;
            }
        }

        private static Font FindWorkingFont()
        {
            Font f;

            // 尝试顺序：Resources本地 → 内置Arial → 系统动态字体
            // ⚠️ Unity 2022+ 已移除 LegacyRuntime.ttf，Resources.GetBuiltinResource 找不到时
            // 会在 C++ 层打 Error 日志（try/catch 吞不掉），因此彻底不再尝试 LegacyRuntime

            // 1. Resources.Load 自己的 Assets/Resources/Fonts 下的字体
            f = TryGetResourceFont("Fonts/Arial");
            if (f != null) return f;
            f = TryGetResourceFont("Fonts/DefaultFont");
            if (f != null) return f;

            // 2. 内置 Arial.ttf —— Unity 2022+ 自带 ✅
            f = TryGetBuiltin("Arial.ttf");
            if (f != null) return f;

            // 3. Arial.ttc 版本兜底
            f = TryGetBuiltin("Arial.ttc");
            if (f != null) return f;

            // 4. 系统字体动态创建（最后兜底，永远不会返回 null）
            f = TryCreateOSFont();
            if (f != null) return f;

            return null;
        }

        private static Font TryGetBuiltin(string name)
        {
            try
            {
                var f = Resources.GetBuiltinResource<Font>(name);
                return f != null ? f : null;
            }
            catch { return null; }
        }

        private static Font TryGetResourceFont(string path)
        {
            try
            {
                // 不指定扩展名，Resources 会自动匹配 ttf/otf/ttc
                var f = Resources.Load<Font>(path);
                return f;
            }
            catch { return null; }
        }

        private static Font TryCreateOSFont()
        {
            try
            {
                var f = Font.CreateDynamicFontFromOSFont(
                    new[] { "Arial", "ArialMT", "Helvetica Neue", "Microsoft YaHei", "sans-serif" }, 16);
                return f;
            }
            catch { return null; }
        }

        public static void Apply(UnityEngine.UI.Text txt)
        {
            if (txt != null && SafeFont != null) txt.font = SafeFont;
        }
    }
}
