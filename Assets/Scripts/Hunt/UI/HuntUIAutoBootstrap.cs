using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Hunt.Core;
using Hunt.UI;

namespace Hunt.UI
{
    /// <summary>
    /// 围猎 UI 自动构建器 - RuntimeInitialize 自动跑
    /// Play 即有完整 Canvas + 7 Panel + 子组件 + 自动绑 HuntUIManager 引用
    /// 用户用 Prefab/FGUI 替换时，把 EnableAutoBootstrap 设为 false 即可
    /// </summary>
    public static class HuntUIAutoBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Bootstrap()
        {
            // 防重复（Editor 域重载 / 场景里已有 HuntCanvas）
            if (GameObject.Find("HuntCanvas") != null) return;

            Debug.Log("[HuntUIAutoBootstrap] 开始自动构建围猎 UI...");

            // 1. Canvas + EventSystem
            var canvasGo = new GameObject("HuntCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            Object.DontDestroyOnLoad(canvasGo);

            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                Object.DontDestroyOnLoad(es);
            }

            // 2. 根节点
            var root = CreatePanel(canvas.transform, "Entry");

            // 3. 创建 7 个 Panel（全屏覆盖）
            var panelEntry        = CreatePanel(canvas.transform, "PanelEntry");
            var panelTeamBuild    = CreatePanel(canvas.transform, "PanelTeamBuilding");
            var panelDigimon      = CreatePanel(canvas.transform, "PanelDigimonSelect");
            var panelRoomSelect   = CreatePanel(canvas.transform, "PanelRoomSelect");
            var panelHuntRoom     = CreatePanel(canvas.transform, "PanelHuntRoom");
            var panelScoreReward  = CreatePanel(canvas.transform, "PanelScoreReward");
            var panelRank         = CreatePanel(canvas.transform, "PanelRank");

            // 4. 给每个 Panel 挂自己的控制器并建子组件
            BuildEntryPanel(panelEntry);
            BuildTeamBuildingPanel(panelTeamBuild);
            BuildDigimonSelectPanel(panelDigimon);
            BuildRoomSelectPanel(panelRoomSelect);
            BuildHuntRoomPanel(panelHuntRoom);
            BuildScoreRewardPanel(panelScoreReward);
            BuildRankPanel(panelRank);

            // 5. 确保 HuntGameLoop 存在
            HuntGameLoop gameLoop;
            if (HuntGameLoop.Instance == null)
            {
                var loopGo = new GameObject("HuntGameLoop");
                Object.DontDestroyOnLoad(loopGo);
                gameLoop = loopGo.AddComponent<HuntGameLoop>();
            }
            else
            {
                gameLoop = HuntGameLoop.Instance;
            }

            // 6. HuntUIManager 挂到 gameLoop 上并填所有引用
            var uiMgr = gameLoop.gameObject.GetComponent<HuntUIManager>();
            if (uiMgr == null) uiMgr = gameLoop.gameObject.AddComponent<HuntUIManager>();

            uiMgr.panelEntry        = panelEntry;
            uiMgr.panelTeamBuilding = panelTeamBuild;
            uiMgr.panelDigimonSelect= panelDigimon;
            uiMgr.panelRoomSelect   = panelRoomSelect;
            uiMgr.panelHuntRoom     = panelHuntRoom;
            uiMgr.panelScoreReward  = panelScoreReward;
            uiMgr.panelRank         = panelRank;

            // 默认先隐藏所有面板（HuntUIManager.Awake 已自动 HideAll）
            foreach (var go in new[] { panelTeamBuild, panelDigimon, panelRoomSelect, panelHuntRoom, panelScoreReward, panelRank })
                go.SetActive(false);

            Debug.Log("[HuntUIAutoBootstrap] 围猎 UI 自动构建完成 ✅ 共 7 个 Panel + HuntUIManager 引用已绑定");
        }

        // ===================== 通用工具 =====================

        private static Font _font;
        private static Font Font => Hunt.Utils.HuntFontHelper.SafeFont;

        private static GameObject CreatePanel(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;  rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;  rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = new Color(0, 0, 0, 0.6f); // 半透明遮罩
            img.raycastTarget = false; // 关键！全屏遮罩不能拦截 Raycast，否则子按钮点不到
            return go;
        }

