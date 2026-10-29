using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Hunt.Config;
using Hunt.Core;
using Hunt.Model;

namespace Hunt.UI
{
    /// <summary>
    /// §5.5 猎场战斗界面 - 核心！
    /// - 玩家固定下方 + 队友剩余 3 位置 + 数码兽展示
    /// - 中央猎场：怪物按路线移动 / 点击 / 长按攻击
    /// - 道具：狂暴 / 锁定（自动续锁同类型）
    /// - 积分飘字（金色自己 / 银色他人）
    /// - IPointerDownHandler / Up / Exit 实现长按
    /// </summary>
    public class HuntRoomUI : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Header("玩家展示")]
        public Transform playerSlotRoot;
        public GameObject playerSlotPrefab;

        [Header("中央猎场")]
        public RectTransform huntArea;
        public GameObject monsterPrefab;
        public Transform monsterContainer;

        [Header("顶部信息栏")]
        public Text tokenText;
        public Text scoreText;
        public Text teamScoreText;

        [Header("道具按钮")]
        public Button fireItemBtn;
        public Text  fireItemCountText;
        public Button lockItemBtn;
        public Text  lockItemCountText;
        public GameObject lockTargetIndicator;

        [Header("退出")]
        public Button exitBtn;

        [Header("子弹/飘字")]
        public GameObject bulletPrefab;
        public GameObject scorePopupPrefab;
        public Canvas popupCanvas;

        [Header("首次引导")]
        public GameObject fingerGuide;

        private readonly Dictionary<long, MonsterView> _monsterViews = new();
        private long _myId;
        private Font _fallbackFont;

        private void OnEnable()
        {
            BindUI();
            _myId = PlayerDataManager.Instance.CurrentPlayerId;
            _fallbackFont = Hunt.Utils.HuntFontHelper.SafeFont;

            HuntRoomManager.Instance.OnMonsterBorn    += OnMonsterBorn;
            HuntRoomManager.Instance.OnMonsterDied    += OnMonsterDied;
            HuntRoomManager.Instance.OnKillResult     += OnKillResult;
            AttackController.Instance.OnAttackTriggered += OnAttackTriggered;
            ItemManager.Instance.OnItemChanged        += OnItemChanged;

            RefreshPlayerSlots();
            RefreshTopBar();
            RefreshItemButtons();
            if (fingerGuide != null) fingerGuide.SetActive(true);
        }

        /// <summary>显式绑定按钮事件 —— 必须在 Bootstrap 字段赋值完后调用</summary>
        public void BindUI()
        {
            if (fireItemBtn != null) { fireItemBtn.onClick.RemoveAllListeners(); fireItemBtn.onClick.AddListener(OnFireItem); }
            if (lockItemBtn != null) { lockItemBtn.onClick.RemoveAllListeners(); lockItemBtn.onClick.AddListener(OnLockItem); }
            if (exitBtn != null)     { exitBtn.onClick.RemoveAllListeners(); exitBtn.onClick.AddListener(OnExit); }
        }

        private void OnDisable()
        {
            if (fireItemBtn != null) fireItemBtn.onClick.RemoveListener(OnFireItem);
            if (lockItemBtn != null) lockItemBtn.onClick.RemoveListener(OnLockItem);
            if (exitBtn != null)     exitBtn.onClick.RemoveListener(OnExit);

            HuntRoomManager.Instance.OnMonsterBorn    -= OnMonsterBorn;
            HuntRoomManager.Instance.OnMonsterDied    -= OnMonsterDied;
            HuntRoomManager.Instance.OnKillResult     -= OnKillResult;
            AttackController.Instance.OnAttackTriggered -= OnAttackTriggered;
            ItemManager.Instance.OnItemChanged        -= OnItemChanged;
            // 注意：这里不能调 ExitRoom。
            // 面板被 SetActive(false)（HideAll / 打开其它面板）也会触发 OnDisable，
            // 若在此退房，玩家只是切了个界面就被踢出猎场。退房只在玩家点「退出猎场」时执行。
        }

        private int _lastTokens = -1, _lastScore = -1, _lastTeamScore = -1;

        private void Update()
        {
            UpdateMonsterPositions();
            RefreshTopBar();
        }

        // ============ 刷新 ============

        private void RefreshPlayerSlots()
        {
            if (playerSlotRoot == null) return;
            for (int i = playerSlotRoot.childCount - 1; i >= 0; i--)
                Destroy(playerSlotRoot.GetChild(i).gameObject);

            var room = HuntRoomManager.Instance.GetPlayerRoom(_myId);
            if (room == null) return;

            for (int i = 0; i < room.PlayerIds.Count; i++)
            {
                long pid = room.PlayerIds[i];
                var go = new GameObject($"Slot_{pid}", typeof(RectTransform), typeof(Text));
                go.transform.SetParent(playerSlotRoot, false);
                var t = go.GetComponent<Text>();
                t.font = _fallbackFont;
                t.fontSize = 20;
                t.alignment = TextAnchor.MiddleCenter;
                t.color = pid == _myId ? Color.yellow : Color.white;
                var p = PlayerDataManager.Instance.GetPlayer(pid);
                string tag = pid == _myId ? "(我)" : "";
                t.text = $"{tag}{p?.Nickname ?? $"玩家_{pid}"}";
            }
        }

        private void RefreshTopBar()
        {
            var tokens = ScoreManager.Instance.GetCurrentTokens(_myId);
            var score  = ScoreManager.Instance.GetPersonalScore(_myId);
            var teamScore = ScoreManager.Instance.GetTeamScoreByPlayer(_myId);

            // 仅在数值变化时才生成字符串，避免每帧 GC 造成移动卡顿
            if (tokens != _lastTokens)
            {
                _lastTokens = tokens;
                if (tokenText != null) tokenText.text = $"代币: {tokens}";
            }
            if (score != _lastScore)
            {
                _lastScore = score;
                if (scoreText != null) scoreText.text = $"积分: {score}";
            }
            if (teamScore != _lastTeamScore)
            {
                _lastTeamScore = teamScore;
                if (teamScoreText != null) teamScoreText.text = $"团队: {teamScore}";
            }
        }

        private void RefreshItemButtons()
        {
            var cfg = HuntActivityManager.Instance.GlobalCfg;
            if (cfg == null) return;

            int fireCount = ItemManager.Instance.GetItemCount(_myId, cfg.fireItemID);
            int lockCount = ItemManager.Instance.GetItemCount(_myId, cfg.lockItemID);

            if (fireItemCountText != null) fireItemCountText.text = fireCount.ToString();
            if (lockItemCountText != null) lockItemCountText.text = lockCount.ToString();
            if (fireItemBtn != null)       fireItemBtn.interactable = fireCount > 0;
            if (lockItemBtn != null)       lockItemBtn.interactable = lockCount > 0;
        }

        private void OnItemChanged(long _p, int _id, int _c) => RefreshItemButtons();

        // ============ 怪物事件 ============

        private void OnMonsterBorn(int roomId)
        {
            if (monsterContainer == null) return;
            var room = HuntRoomManager.Instance.GetRoom(roomId);
            if (room == null) return;

            foreach (var m in room.Monsters)
            {
                if (_monsterViews.ContainsKey(m.InstanceId)) continue;

                var go = new GameObject($"Monster_{m.InstanceId}",
                    typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(monsterContainer, false);

                // 锚点必须居中，否则 anchoredPosition 是相对父物体左下角，
                // 与 UpdateMonsterPositions 里 ±w/2 的居中坐标计算对不上
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(60, 60);

                var img = go.GetComponent<Image>();
                img.color = new Color(1f, 0.5f, 0.2f, 0.9f); // 橙色方块占位
                img.raycastTarget = true;

                go.GetComponent<Button>().transition = Selectable.Transition.None;

                var monsterView = go.AddComponent<MonsterView>();
                monsterView.Bind(m, OnMonsterClicked);
                _monsterViews[m.InstanceId] = monsterView;
            }
        }

        private void OnMonsterDied(int roomId, long monsterInstanceId)
        {
            int id = (int)monsterInstanceId;
            if (_monsterViews.TryGetValue(id, out var view))
            {
                Destroy(view.gameObject);
                _monsterViews.Remove(id);
            }
        }

        private void UpdateMonsterPositions()
        {
            var room = HuntRoomManager.Instance.GetPlayerRoom(_myId);
            if (room == null) return;
            if (huntArea == null) return;

            float w = huntArea.rect.width;
            float h = huntArea.rect.height;

            foreach (var m in room.Monsters)
            {
                if (!_monsterViews.TryGetValue(m.InstanceId, out var v) || v == null) continue;
                if (m.LinePoints == null || m.LinePoints.Count == 0) continue;

                // 用连续插值后的坐标，怪物会一直在航点之间平滑移动
                var p = m.GetInterpolatedPosition();
                float nx = (float)p.x / 480f * w - w / 2f;
                float ny = (float)p.y / 300f * h - h / 2f;
                var rt = v.CachedRect ?? (v.CachedRect = v.GetComponent<RectTransform>());
                rt.anchoredPosition = new Vector2(nx, ny);
            }
        }

        private void OnMonsterClicked(MonsterView _view, HuntMonsterInstance _monster)
        {
            // 后续可用来：锁定模式切换目标 / 单次点击也发起攻击
        }

        // ============ 攻击接口 ============

        public void OnPointerDown(PointerEventData _eventData)
        {
            if (fingerGuide != null) fingerGuide.SetActive(false);
            AttackController.Instance.StartContinuousAttack();
        }

        public void OnPointerUp(PointerEventData _eventData)
        {
            AttackController.Instance.StopContinuousAttack();
        }

        public void OnPointerExit(PointerEventData _eventData)
        {
            AttackController.Instance.StopContinuousAttack();
        }

        private void OnAttackTriggered()
        {
            // 子弹表现（简化：从猎场中心闪一下）
            if (bulletPrefab == null || huntArea == null) return;
            var bullet = Instantiate(bulletPrefab, huntArea);
            // prefab 本身是 inactive 的，实例化出来的克隆也是 inactive，必须手动激活才看得见
            bullet.SetActive(true);
            var brt = bullet.GetComponent<RectTransform>();
            if (brt != null) brt.anchoredPosition = Vector2.zero;
            Destroy(bullet, 0.2f);
        }

        private void OnKillResult(KillResult result)
        {
            bool isMine = result.KillerId == _myId;
            ShowScorePopup(result.FinalScore, isMine);

            if (isMine)
            {
                foreach (var d in result.DropItems)
                    ItemManager.Instance.AddItem(_myId, d.ItemId, d.Count);
            }
        }

        private void ShowScorePopup(int score, bool isMine)
        {
            if (scorePopupPrefab == null || popupCanvas == null) return;

            // 猎场中心附近随机偏移，避免多个飘字完全重叠
            Vector2 pos = new Vector2(Random.Range(-140f, 140f), Random.Range(-60f, 60f));
            var anim = ScorePopupAnimator.Spawn(
                popupCanvas.transform,
                scorePopupPrefab,
                pos,
                $"+{score}",
                isMine ? Color.yellow : Color.white);

            if (anim != null) anim.transform.SetAsLastSibling();
        }

        // ============ 道具 ============

        private void OnFireItem() { AttackController.Instance.UseFireItem(); }

        private void OnLockItem()
        {
            var room = HuntRoomManager.Instance.GetPlayerRoom(_myId);
            if (room == null || room.Monsters.Count == 0) return;
            var first = room.Monsters.Find(m => m.IsAlive);
            if (first != null)
            {
                AttackController.Instance.UseLockItem(first);
                if (lockTargetIndicator != null) lockTargetIndicator.SetActive(true);
            }
        }

        private void OnExit()
        {
            // 只有明确点「退出猎场」才真正退房，避免切界面（HideAll）把玩家踢出猎场
            HuntRoomManager.Instance.ExitRoom(_myId);
            HuntUIManager.Instance.OpenRoomSelect();
        }
    }

    public class MonsterView : MonoBehaviour
    {
        public HuntMonsterInstance Monster;
        public Button clickBtn;
        public RectTransform CachedRect;
        public System.Action<MonsterView, HuntMonsterInstance> OnClick;

        private void Awake()
        {
            if (clickBtn == null) clickBtn = GetComponent<Button>();
            if (CachedRect == null) CachedRect = GetComponent<RectTransform>();
            if (clickBtn != null) clickBtn.onClick.AddListener(() => OnClick?.Invoke(this, Monster));
        }

        public void Bind(HuntMonsterInstance monster,
                         System.Action<MonsterView, HuntMonsterInstance> onClick)
        {
            Monster = monster;
            OnClick = onClick;
        }
    }
}
