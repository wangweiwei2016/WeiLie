using System.Collections;
using UnityEngine;

namespace Hunt.UI
{
    /// <summary>
    /// 子弹飞行动画组件
    /// 纯协程 + 贝塞尔曲线，不依赖 DOTween
    /// 使用：Instantiate(bulletPrefab) → 设起点/终点 → Play()
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class BulletAnimator : MonoBehaviour
    {
        public float duration = 0.18f;      // 飞行时长秒
        public float heightOffset = 40f;    // 抛物线高度
        public float scaleFactor = 1.5f;    // 中途放大倍数

        private RectTransform _rt;
        private Vector2 _from;
        private Vector2 _to;
        private Coroutine _running;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
        }

        public void Play(Vector2 from, Vector2 to, System.Action onHit = null)
        {
            _from = from;
            _to = to;
            _running = StartCoroutine(FlyRoutine(onHit));
        }

        public void Stop()
        {
            if (_running != null) StopCoroutine(_running);
            Destroy(gameObject);
        }

        private IEnumerator FlyRoutine(System.Action onHit)
        {
            float t = 0;
            _rt.anchoredPosition = _from;
            _rt.localScale = Vector3.one;

            while (t < duration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / duration);

                // 贝塞尔：中段放大
                float scaleCurve = 1f + Mathf.Sin(u * Mathf.PI) * (scaleFactor - 1f);
                _rt.localScale = Vector3.one * scaleCurve;

                // 位置：线性插值 + 抛物线 Y
                Vector2 pos = Vector2.Lerp(_from, _to, u);
                pos.y += Mathf.Sin(u * Mathf.PI) * heightOffset;
                _rt.anchoredPosition = pos;

                // 简单旋转指向目标
                Vector2 dir = _to - _from;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                _rt.rotation = Quaternion.Euler(0, 0, angle);

                yield return null;
            }

            onHit?.Invoke();
            Destroy(gameObject);
        }

        /// <summary>静态工具：创建子弹并飞行</summary>
        public static BulletAnimator Spawn(Transform parent, GameObject prefab,
                                            Vector2 from, Vector2 to,
                                            System.Action onHit = null)
        {
            var go = Instantiate(prefab, parent);
            var anim = go.GetComponent<BulletAnimator>();
            if (anim == null) anim = go.AddComponent<BulletAnimator>();
            anim.Play(from, to, onHit);
            return anim;
        }
    }
}