        private static Text CreateText(Transform parent, string text, int fontSize = 24,
                                        TextAnchor anchor = TextAnchor.MiddleCenter,
                                        Color? color = null)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = fontSize;
            t.alignment = anchor;
            t.color = color ?? Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            return t;
        }

        private static Button CreateButton(Transform parent, string label, int fontSize = 22)
        {
            var go = new GameObject($"Btn_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.2f, 0.6f, 1f, 0.9f);
            img.raycastTarget = true;
            var btn = go.GetComponent<Button>();
            var txt = CreateText(go.transform, label, fontSize);
            txt.rectTransform.anchorMin = Vector2.zero;
            txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.offsetMin = Vector2.zero;
            txt.rectTransform.offsetMax = Vector2.zero;
            return btn;
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            return img;
        }

        // ===================== 各 Panel 构建 =====================

        private static void BuildEntryPanel(GameObject panelEntry)
        {
            // 活动入口面板 - 显示标题 + 点击按钮 + 倒计时 + 红点
            var rt = (RectTransform)panelEntry.transform;

            // 标题
            var title = CreateText(panelEntry.transform, "数码兽 · 围猎", 48, TextAnchor.MiddleCenter, Color.yellow);
            title.rectTransform.anchorMin = new Vector2(0.5f, 1);
            title.rectTransform.anchorMax = new Vector2(0.5f, 1);
            title.rectTransform.pivot = new Vector2(0.5f, 1);
            title.rectTransform.anchoredPosition = new Vector2(0, -120);
            title.rectTransform.sizeDelta = new Vector2(600, 80);

            // 活动入口按钮
            var entryBtnGo = new GameObject("Btn_Enter", typeof(RectTransform), typeof(Image), typeof(Button));
            entryBtnGo.transform.SetParent(panelEntry.transform, false);
            var eBtnImg = entryBtnGo.GetComponent<Image>();
            eBtnImg.color = new Color(0.9f, 0.7f, 0.2f, 0.95f);
            var entryBtn = entryBtnGo.GetComponent<Button>();
            var eBtnRT = entryBtnGo.GetComponent<RectTransform>();
            eBtnRT.anchorMin = eBtnRT.anchorMax = new Vector2(0.5f, 0.4f);
            eBtnRT.sizeDelta = new Vector2(300, 100);
            CreateText(entryBtnGo.transform, "点击进入围猎", 28).rectTransform.anchoredPosition = Vector2.zero;

            // 红点（挂 HuntEntryController）
            var redDotGo = new GameObject("RedDot", typeof(RectTransform), typeof(Image), typeof(RedDotHelper));
            redDotGo.transform.SetParent(entryBtnGo.transform, false);
            var rdRT = redDotGo.GetComponent<RectTransform>();
            rdRT.anchorMin = rdRT.anchorMax = new Vector2(1, 1);
            rdRT.pivot = new Vector2(0, 0.5f);
            rdRT.sizeDelta = new Vector2(24, 24);
            var rdImg = redDotGo.GetComponent<Image>();
            rdImg.color = Color.red;
            redDotGo.GetComponent<RedDotHelper>().redDotImage = rdImg;

            var ctrl = panelEntry.AddComponent<HuntEntryController>();
            ctrl.entryBtn = entryBtn;
            ctrl.redDot = redDotGo.GetComponent<RedDotHelper>();
            ctrl.BindUI(); // ⚠️ 必须在所有字段赋值完后调，AddComponent 触发的 OnEnable 拿不到 entryBtn
        }

        private static void BuildTeamBuildingPanel(GameObject panel)
        {
            var rt = (RectTransform)panel.transform;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = panel.GetComponent<Image>();
            img.color = new Color(0.05f, 0.1f, 0.15f, 0.95f);

            // 标题
            CreateText(panel.transform, "组队阶段", 36, TextAnchor.MiddleCenter, Color.white).rectTransform.sizeDelta = new Vector2(300, 60);

            var ui = panel.AddComponent<TeamBuildingUI>();

            // === 队伍展示 ===
            var memberArea = new GameObject("MemberArea", typeof(RectTransform), typeof(VerticalLayoutGroup));
            memberArea.transform.SetParent(panel.transform, false);
            var maRT = memberArea.GetComponent<RectTransform>();
            maRT.anchorMin = new Vector2(0, 0.5f);
            maRT.anchorMax = new Vector2(1, 0.5f);
            maRT.pivot = new Vector2(0.5f, 0.5f);
            maRT.sizeDelta = new Vector2(0, 200);
            var vlg = memberArea.GetComponent<VerticalLayoutGroup>();
            vlg.childForceExpandHeight = false;
            vlg.spacing = 10;

            // 成员槽 Prefab（带 TeamMemberSlotUI）
            var memberSlot = new GameObject("MemberSlot", typeof(RectTransform), typeof(Image));
            var msRT = memberSlot.GetComponent<RectTransform>();
            msRT.sizeDelta = new Vector2(240, 50);
            memberSlot.GetComponent<Image>().color = new Color(0.2f, 0.4f, 0.6f, 0.7f);
            CreateText(memberSlot.transform, "玩家名", 22).rectTransform.anchoredPosition = Vector2.zero;
            memberSlot.AddComponent<TeamMemberSlotUI>();
            ui.memberSlotPrefab = memberSlot;
            ui.memberRoot = memberArea.transform;

            // === Tab 区域 ===
            var tabArea = new GameObject("TabArea", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            tabArea.transform.SetParent(panel.transform, false);
            var taRT = tabArea.GetComponent<RectTransform>();
            taRT.anchorMin = new Vector2(0, 0.72f);
            taRT.anchorMax = new Vector2(1, 0.72f);
            taRT.sizeDelta = new Vector2(0, 80);
            var hlg = tabArea.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;

            ui.tabRecommendBtn = CreateButton(tabArea.transform, "推荐玩家", 20);
            ui.tabInviteBtn    = CreateButton(tabArea.transform, "收到的邀请", 20);
            ui.tabFriendBtn    = CreateButton(tabArea.transform, "好友", 20);

            // === Tab 内容面板 ===
            ui.panelRecommend = CreateTabContent(panel.transform, "RecommendPanel", ui.recommendUI = new GameObject("RecommendPanel", typeof(RectTransform)).AddComponent<RecommendListUI>());
            ui.panelInvite    = CreateTabContent(panel.transform, "InvitePanel",    ui.inviteUI    = new GameObject("InvitePanel", typeof(RectTransform)).AddComponent<InviteListUI>());
            ui.panelFriend    = CreateTabContent(panel.transform, "FriendPanel",    ui.friendUI    = new GameObject("FriendPanel", typeof(RectTransform)).AddComponent<FriendListUI>());

            // 给三个 Tab UI 填 listRoot / itemPrefab / emptyText
            BuildListFor(ui.recommendUI);
            BuildListFor(ui.inviteUI);
            BuildListFor(ui.friendUI);

            // === 倒计时 ===
            ui.countdownText = CreateText(panel.transform, "倒计时: 05:00", 24, TextAnchor.MiddleCenter, Color.yellow);
            var cdRT = ui.countdownText.rectTransform;
            cdRT.anchorMin = cdRT.anchorMax = new Vector2(0.5f, 0.85f);
            cdRT.pivot = new Vector2(0.5f, 0.5f);
            cdRT.sizeDelta = new Vector2(300, 40);

            // === 开始狩猎按钮 ===
            ui.startBtn = CreateButton(panel.transform, "开始狩猎 ▶", 26);
            var sRT = (RectTransform)ui.startBtn.transform;
            sRT.anchorMin = sRT.anchorMax = new Vector2(0.5f, 0.1f);
            sRT.pivot = new Vector2(0.5f, 0.5f);
            sRT.sizeDelta = new Vector2(240, 80);

            // === 退出队伍按钮（挂在 memberSlotPrefab 的 TeamMemberSlotUI 里，实际留空也 OK）===
            ui.leaveTeamBtn = null; // 简化：UI 上暂不做，功能在代码里留了

            ui.leaveConfirmDialog = new GameObject("LeaveConfirmDialog");
            ui.leaveConfirmDialog.SetActive(false);
            ui.BindUI();
        }

        private static GameObject CreateTabContent(Transform parent, string name, Component tabUIComp)
        {
            var go = tabUIComp.gameObject;
            go.name = name;
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.08f);
            rt.anchorMax = new Vector2(1, 0.08f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(0, 260);
            go.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.2f, 0.7f);
            go.GetComponent<Image>().raycastTarget = false; // Tab 容器不拦截子内容射线
            return go;
        }

        private static void BuildListFor(Component listUIComp)
        {
            var go = listUIComp.gameObject;

            // Content root
            var listRoot = new GameObject("ListRoot", typeof(RectTransform));
            listRoot.transform.SetParent(go.transform, false);
            var lrRT = listRoot.GetComponent<RectTransform>();
            lrRT.anchorMin = Vector2.zero;  lrRT.anchorMax = Vector2.one;
            lrRT.offsetMin = Vector2.zero;  lrRT.offsetMax = Vector2.zero;

            // 条目 Prefab
            var itemPrefab = new GameObject("ItemPrefab", typeof(RectTransform), typeof(Image), typeof(Button));
            itemPrefab.SetActive(false);
            var ipImg = itemPrefab.GetComponent<Image>();
            ipImg.color = new Color(0.25f, 0.4f, 0.55f, 0.9f);
            var ipRT = itemPrefab.GetComponent<RectTransform>();
            ipRT.sizeDelta = new Vector2(0, 50);

            var txt = CreateText(itemPrefab.transform, "玩家名", 20, TextAnchor.MiddleLeft);
            var txtRT = txt.rectTransform;
            txtRT.anchorMin = new Vector2(0, 0.5f);
            txtRT.anchorMax = new Vector2(0.5f, 0.5f);
            txtRT.pivot = new Vector2(0, 0.5f);
            txtRT.sizeDelta = new Vector2(200, 40);
            txtRT.anchoredPosition = new Vector2(20, 0);

            var btn = CreateButton(itemPrefab.transform, "邀请", 18);
            var bRT = (RectTransform)btn.transform;
            bRT.anchorMin = new Vector2(1, 0.5f);
            bRT.anchorMax = new Vector2(1, 0.5f);
            bRT.pivot = new Vector2(1, 0.5f);
            bRT.sizeDelta = new Vector2(120, 40);
            bRT.anchoredPosition = new Vector2(-20, 0);

            // 空提示
            var emptyGO = new GameObject("EmptyText", typeof(RectTransform));
            emptyGO.transform.SetParent(go.transform, false);
            var emptyTxt = CreateText(emptyGO.transform, "暂无数据", 22);
            var eRT = emptyTxt.rectTransform;
            eRT.anchorMin = Vector2.zero; eRT.anchorMax = Vector2.one;
            eRT.offsetMin = Vector2.zero; eRT.offsetMax = Vector2.zero;

            // 绑定到具体 ListUI
            if (listUIComp is RecommendListUI rec)
            {
                rec.listRoot = listRoot.transform;
                rec.itemPrefab = itemPrefab;
                rec.emptyText = emptyTxt;
                // 给 itemPrefab 挂 RecommendItemUI（Awake 会 AddListener 到已有 Button）
                itemPrefab.AddComponent<RecommendItemUI>();
            }
            else if (listUIComp is InviteListUI inv)
            {
                inv.listRoot = listRoot.transform;
                inv.itemPrefab = itemPrefab;
                inv.emptyText = emptyTxt;
                // InviteListUI 需要带 Accept + Reject 的条目 Prefab —— 重新搭一个
                itemPrefab.name = "InviteItemPrefab";
                Object.Destroy(ipImg); Object.Destroy(btn.gameObject); Object.Destroy(txt.gameObject);
                var inviteBtnGo = new GameObject("BtnGroup", typeof(RectTransform));
                inviteBtnGo.transform.SetParent(itemPrefab.transform, false);
                var igRT = inviteBtnGo.GetComponent<RectTransform>();
                igRT.anchorMin = new Vector2(1, 0.5f); igRT.anchorMax = new Vector2(1, 0.5f);
                igRT.pivot = new Vector2(1, 0.5f); igRT.sizeDelta = new Vector2(260, 40);
                igRT.anchoredPosition = new Vector2(-20, 0);

                var accept = CreateButton(inviteBtnGo.transform, "接受", 18);
                var acceptRT = (RectTransform)accept.transform;
                acceptRT.anchorMin = acceptRT.anchorMax = new Vector2(0.25f, 0.5f);
                acceptRT.sizeDelta = new Vector2(120, 40);

                var reject = CreateButton(inviteBtnGo.transform, "拒绝", 18);
                var rejectRT = (RectTransform)reject.transform;
                rejectRT.anchorMin = rejectRT.anchorMax = new Vector2(0.75f, 0.5f);
                rejectRT.sizeDelta = new Vector2(120, 40);

                var inviteTxt = CreateText(itemPrefab.transform, "邀请者", 20, TextAnchor.MiddleLeft);
                var itRT = inviteTxt.rectTransform;
                itRT.anchorMin = new Vector2(0, 0.5f); itRT.anchorMax = new Vector2(0.5f, 0.5f);
                itRT.pivot = new Vector2(0, 0.5f); itRT.sizeDelta = new Vector2(200, 40);
                itRT.anchoredPosition = new Vector2(20, 0);

                var invItem = itemPrefab.AddComponent<InviteItemUI>();
                invItem.nicknameText = inviteTxt;
                invItem.acceptBtn = accept;
                invItem.rejectBtn = reject;
            }
            else if (listUIComp is FriendListUI f)
            {
                f.listRoot = listRoot.transform;
                f.itemPrefab = itemPrefab;
                f.emptyText = emptyTxt;
                itemPrefab.AddComponent<FriendItemUI>();
            }
        }

        private static void BuildDigimonSelectPanel(GameObject panel)
        {
            panel.GetComponent<Image>().color = new Color(0.05f, 0.1f, 0.15f, 0.95f);
            CreateText(panel.transform, "选择参战数码兽", 36, TextAnchor.MiddleCenter, Color.white).rectTransform.sizeDelta = new Vector2(500, 60);

            var ui = panel.AddComponent<DigimonSelectUI>();

            // 队友区域
            var tmArea = new GameObject("TeammateArea", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            tmArea.transform.SetParent(panel.transform, false);
            var tmRT = tmArea.GetComponent<RectTransform>();
            tmRT.anchorMin = new Vector2(0, 0.7f); tmRT.anchorMax = new Vector2(1, 0.7f);
            tmRT.pivot = new Vector2(0.5f, 0.5f); tmRT.sizeDelta = new Vector2(0, 180);
            tmArea.GetComponent<HorizontalLayoutGroup>().spacing = 20;

            // 队友槽 Prefab
            var tmSlot = new GameObject("TeammateSlot", typeof(RectTransform));
            var tmRT2 = tmSlot.GetComponent<RectTransform>();
            tmRT2.sizeDelta = new Vector2(140, 160);
            CreateImage(tmSlot.transform, "WaitingMask", new Color(0.3f, 0.3f, 0.3f, 0.8f));
            CreateText(tmSlot.transform, "未选", 20);
            tmSlot.AddComponent<DigimonTeammateSlotUI>();
            ui.teammateSlotPrefab = tmSlot;
            ui.teammateRoot = tmArea.transform;

            // 数码兽列表
            var listArea = new GameObject("DigimonList", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listArea.transform.SetParent(panel.transform, false);
            var laRT = listArea.GetComponent<RectTransform>();
            laRT.anchorMin = new Vector2(0, 0.08f); laRT.anchorMax = new Vector2(1, 0.5f);
            laRT.pivot = new Vector2(0.5f, 0f); laRT.sizeDelta = new Vector2(0, 500);
            listArea.GetComponent<VerticalLayoutGroup>().spacing = 10;

            var dItem = new GameObject("DigimonItem", typeof(RectTransform), typeof(Image), typeof(Button));
            var dRT = dItem.GetComponent<RectTransform>();
            dRT.sizeDelta = new Vector2(0, 80);
            dItem.GetComponent<Image>().color = new Color(0.2f, 0.4f, 0.6f, 0.9f);
            var dSelFrame = CreateImage(dItem.transform, "SelectionFrame", new Color(1f, 0.9f, 0.2f, 0.8f));
            dSelFrame.rectTransform.anchorMin = Vector2.zero; dSelFrame.rectTransform.anchorMax = Vector2.one;
            dSelFrame.rectTransform.offsetMin = new Vector2(-4, -4); dSelFrame.rectTransform.offsetMax = new Vector2(4, 4);
            dSelFrame.enabled = false;

            var dName = CreateText(dItem.transform, "数码兽名", 24, TextAnchor.MiddleLeft);
            dName.rectTransform.anchorMin = new Vector2(0, 0.5f); dName.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            dName.rectTransform.pivot = new Vector2(0, 0.5f);
            dName.rectTransform.sizeDelta = new Vector2(300, 60);
            dName.rectTransform.anchoredPosition = new Vector2(20, 0);

            var dQText = CreateText(dItem.transform, "品质", 22, TextAnchor.MiddleRight, Color.yellow);
            dQText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f); dQText.rectTransform.anchorMax = new Vector2(1, 0.5f);
            dQText.rectTransform.pivot = new Vector2(1, 0.5f);
            dQText.rectTransform.sizeDelta = new Vector2(200, 60);
            dQText.rectTransform.anchoredPosition = new Vector2(-20, 0);

            var dUI = dItem.AddComponent<DigimonItemUI>();
            dUI.nameText = dName; dUI.qualityText = dQText; dUI.selectionFrame = dSelFrame.GetComponent<Image>();
            // 因为 DigimonItemUI.Awake 会挂 Button.onClick，但我们手动 AddComponent 后 Button 已经在，需手动确保 Awake 跑了
            dItem.SetActive(false); dItem.SetActive(true); // 强制 Awake

            ui.digimonItemPrefab = dItem;
            ui.listRoot = listArea.transform;

            // 按钮
            ui.confirmBtn = CreateButton(panel.transform, "确定 → 进入猎场", 26);
            var cfRT = (RectTransform)ui.confirmBtn.transform;
            cfRT.anchorMin = cfRT.anchorMax = new Vector2(0.5f, 0.05f);
            cfRT.sizeDelta = new Vector2(280, 80);

            ui.closeBtn = CreateButton(panel.transform, "返回", 22);
            var clRT = (RectTransform)ui.closeBtn.transform;
            clRT.anchorMin = new Vector2(0, 1); clRT.anchorMax = new Vector2(0, 1);
            clRT.pivot = new Vector2(0, 1);
            clRT.sizeDelta = new Vector2(120, 60);
            clRT.anchoredPosition = new Vector2(20, -20);
            ui.BindUI();
        }

        private static void BuildRoomSelectPanel(GameObject panel)
        {
            panel.GetComponent<Image>().color = new Color(0.05f, 0.1f, 0.15f, 0.95f);
            CreateText(panel.transform, "选择猎场", 36, TextAnchor.MiddleCenter, Color.white).rectTransform.sizeDelta = new Vector2(400, 60);

            var ui = panel.AddComponent<RoomSelectUI>();

            // 排行榜预览区
            var rankArea = new GameObject("RankPreview", typeof(RectTransform));
            rankArea.transform.SetParent(panel.transform, false);
            var raRT = rankArea.GetComponent<RectTransform>();
            raRT.anchorMin = new Vector2(0, 0.9f); raRT.anchorMax = new Vector2(0.45f, 0.98f);
            raRT.pivot = new Vector2(0, 1);
            raRT.sizeDelta = Vector2.zero;
            rankArea.AddComponent<Image>().color = new Color(0.15f, 0.2f, 0.3f, 0.8f);
            rankArea.GetComponent<Image>().raycastTarget = false;

            ui.rankText = CreateText(rankArea.transform, "未上榜", 22, TextAnchor.MiddleLeft, Color.yellow);
            var rRT = ui.rankText.rectTransform;
            rRT.anchorMin = new Vector2(0, 0); rRT.anchorMax = new Vector2(1, 0.5f);
            rRT.offsetMin = new Vector2(10, 0); rRT.offsetMax = new Vector2(-10, 0);

            ui.rankRewardPreviewText = CreateText(rankArea.transform, "暂无奖励", 20, TextAnchor.MiddleLeft, Color.white);
            var rrRT = ui.rankRewardPreviewText.rectTransform;
            rrRT.anchorMin = new Vector2(0, 0.5f); rrRT.anchorMax = new Vector2(1, 1f);
            rrRT.offsetMin = new Vector2(10, 0); rrRT.offsetMax = new Vector2(-10, 0);

            ui.rankDetailBtn = CreateButton(rankArea.transform, "查看排行榜", 18);
            var rdRT = (RectTransform)ui.rankDetailBtn.transform;
            rdRT.anchorMin = rdRT.anchorMax = new Vector2(0.9f, 0.5f);
            rdRT.sizeDelta = new Vector2(160, 40);

            // 奖励按钮 + 红点
            ui.scoreRewardBtn = CreateButton(panel.transform, "积分奖励", 20);
            var srRT = (RectTransform)ui.scoreRewardBtn.transform;
            srRT.anchorMin = srRT.anchorMax = new Vector2(1, 0.93f);
            srRT.pivot = new Vector2(1, 0.5f);
            srRT.anchoredPosition = new Vector2(-20, 0);
            srRT.sizeDelta = new Vector2(160, 60);

            var srDot = new GameObject("ScoreRedDot", typeof(RectTransform), typeof(Image));
            srDot.transform.SetParent(ui.scoreRewardBtn.transform, false);
            var srdRT = srDot.GetComponent<RectTransform>();
            srdRT.anchorMin = srdRT.anchorMax = new Vector2(1, 1);
            srdRT.pivot = new Vector2(0, 0.5f);
            srdRT.sizeDelta = new Vector2(24, 24);
            var srdImg = srDot.GetComponent<Image>();
            srdImg.color = Color.red;
            ui.scoreRewardRedDot = srDot;
            srDot.SetActive(false);

            // 猎场滚动区
            var scrollArea = new GameObject("ScrollArea", typeof(RectTransform));
            scrollArea.transform.SetParent(panel.transform, false);
            var scRT = scrollArea.GetComponent<RectTransform>();
            scRT.anchorMin = new Vector2(0, 0.3f); scRT.anchorMax = new Vector2(1, 0.85f);
            scRT.offsetMin = scRT.offsetMax = Vector2.zero;
            scrollArea.AddComponent<Image>().color = new Color(0.1f, 0.15f, 0.2f, 0.7f);
            scrollArea.GetComponent<Image>().raycastTarget = false; // 滚动容器不拦截 RoomCard 点击

            var content = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            content.transform.SetParent(scrollArea.transform, false);
            var cRT = content.GetComponent<RectTransform>();
            cRT.anchorMin = cRT.anchorMax = new Vector2(0.5f, 0.5f);
            cRT.sizeDelta = new Vector2(3000, 0);
            var hlg = content.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 30;
            hlg.childForceExpandHeight = false;

            ui.roomScrollContent = cRT;

            // 左右按钮
            ui.leftBtn  = CreateButton(panel.transform, "◀", 36);
            ui.rightBtn = CreateButton(panel.transform, "▶", 36);
            var lBtnRT = (RectTransform)ui.leftBtn.transform;
            lBtnRT.anchorMin = lBtnRT.anchorMax = new Vector2(0.05f, 0.55f);
            lBtnRT.sizeDelta = new Vector2(80, 80);
            var rBtnRT = (RectTransform)ui.rightBtn.transform;
            rBtnRT.anchorMin = rBtnRT.anchorMax = new Vector2(0.95f, 0.55f);
            rBtnRT.sizeDelta = new Vector2(80, 80);

            // Dot 指示器
            var dotRoot = new GameObject("DotRoot", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            dotRoot.transform.SetParent(panel.transform, false);
            var drRT = dotRoot.GetComponent<RectTransform>();
            drRT.anchorMin = drRT.anchorMax = new Vector2(0.5f, 0.32f);
            drRT.pivot = new Vector2(0.5f, 0.5f);
            drRT.sizeDelta = new Vector2(300, 30);
            dotRoot.GetComponent<HorizontalLayoutGroup>().spacing = 20;

            ui.dotIndicatorRoot = drRT;

            var dot = new GameObject("Dot", typeof(RectTransform), typeof(Image));
            var dotRT = dot.GetComponent<RectTransform>();
            dotRT.sizeDelta = new Vector2(20, 20);
            dot.GetComponent<Image>().color = new Color(1, 1, 1, 0.3f);
            dot.SetActive(false);
            ui.dotPrefab = dot;

            // 猎场卡片 Prefab
            var card = new GameObject("RoomCard", typeof(RectTransform), typeof(Image));
            var cRT2 = card.GetComponent<RectTransform>();
            cRT2.sizeDelta = new Vector2(280, 220);
            card.GetComponent<Image>().color = new Color(0.3f, 0.5f, 0.7f, 0.9f);
            var cardSelFrame = CreateImage(card.transform, "SelectedFrame", new Color(1f, 0.9f, 0.2f, 0.9f));
            cardSelFrame.rectTransform.anchorMin = Vector2.zero; cardSelFrame.rectTransform.anchorMax = Vector2.one;
            cardSelFrame.rectTransform.offsetMin = new Vector2(-4, -4); cardSelFrame.rectTransform.offsetMax = new Vector2(4, 4);
            cardSelFrame.enabled = false;
            var cName = CreateText(card.transform, "猎场名", 26, TextAnchor.MiddleCenter, Color.white);
            cName.rectTransform.anchorMin = new Vector2(0, 0.55f); cName.rectTransform.anchorMax = new Vector2(1, 0.8f);
            cName.rectTransform.offsetMin = Vector2.zero; cName.rectTransform.offsetMax = Vector2.zero;
            var cMul = CreateText(card.transform, "100x", 36, TextAnchor.MiddleCenter, Color.yellow);
            cMul.rectTransform.anchorMin = new Vector2(0, 0.3f); cMul.rectTransform.anchorMax = new Vector2(1, 0.55f);
            cMul.rectTransform.offsetMin = Vector2.zero; cMul.rectTransform.offsetMax = Vector2.zero;
            var cUnlock = CreateText(card.transform, "已解锁", 18, TextAnchor.MiddleCenter, Color.gray);
            cUnlock.rectTransform.anchorMin = new Vector2(0, 0f); cUnlock.rectTransform.anchorMax = new Vector2(1, 0.3f);
            cUnlock.rectTransform.offsetMin = Vector2.zero; cUnlock.rectTransform.offsetMax = Vector2.zero;
            var cLockMask = CreateImage(card.transform, "LockedMask", new Color(0, 0, 0, 0.6f));
            cLockMask.rectTransform.anchorMin = Vector2.zero; cLockMask.rectTransform.anchorMax = Vector2.one;

            var cardUI = card.AddComponent<RoomCardUI>();
            cardUI.nameText = cName; cardUI.multipleText = cMul; cardUI.unlockText = cUnlock;
            cardUI.lockedMask = cLockMask; cardUI.selectedFrame = cardSelFrame.GetComponent<Image>();
            card.SetActive(false);
            ui.roomCardPrefab = card;

            // 数码兽展示区 + 切换按钮
            var dmArea = new GameObject("DigimonArea", typeof(RectTransform));
            dmArea.transform.SetParent(panel.transform, false);
            var daRT = dmArea.GetComponent<RectTransform>();
            daRT.anchorMin = new Vector2(0, 0.05f); daRT.anchorMax = new Vector2(1, 0.28f);
            daRT.offsetMin = daRT.offsetMax = Vector2.zero;
            dmArea.AddComponent<Image>().color = new Color(0.15f, 0.18f, 0.25f, 0.8f);
            dmArea.GetComponent<Image>().raycastTarget = false;
            ui.digimonDisplayRoot = daRT;

            ui.digimonSwitchBtn = CreateButton(panel.transform, "切换数码兽", 18);
            var dsRT = (RectTransform)ui.digimonSwitchBtn.transform;
            dsRT.anchorMin = dsRT.anchorMax = new Vector2(1, 0.08f);
            dsRT.pivot = new Vector2(1, 0.5f);
            dsRT.sizeDelta = new Vector2(160, 50);
            dsRT.anchoredPosition = new Vector2(-20, 0);

            // 开始围猎
            ui.startHuntBtn = CreateButton(panel.transform, "🔥 开始围猎 🔥", 28);
            var shRT = (RectTransform)ui.startHuntBtn.transform;
            shRT.anchorMin = shRT.anchorMax = new Vector2(0.5f, 0.02f);
            shRT.pivot = new Vector2(0.5f, 0.5f);
            shRT.sizeDelta = new Vector2(320, 90);
            ui.BindUI();
        }

        private static void BuildHuntRoomPanel(GameObject panel)
        {
            panel.GetComponent<Image>().color = new Color(0.05f, 0.12f, 0.08f, 1f);

            // 中央猎场区：先创建并保持 inactive。
            // 原因：AddComponent 会同步触发 Awake/OnEnable，此时所有 Inspector 字段还是 null，
            // OnEnable 里的 RefreshXXX 全部白跑。设为 inactive 可以推迟到 SetActive(true) 时再初始化。
            var huntArea = new GameObject("HuntArea", typeof(RectTransform), typeof(Image));
            huntArea.transform.SetParent(panel.transform, false);
            var haRT = huntArea.GetComponent<RectTransform>();
            haRT.anchorMin = new Vector2(0, 0.15f); haRT.anchorMax = new Vector2(1, 0.85f);
            haRT.offsetMin = haRT.offsetMax = Vector2.zero;
            var haImg = huntArea.GetComponent<Image>();
            haImg.color = new Color(0.2f, 0.35f, 0.2f, 1f);
            haImg.raycastTarget = true; // 猎场区必须接收点击/长按（HuntRoomUI 实现了 IPointerDownHandler）

            huntArea.SetActive(false);
            var ui = huntArea.AddComponent<HuntRoomUI>();
            ui.huntArea = haRT;

            // 怪物容器：铺满猎场区并居中，作为怪物坐标的基准。
            // 原来 anchorMin=anchorMax=0 且尺寸为 0，会导致 ±w/2 的坐标映射整体偏移半个屏幕。
            var monsterContainer = new GameObject("MonsterContainer", typeof(RectTransform));
            monsterContainer.transform.SetParent(huntArea.transform, false);
            var mcRT = monsterContainer.GetComponent<RectTransform>();
            mcRT.anchorMin = Vector2.zero; mcRT.anchorMax = Vector2.one;
            mcRT.offsetMin = Vector2.zero; mcRT.offsetMax = Vector2.zero;
            mcRT.pivot = new Vector2(0.5f, 0.5f);
            ui.monsterContainer = monsterContainer.transform;

            // 顶部信息栏
            var topBar = new GameObject("TopBar", typeof(RectTransform));
            topBar.transform.SetParent(panel.transform, false);
            var tbRT = topBar.GetComponent<RectTransform>();
            tbRT.anchorMin = new Vector2(0, 1); tbRT.anchorMax = new Vector2(1, 1);
            tbRT.pivot = new Vector2(0.5f, 1); tbRT.sizeDelta = new Vector2(0, 80);
            tbRT.anchoredPosition = new Vector2(0, 0);
            topBar.AddComponent<Image>().color = new Color(0.15f, 0.2f, 0.15f, 0.9f);
            topBar.GetComponent<Image>().raycastTarget = false;

            ui.tokenText  = CreateText(topBar.transform, "代币: 0", 22, TextAnchor.MiddleCenter, Color.cyan);
            var ttRT = ui.tokenText.rectTransform;
            ttRT.anchorMin = new Vector2(0, 0); ttRT.anchorMax = new Vector2(0.33f, 1);
            ttRT.offsetMin = Vector2.zero; ttRT.offsetMax = Vector2.zero;

            ui.scoreText  = CreateText(topBar.transform, "积分: 0", 22, TextAnchor.MiddleCenter, Color.yellow);
            var stRT = ui.scoreText.rectTransform;
            stRT.anchorMin = new Vector2(0.33f, 0); stRT.anchorMax = new Vector2(0.66f, 1);
            stRT.offsetMin = Vector2.zero; stRT.offsetMax = Vector2.zero;

            ui.teamScoreText = CreateText(topBar.transform, "团队: 0", 22, TextAnchor.MiddleCenter, Color.green);
            var tsRT = ui.teamScoreText.rectTransform;
            tsRT.anchorMin = new Vector2(0.66f, 0); tsRT.anchorMax = new Vector2(1, 1);
            tsRT.offsetMin = Vector2.zero; tsRT.offsetMax = Vector2.zero;

            // 玩家槽位区（底部）
            var psArea = new GameObject("PlayerSlots", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            psArea.transform.SetParent(panel.transform, false);
            var psRT = psArea.GetComponent<RectTransform>();
            psRT.anchorMin = new Vector2(0, 0); psRT.anchorMax = new Vector2(1, 0.15f);
            psRT.offsetMin = psRT.offsetMax = Vector2.zero;
            var hlg = psArea.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 30;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.padding = new RectOffset(40, 40, 20, 20);
            ui.playerSlotRoot = psArea.transform;

            // 道具按钮（顶部左侧，避开底部玩家槽位区，避免互相遮挡）
            ui.fireItemBtn = CreateButton(panel.transform, "狂暴", 18);
            var fbRT = (RectTransform)ui.fireItemBtn.transform;
            fbRT.anchorMin = fbRT.anchorMax = new Vector2(0.1f, 0.93f);
            fbRT.pivot = new Vector2(0.5f, 0.5f);
            fbRT.sizeDelta = new Vector2(120, 60);
            fbRT.anchoredPosition = Vector2.zero;
            ui.fireItemCountText = CreateText(panel.transform, "x0", 16, TextAnchor.MiddleCenter, Color.white);
            var fctRT = ui.fireItemCountText.rectTransform;
            fctRT.anchorMin = fctRT.anchorMax = new Vector2(0.1f, 0.88f);
            fctRT.pivot = new Vector2(0.5f, 0.5f);
            fctRT.sizeDelta = new Vector2(60, 30);

            ui.lockItemBtn = CreateButton(panel.transform, "锁定", 18);
            var lbRT = (RectTransform)ui.lockItemBtn.transform;
            lbRT.anchorMin = lbRT.anchorMax = new Vector2(0.25f, 0.93f);
            lbRT.pivot = new Vector2(0.5f, 0.5f);
            lbRT.sizeDelta = new Vector2(120, 60);
            lbRT.anchoredPosition = Vector2.zero;
            ui.lockItemCountText = CreateText(panel.transform, "x0", 16, TextAnchor.MiddleCenter, Color.white);
            var lctRT = ui.lockItemCountText.rectTransform;
            lctRT.anchorMin = lctRT.anchorMax = new Vector2(0.25f, 0.88f);
            lctRT.pivot = new Vector2(0.5f, 0.5f);
            lctRT.sizeDelta = new Vector2(60, 30);

            // 锁定目标指示器
            var lockInd = new GameObject("LockIndicator", typeof(RectTransform), typeof(Image));
            lockInd.transform.SetParent(panel.transform, false);
            var liRT = lockInd.GetComponent<RectTransform>();
            liRT.anchorMin = liRT.anchorMax = new Vector2(0.5f, 0.2f);
            liRT.sizeDelta = new Vector2(80, 80);
            lockInd.GetComponent<Image>().color = new Color(1, 0.5f, 0, 0.8f);
            lockInd.SetActive(false);
            ui.lockTargetIndicator = lockInd;

            // 退出按钮
            ui.exitBtn = CreateButton(panel.transform, "退出猎场", 18);
            var exRT = (RectTransform)ui.exitBtn.transform;
            exRT.anchorMin = exRT.anchorMax = new Vector2(1, 0.93f);
            exRT.pivot = new Vector2(1, 0.5f);
            exRT.sizeDelta = new Vector2(160, 50);
            exRT.anchoredPosition = new Vector2(-20, 0);

            // 子弹 Prefab（保持 inactive，实例化后由 HuntRoomUI 手动 SetActive(true)）
            var bullet = new GameObject("BulletPrefab", typeof(RectTransform), typeof(Image));
            bullet.transform.SetParent(panel.transform, false);
            var bRT = bullet.GetComponent<RectTransform>();
            bRT.sizeDelta = new Vector2(20, 20);
            var bImg = bullet.GetComponent<Image>();
            bImg.color = new Color(1, 0.9f, 0.3f, 1f);
            bImg.raycastTarget = false;
            bullet.SetActive(false);
            ui.bulletPrefab = bullet;

            // 飘字 Prefab —— 必须自带 Text，否则 HuntRoomUI 里 GetComponent<Text>() 拿到 null，飘字永远不显示
            var popup = new GameObject("ScorePopupPrefab", typeof(RectTransform), typeof(Text), typeof(ScorePopupAnimator));
            popup.transform.SetParent(panel.transform, false);
            var pRT = popup.GetComponent<RectTransform>();
            pRT.sizeDelta = new Vector2(160, 60);
            var pTxt = popup.GetComponent<Text>();
            pTxt.font = Font;
            pTxt.fontSize = 28;
            pTxt.alignment = TextAnchor.MiddleCenter;
            pTxt.color = Color.yellow;
            pTxt.raycastTarget = false;
            popup.SetActive(false);
            ui.scorePopupPrefab = popup;

            // Canvas（panel 可能在 inactive 状态下构建，includeInactive 才能找到父级 Canvas）
            ui.popupCanvas = huntArea.GetComponentInParent<Canvas>(true);

            // 手指引导
            var guide = new GameObject("FingerGuide", typeof(RectTransform));
            guide.transform.SetParent(panel.transform, false);
            var gdRT = guide.GetComponent<RectTransform>();
            gdRT.anchorMin = gdRT.anchorMax = new Vector2(0.5f, 0.45f);
            gdRT.sizeDelta = new Vector2(200, 100);
            var gdImg = guide.AddComponent<Image>();
            gdImg.color = new Color(1, 1, 1, 0.3f);
            gdImg.raycastTarget = false; // 引导手势不能拦截玩家点击怪物！
            var gdTxt = CreateText(guide.transform, "👆 点击或长按攻击怪物！", 24, TextAnchor.MiddleCenter, Color.white);
            gdTxt.rectTransform.anchorMin = Vector2.zero; gdTxt.rectTransform.anchorMax = Vector2.one;
            gdTxt.rectTransform.offsetMin = Vector2.zero; gdTxt.rectTransform.offsetMax = Vector2.zero;
            ui.fingerGuide = guide;

            // 所有字段赋值完成 → 绑定按钮事件 → 激活猎场区，
            // 此时 HuntRoomUI 的 Awake/OnEnable 才会带着完整引用执行一次
            ui.BindUI();
            huntArea.SetActive(true);
        }

        private static void BuildScoreRewardPanel(GameObject panel)
        {
            panel.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.2f, 0.98f);
            CreateText(panel.transform, "积分奖励", 32, TextAnchor.MiddleCenter, Color.white).rectTransform.sizeDelta = new Vector2(300, 60);

            var ui = panel.AddComponent<ScoreRewardUI>();

            ui.teamScoreText = CreateText(panel.transform, "团队积分: 0", 22, TextAnchor.MiddleCenter, Color.yellow);
            var tsRT = ui.teamScoreText.rectTransform;
            tsRT.anchorMin = tsRT.anchorMax = new Vector2(0.5f, 0.9f);
            tsRT.sizeDelta = new Vector2(300, 40);

            ui.closeBtn = CreateButton(panel.transform, "关闭", 20);
            var clRT = (RectTransform)ui.closeBtn.transform;
            clRT.anchorMin = clRT.anchorMax = new Vector2(1, 0.97f);
            clRT.pivot = new Vector2(1, 1);
            clRT.anchoredPosition = new Vector2(-20, -20);
            clRT.sizeDelta = new Vector2(120, 50);

            // 普通奖励列表
            var normalArea = new GameObject("NormalRewards", typeof(RectTransform), typeof(VerticalLayoutGroup));
            normalArea.transform.SetParent(panel.transform, false);
            var naRT = normalArea.GetComponent<RectTransform>();
            naRT.anchorMin = new Vector2(0, 0.3f); naRT.anchorMax = new Vector2(1, 0.85f);
            naRT.offsetMin = naRT.offsetMax = Vector2.zero;
            var vlg2 = normalArea.GetComponent<VerticalLayoutGroup>();
            vlg2.spacing = 10;
            vlg2.childForceExpandHeight = false;
            ui.normalRewardRoot = naRT;

            var rewardItem = new GameObject("RewardItem", typeof(RectTransform), typeof(Image));
            var riRT = rewardItem.GetComponent<RectTransform>();
            riRT.sizeDelta = new Vector2(0, 80);
            rewardItem.GetComponent<Image>().color = new Color(0.2f, 0.3f, 0.45f, 0.9f);

            var rDesc = CreateText(rewardItem.transform, "奖励描述", 22, TextAnchor.MiddleLeft, Color.white);
            rDesc.rectTransform.anchorMin = new Vector2(0, 0.7f); rDesc.rectTransform.anchorMax = new Vector2(0.6f, 1);
            rDesc.rectTransform.offsetMin = new Vector2(10, 0); rDesc.rectTransform.offsetMax = new Vector2(-10, 0);

            var rScore = CreateText(rewardItem.transform, "积分条件", 20, TextAnchor.MiddleLeft, Color.yellow);
            rScore.rectTransform.anchorMin = new Vector2(0, 0); rScore.rectTransform.anchorMax = new Vector2(0.6f, 0.35f);
            rScore.rectTransform.offsetMin = new Vector2(10, 0); rScore.rectTransform.offsetMax = new Vector2(-10, 0);

            var rReward = CreateText(rewardItem.transform, "奖励", 18, TextAnchor.MiddleRight, Color.cyan);
            rReward.rectTransform.anchorMin = new Vector2(0.6f, 0); rReward.rectTransform.anchorMax = new Vector2(1, 0.5f);
            rReward.rectTransform.offsetMin = new Vector2(-10, 0); rReward.rectTransform.offsetMax = new Vector2(-10, 0);

            var rClaimBtn = CreateButton(rewardItem.transform, "领取", 20);
            var rcbRT = (RectTransform)rClaimBtn.transform;
            rcbRT.anchorMin = new Vector2(0.7f, 0.5f); rcbRT.anchorMax = new Vector2(1, 1);
            rcbRT.offsetMin = new Vector2(-10, -5); rcbRT.offsetMax = new Vector2(-10, -5);

            var rClaimedTxt = CreateText(rewardItem.transform, "已领取 ✓", 22, TextAnchor.MiddleCenter, Color.green);
            var rcRT = rClaimedTxt.rectTransform;
            rcRT.anchorMin = new Vector2(0.7f, 0.5f); rcRT.anchorMax = new Vector2(1, 1);
            rcRT.offsetMin = new Vector2(-10, -5); rcRT.offsetMax = new Vector2(-10, -5);
            rClaimedTxt.gameObject.SetActive(false);

            var rUnlockedMask = CreateImage(rewardItem.transform, "LockedMask", new Color(0, 0, 0, 0.5f));
            rUnlockedMask.rectTransform.anchorMin = Vector2.zero; rUnlockedMask.rectTransform.anchorMax = Vector2.one;

            var rewardUI = rewardItem.AddComponent<ScoreRewardItemUI>();
            rewardUI.descText = rDesc; rewardUI.scoreText = rScore; rewardUI.rewardText = rReward;
            rewardUI.claimBtn = rClaimBtn; rewardUI.claimedText = rClaimedTxt; rewardUI.unlockedMask = rUnlockedMask;
            rewardItem.SetActive(false);
            ui.normalRewardItemPrefab = rewardItem;

            // 循环奖励
            var cycArea = new GameObject("CycleArea", typeof(RectTransform));
            cycArea.transform.SetParent(panel.transform, false);
            var caRT = cycArea.GetComponent<RectTransform>();
            caRT.anchorMin = new Vector2(0, 0.05f); caRT.anchorMax = new Vector2(1, 0.28f);
            caRT.offsetMin = caRT.offsetMax = Vector2.zero;
            cycArea.AddComponent<Image>().color = new Color(0.15f, 0.25f, 0.3f, 0.9f);
            cycArea.GetComponent<Image>().raycastTarget = false;

            ui.cycleStatusText = CreateText(cycArea.transform, "循环奖励: 等待解锁", 20, TextAnchor.MiddleCenter, Color.white);
            var csRT = ui.cycleStatusText.rectTransform;
            csRT.anchorMin = new Vector2(0, 0.6f); csRT.anchorMax = new Vector2(1, 1f);
            csRT.offsetMin = new Vector2(10, 0); csRT.offsetMax = new Vector2(-10, 0);

            // 进度条（Image fill）
            ui.cycleProgressBar = CreateImage(cycArea.transform, "ProgressBar", new Color(0.2f, 0.8f, 0.4f, 0.9f));
            ui.cycleProgressBar.type = Image.Type.Filled;
            ui.cycleProgressBar.fillMethod = Image.FillMethod.Horizontal;
            var cpRT = ui.cycleProgressBar.rectTransform;
            cpRT.anchorMin = new Vector2(0, 0.3f); cpRT.anchorMax = new Vector2(1, 0.6f);
            cpRT.offsetMin = new Vector2(10, -4); cpRT.offsetMax = new Vector2(-10, 4);

            ui.cycleProgressText = CreateText(cycArea.transform, "0%", 18, TextAnchor.MiddleCenter, Color.white);
            var cpTxtRT = ui.cycleProgressText.rectTransform;
            cpTxtRT.anchorMin = new Vector2(0, 0.3f); cpTxtRT.anchorMax = new Vector2(1, 0.6f);
            cpTxtRT.offsetMin = Vector2.zero; cpTxtRT.offsetMax = Vector2.zero;

            ui.cycleClaimBtn = CreateButton(cycArea.transform, "领取循环奖励", 20);
            var ccbRT = (RectTransform)ui.cycleClaimBtn.transform;
            ccbRT.anchorMin = ccbRT.anchorMax = new Vector2(0.5f, 0.1f);
            ccbRT.pivot = new Vector2(0.5f, 0.5f);
            ccbRT.sizeDelta = new Vector2(220, 40);
            ui.BindUI();
        }

        private static void BuildRankPanel(GameObject panel)
        {
            panel.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.18f, 0.98f);
            CreateText(panel.transform, "排行榜", 32, TextAnchor.MiddleCenter, Color.white).rectTransform.sizeDelta = new Vector2(300, 60);

            var ui = panel.AddComponent<RankUI>();

            // Tab 按钮
            ui.teamServerBtn       = CreateButton(panel.transform, "团队区服", 18);
            ui.teamAllServerBtn    = CreateButton(panel.transform, "团队全服", 18);
            ui.personalServerBtn   = CreateButton(panel.transform, "个人区服", 18);
            ui.personalAllServerBtn= CreateButton(panel.transform, "个人全服", 18);
            var tsRT = (RectTransform)ui.teamServerBtn.transform;  tsRT.anchorMin = tsRT.anchorMax = new Vector2(0.1f, 0.9f);  tsRT.sizeDelta = new Vector2(160, 50);
            var taRT = (RectTransform)ui.teamAllServerBtn.transform; taRT.anchorMin = taRT.anchorMax = new Vector2(0.3f, 0.9f);  taRT.sizeDelta = new Vector2(160, 50);
            var psRT = (RectTransform)ui.personalServerBtn.transform; psRT.anchorMin = psRT.anchorMax = new Vector2(0.55f, 0.9f); psRT.sizeDelta = new Vector2(160, 50);
            var paRT = (RectTransform)ui.personalAllServerBtn.transform; paRT.anchorMin = paRT.anchorMax = new Vector2(0.75f, 0.9f); paRT.sizeDelta = new Vector2(160, 50);

            // 我的排名
            ui.myRankText = CreateText(panel.transform, "未上榜", 24, TextAnchor.MiddleLeft, Color.yellow);
            var mrRT = ui.myRankText.rectTransform;
            mrRT.anchorMin = mrRT.anchorMax = new Vector2(0.1f, 0.8f);
            mrRT.sizeDelta = new Vector2(400, 40);

            ui.myScoreText = CreateText(panel.transform, "", 22, TextAnchor.MiddleLeft, Color.white);
            var msRT = ui.myScoreText.rectTransform;
            msRT.anchorMin = new Vector2(0.1f, 0.72f); msRT.anchorMax = new Vector2(0.5f, 0.8f);
            msRT.sizeDelta = new Vector2(400, 35);

            ui.myRewardText = CreateText(panel.transform, "", 20, TextAnchor.MiddleLeft, Color.green);
            var mrwRT = ui.myRewardText.rectTransform;
            mrwRT.anchorMin = new Vector2(0.55f, 0.72f); mrwRT.anchorMax = new Vector2(1f, 0.8f);
            mrwRT.offsetMin = mrwRT.offsetMax = Vector2.zero;

            // 列表
            var listArea = new GameObject("RankList", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listArea.transform.SetParent(panel.transform, false);
            var laRT = listArea.GetComponent<RectTransform>();
            laRT.anchorMin = new Vector2(0, 0.05f); laRT.anchorMax = new Vector2(1, 0.7f);
            laRT.offsetMin = laRT.offsetMax = Vector2.zero;
            var vlg3 = listArea.GetComponent<VerticalLayoutGroup>();
            vlg3.spacing = 6;
            vlg3.childForceExpandHeight = false;
            ui.rankListRoot = laRT;

            var rankItem = new GameObject("RankItem", typeof(RectTransform), typeof(Image));
            var riRT = rankItem.GetComponent<RectTransform>();
            riRT.sizeDelta = new Vector2(0, 50);
            rankItem.GetComponent<Image>().color = new Color(0.2f, 0.25f, 0.4f, 0.8f);

            var riRank = CreateText(rankItem.transform, "#1", 22, TextAnchor.MiddleCenter, Color.yellow);
            riRank.rectTransform.anchorMin = new Vector2(0, 0); riRank.rectTransform.anchorMax = new Vector2(0.1f, 1);
            riRank.rectTransform.offsetMin = riRank.rectTransform.offsetMax = Vector2.zero;

            var riName = CreateText(rankItem.transform, "玩家/队伍", 22, TextAnchor.MiddleLeft, Color.white);
            riName.rectTransform.anchorMin = new Vector2(0.1f, 0); riName.rectTransform.anchorMax = new Vector2(0.45f, 1);
            riName.rectTransform.offsetMin = new Vector2(10, 0); riName.rectTransform.offsetMax = Vector2.zero;

            var riScore = CreateText(rankItem.transform, "积分", 22, TextAnchor.MiddleRight, Color.cyan);
            riScore.rectTransform.anchorMin = new Vector2(0.45f, 0); riScore.rectTransform.anchorMax = new Vector2(0.9f, 1);
            riScore.rectTransform.offsetMin = Vector2.zero; riScore.rectTransform.offsetMax = Vector2.zero;

            var riMembers = CreateText(rankItem.transform, "", 18, TextAnchor.MiddleRight, Color.gray);
            riMembers.rectTransform.anchorMin = new Vector2(0.9f, 0); riMembers.rectTransform.anchorMax = new Vector2(1, 1);
            riMembers.rectTransform.offsetMin = Vector2.zero; riMembers.rectTransform.offsetMax = new Vector2(-10, 0);

            var rItemUI = rankItem.AddComponent<RankItemUI>();
            rItemUI.rankText = riRank; rItemUI.nameText = riName; rItemUI.scoreText = riScore; rItemUI.memberCountText = riMembers;
            rankItem.SetActive(false);
            ui.rankItemPrefab = rankItem;

            ui.closeBtn = CreateButton(panel.transform, "关闭", 20);
            var clRT = (RectTransform)ui.closeBtn.transform;
            clRT.anchorMin = clRT.anchorMax = new Vector2(1, 1);
            clRT.pivot = new Vector2(1, 1);
            clRT.anchoredPosition = new Vector2(-20, -20);
            clRT.sizeDelta = new Vector2(120, 50);
            ui.BindUI();
        }
    }
}
