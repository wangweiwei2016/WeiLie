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

            HuntRoomManager.Instance.ExitRoom(_myId);
        }

        private void Update()
        {
            UpdateMonsterPositions();
            RefreshTopBar();
        }

        // ============ 刷新 ============

        private void RefreshPlayerSlots()
        {
            if (playerSlotRoot == null) return;
            foreach (Transform c in playerSlotRoot) Destroy(c.gameObject);

            var room = HuntRoomManager.Instance.GetPlayerRoom(_myId);
            if (room == null) return;

            for (int i = 0; i < room.PlayerIds.Count; i++)
            {
                var pid = room.PlayerIds[i];
                var go = new GameObject($"Slot_{pid}");
                go.transform.SetParent(playerSlotRoot, false);
                var t = go.AddComponent<Text>();
                t.font = _fallbackFont;
                string tag = pid == _myId ? "(我) " : "";
                var p = PlayerDataManager.Instance.GetPlayer(pid);
                //t.text = $"{tag}{p?.Nickname ?? pid}";
                t.alignment = TextAnchor.MiddleCenter;
            }
        }

        private void RefreshTopBar()
        {
            var tokens = ScoreManager.Instance.GetCurrentTokens(_myId);
            var score  = ScoreManager.Instance.GetPersonalScore(_myId);
            var teamScore = ScoreManager.Instance.GetTeamScoreByPlayer(_myId);
            if (tokenText != null)    tokenText.text    = $"代币: {tokens}";
            if (scoreText != null)    scoreText.text    = $"积分: {score}";
            if (teamScoreText != null) teamScoreText.text = $"团队: {teamScore}";
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
            var room = HuntRoomManager.Instance.GetRoom(roomId);
            if (room == null) return;

            foreach (var m in room.Monsters)
            {
                if (!_monsterViews.ContainsKey(m.InstanceId))
                {
                    var go = new GameObject($"Monster_{m.InstanceId}");
                    go.transform.SetParent(monsterContainer, false);
                    var rt = go.AddComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(60, 60);
                    var img = go.AddComponent<Image>();
                    img.color = new Color(1f, 0.5f, 0.2f, 0.9f); // 橙色方块占位
                    var monsterView = go.AddComponent<MonsterView>();
                    monsterView.Bind(m, OnMonsterClicked);
                    _monsterViews[m.InstanceId] = monsterView;
                }
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

                var p = m.LinePoints[Mathf.Min(m.CurrentPointIndex, m.LinePoints.Count - 1)];
                // 假设 Grid 最大 480×300，映射到 UI 中心坐标
                float nx = (float)p.x / 480f * w - w / 2f;
                float ny = (float)p.y / 300f * h - h / 2f;
                v.GetComponent<RectTransform>().anchoredPosition = new Vector2(nx, ny);
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
            // 子弹表现（简化：在猎场中心闪一下）
            if (bulletPrefab != null && huntArea != null)
            {
                var bullet = Instantiate(bulletPrefab, huntArea);
                Destroy(bullet, 0.2f);
            }
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
            var popup = Instantiate(scorePopupPrefab, popupCanvas.transform);
            var txt = popup.GetComponent<Text>();
            if (txt != null)
            {
                txt.font = _fallbackFont;
                txt.text = $"+{score}";
                txt.fontSize = 28;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = isMine ? Color.yellow : Color.white;
            }
            Destroy(popup, 1.2f);
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
            gameObject.SetActive(false);
            HuntUIManager.Instance.OpenRoomSelect();
        }
    }

    public class MonsterView : MonoBehaviour
    {
        public HuntMonsterInstance Monster;
        public Button clickBtn;
        public System.Action<MonsterView, HuntMonsterInstance> OnClick;

        private void Awake()
        {
            if (clickBtn == null) clickBtn = GetComponent<Button>();
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
