using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Hunt.Core;

namespace Hunt.UI
{
    /// <summary>
    /// §5.3 数码兽选择界面
    /// - 展示玩家拥有的数码兽列表（品质 → 战斗力降序）
    /// - 队友已选数码兽展示（剪影问号表示未选）
    /// - 确定后进入猎场选择
    /// </summary>
    public class DigimonSelectUI : MonoBehaviour
    {
        [Header("队友展示")]
        public Transform teammateRoot;
        public GameObject teammateSlotPrefab;

        [Header("数码兽列表")]
        public Transform listRoot;
        public GameObject digimonItemPrefab;

        [Header("按钮")]
        public Button confirmBtn;
        public Button closeBtn;

        private readonly List<MockDigimon> _mockDigimons = new()
        {
            new MockDigimon { Id = 101, Name = "战斗暴龙兽", Quality = 5, Power = 9800 },
            new MockDigimon { Id = 102, Name = "钢铁加鲁鲁", Quality = 5, Power = 9600 },
            new MockDigimon { Id = 103, Name = "迪路兽",     Quality = 4, Power = 7200 },
            new MockDigimon { Id = 104, Name = "巴达兽",     Quality = 3, Power = 4500 },
            new MockDigimon { Id = 105, Name = "V仔兽",      Quality = 3, Power = 4300 },
        };

        private DigimonItemUI _selectedItem;

        private void OnEnable()
        {
            BindUI();
            RefreshList();
            RefreshTeammate();
            _selectedItem = null;
        }

        /// <summary>显式绑定按钮事件 —— 必须在 Bootstrap 字段赋值完后调用</summary>
        public void BindUI()
        {
            if (confirmBtn != null) { confirmBtn.onClick.RemoveAllListeners(); confirmBtn.onClick.AddListener(OnConfirm); }
            if (closeBtn != null)   { closeBtn.onClick.RemoveAllListeners();   closeBtn.onClick.AddListener(() => gameObject.SetActive(false)); }
        }

        private void OnDisable()
        {
            if (confirmBtn != null) confirmBtn.onClick.RemoveListener(OnConfirm);
            if (closeBtn != null)   closeBtn.onClick.RemoveAllListeners();
        }

        private void RefreshList()
        {
            if (listRoot == null || digimonItemPrefab == null) return;
            foreach (Transform c in listRoot) Destroy(c.gameObject);

            // 品质降序 → 同品质战斗力降序
            _mockDigimons.Sort((a, b) =>
            {
                int q = b.Quality.CompareTo(a.Quality);
                return q != 0 ? q : b.Power.CompareTo(a.Power);
            });

            foreach (var d in _mockDigimons)
            {
                var go = Instantiate(digimonItemPrefab, listRoot);
                var item = go.GetComponent<DigimonItemUI>();
                if (item != null)
                {
                    item.SetData(d);
                    item.OnSelected -= OnDigimonSelected; // 防重复
                    item.OnSelected += OnDigimonSelected;
                }
            }
        }

        private void RefreshTeammate()
        {
            if (teammateRoot == null || teammateSlotPrefab == null) return;
            foreach (Transform c in teammateRoot) Destroy(c.gameObject);

            var team = TeamManager.Instance.GetPlayerTeam(PlayerDataManager.Instance.CurrentPlayerId);
            if (team == null) return;

            foreach (var mid in team.MemberIds)
            {
                var go = Instantiate(teammateSlotPrefab, teammateRoot);
                var view = go.GetComponent<DigimonTeammateSlotUI>();
                if (view != null)
                {
                    var p = PlayerDataManager.Instance.GetPlayer(mid);
                    bool selected = p != null && p.DigimonId > 0;
                    view.SetData(mid, p?.Nickname ?? $"玩家_{mid}", selected, p?.DigimonQuality ?? 0);
                }
            }
        }

        private void OnDigimonSelected(DigimonItemUI item)
        {
            _selectedItem?.SetSelected(false);
            _selectedItem = item;
            _selectedItem.SetSelected(true);
        }

        private void OnConfirm()
        {
            if (_selectedItem == null) return;
            long myId = PlayerDataManager.Instance.CurrentPlayerId;
            PlayerDataManager.Instance.SelectDigimon(myId, _selectedItem.Data.Id, _selectedItem.Data.Quality);
            HuntUIManager.Instance.OpenRoomSelect();
            gameObject.SetActive(false);
        }
    }

    public class DigimonItemUI : MonoBehaviour
    {
        public Text nameText;
        public Text qualityText;
        public Image selectionFrame;
        public MockDigimon Data;

        public System.Action<DigimonItemUI> OnSelected;

        private Button _btn;

        private void Awake()
        {
            _btn = GetComponent<Button>();
            if (_btn != null) _btn.onClick.AddListener(() => OnSelected?.Invoke(this));
        }

        private void OnDestroy()
        {
            if (_btn != null) _btn.onClick.RemoveAllListeners();
        }

        public void SetData(MockDigimon d)
        {
            Data = d;
            if (nameText != null)    nameText.text    = d.Name;
            if (qualityText != null) qualityText.text = $"品质{d.Quality}";
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (selectionFrame != null) selectionFrame.enabled = selected;
        }
    }

    public class DigimonTeammateSlotUI : MonoBehaviour
    {
        public Text nicknameText;
        public GameObject selectedMask;
        public GameObject waitingMask;

        public void SetData(long playerId, string nickname, bool selected, int quality)
        {
            if (nicknameText != null) nicknameText.text = nickname;
            if (selectedMask != null) selectedMask.SetActive(selected);
            if (waitingMask != null)  waitingMask.SetActive(!selected);
        }
    }

    public class MockDigimon { public int Id; public string Name; public int Quality; public int Power; }
}
