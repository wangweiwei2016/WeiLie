using UnityEngine;
using UnityEngine.UI;

namespace Hunt.UI
{
    /// <summary>
    /// 红点组件 - 挂在需要红点提示的入口图标上
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class RedDotHelper : MonoBehaviour
    {
        [Tooltip("红点 Image，若未赋值则自动取当前 GameObject 的 Image")]
        public Image redDotImage;

        private void Awake()
        {
            if (redDotImage == null)
            {
                redDotImage = GetComponent<Image>();
                if (redDotImage == null)
                    Debug.LogWarning("[RedDotHelper] 未找到 Image 组件", this);
            }
            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            if (redDotImage != null) redDotImage.enabled = visible;
        }
    }
}
