using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Hunt.UI
{
    /// <summary>
    /// 围猎 UI 管理器 - 单例
    /// 管理各子界面的打开/关闭/切换
    /// 遵循 Unity 经验：OnEnable/OnDisable 成对订阅事件
    /// </summary>
    public class HuntUIManager : MonoBehaviour
    {
        public static HuntUIManager Instance { get; private set; }

        [Header("Panel 引用 - 按需求文档 §5 定义")]
        public GameObject panelEntry;        // 5.1 活动入口
        public GameObject panelTeamBuilding; // 5.2 组队
        public GameObject panelDigimonSelect; // 5.3 数码兽选择
        public GameObject panelRoomSelect;    // 5.4 猎场选择
        public GameObject panelHuntRoom;     // 5.5 战斗
        public GameObject panelScoreReward;  // 5.6 积分奖励
        public GameObject panelRank;         // 5.7 排行榜

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // 注意：这里不能缓存 panel 列表。
            // HuntUIAutoBootstrap 先 AddComponent（Awake 立即执行）再给 panelXXX 赋值，
            // 此时字段全是 null，缓存下来会导致 HideAll() 什么都隐藏不了。
        }

        /// <summary>每次实时读取字段，保证 Bootstrap 后赋的引用也能生效</summary>
        private IEnumerable<GameObject> AllPanels()
        {
            yield return panelEntry;
            yield return panelTeamBuilding;
            yield return panelDigimonSelect;
            yield return panelRoomSelect;
            yield return panelHuntRoom;
            yield return panelScoreReward;
            yield return panelRank;
        }

        private void OnEnable()
        {
            Hunt.Core.HuntActivityManager.Instance.OnStageChanged += OnStageChanged;
        }

        private void OnDisable()
        {
            Hunt.Core.HuntActivityManager.Instance.OnStageChanged -= OnStageChanged;
        }

        // ============ 打开/关闭 ============

        public void Show(GameObject panel)
        {
            if (panel == null) { Debug.LogError("[HuntUIManager] Show: panel is null"); return; }
            panel.SetActive(true);
        }

        public void Hide(GameObject panel)
        {
            if (panel != null) panel.SetActive(false);
        }

        public void HideAll()
        {
            foreach (var p in AllPanels()) Hide(p);
        }

        // ============ 阶段驱动 ============

        private void OnStageChanged(Hunt.Config.HuntActivityStage stage)
        {
            switch (stage)
            {
                case Hunt.Config.HuntActivityStage.TeamBuilding:
                    HideAll();
                    Show(panelEntry);
                    Show(panelTeamBuilding);
                    break;
                case Hunt.Config.HuntActivityStage.Hunting:
                    // 不强制弹，玩家点入口触发
                    break;
                case Hunt.Config.HuntActivityStage.Ended:
                    HideAll();
                    Show(panelRank);
                    break;
            }
        }

        // ============ 便捷入口 ============

        public void OpenTeamBuilding()  { HideAll(); Show(panelEntry); Show(panelTeamBuilding); }
        public void OpenDigimonSelect() { HideAll(); Show(panelEntry); Show(panelDigimonSelect); }
        public void OpenRoomSelect()    { HideAll(); Show(panelEntry); Show(panelRoomSelect); }
        public void OpenHuntRoom()      { HideAll(); Show(panelHuntRoom); }
        public void OpenScoreReward()   { Show(panelScoreReward); } // 子面板，不 HideAll
        public void OpenRank()          { HideAll(); Show(panelRank); }
        public void CloseAllPanels()    { HideAll(); Show(panelEntry); }
    }
}
