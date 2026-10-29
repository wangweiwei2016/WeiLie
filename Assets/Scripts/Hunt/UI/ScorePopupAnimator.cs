using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Hunt.UI
{
    /// <summary>
    /// 积分飘字动画 —— 向上浮动 + 淡出 + 缩放 pop
    /// 不依赖 DOTween，纯协程
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class ScorePopupAnimator : MonoBehaviour
    {
        public float lifetime  = 1.2f;     // 总时长
        public float riseHeight = 80f;      // 向上飘的像素
        public float fadeDelay  = 0.4f;     // 何时开始淡出
        public Vector3 maxScale = new Vector3(1.4f, 1.4f, 1f); // 放大峰值

        private Text _text;
        private CanvasGroup _cg;

        private void Awake()
        {
            _text = gameObject.GetComponent<Text>();
            _cg = gameObject.GetComponent<CanvasGroup>();
            if (null == _cg)
            {
                _cg = gameObject.AddComponent<CanvasGroup>();
            }

            if (null != _cg)
            {
                _cg.alpha = 1f;
            }
        }

        private void OnEnable()
        {
            StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            float t = 0;
            Vector2 start = GetComponent<RectTransform>().anchoredPosition;

            while (t < lifetime)
            {
                t += Time.deltaTime;
                float u = t / lifetime;

                // Y 上浮（ ease-out 曲线）
                float eased = 1f - Mathf.Pow(1f - u, 3f);
                var rt = GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(start.x, start.y + eased * riseHeight);

                // 缩放 pop（先涨后缩）
                float sCurve = Mathf.Sin(u * Mathf.PI);
                transform.localScale = Vector3.Lerp(Vector3.one, maxScale, sCurve);

                // 淡出（fadeDelay 后开始）
                if (u > fadeDelay / lifetime)
                {
                    float fu = (u - fadeDelay / lifetime) / (1f - fadeDelay / lifetime);
                    _cg.alpha = 1f - fu;
                }

                yield return null;
            }

            Destroy(gameObject);
        }

        /// <summary>静态工具：创建飘字</summary>
        public static ScorePopupAnimator Spawn(Transform parent, GameObject prefab,
                                                Vector2 pos, string text, Color color)
        {
            var go = Instantiate(prefab, parent);
            go.SetActive(true); // prefab 通常是 inactive 的，克隆出来必须手动激活，否则动画协程不会启动
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            var txt = go.GetComponent<Text>();
            if (txt == null) txt = go.AddComponent<Text>();
            txt.font = Hunt.Utils.HuntFontHelper.SafeFont;
            txt.text = text;
            txt.fontSize = 28;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = color;

            var anim = go.GetComponent<ScorePopupAnimator>();
            if (anim == null) anim = go.AddComponent<ScorePopupAnimator>();
            return anim;
        }
    }
}
