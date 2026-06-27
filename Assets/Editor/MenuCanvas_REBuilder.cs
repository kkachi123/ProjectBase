using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Tools > UI > Build MenuCanvas_RE  을 실행하면 Scene에 MenuCanvas_RE 를 생성합니다.
/// </summary>
public static class MenuCanvas_REBuilder
{
    // ── Color palette ─────────────────────────────────────────────────────────
    static readonly Color C_Overlay    = new Color(0.04f, 0.04f, 0.08f, 0.92f);
    static readonly Color C_Panel      = new Color(0.10f, 0.08f, 0.05f, 0.97f);
    static readonly Color C_TopBar     = new Color(0.14f, 0.11f, 0.07f, 1.00f);
    static readonly Color C_TabActive  = new Color(0.80f, 0.60f, 0.15f, 1.00f);
    static readonly Color C_TabInact   = new Color(0.25f, 0.20f, 0.12f, 1.00f);
    static readonly Color C_TabText    = new Color(0.95f, 0.88f, 0.65f, 1.00f);
    static readonly Color C_SlotBG     = new Color(0.08f, 0.06f, 0.04f, 1.00f);
    static readonly Color C_SlotBorder = new Color(0.45f, 0.38f, 0.20f, 1.00f);
    static readonly Color C_Text       = new Color(0.90f, 0.84f, 0.68f, 1.00f);
    static readonly Color C_TextDim    = new Color(0.55f, 0.50f, 0.38f, 1.00f);
    static readonly Color C_Divider    = new Color(0.38f, 0.30f, 0.16f, 1.00f);
    static readonly Color C_DetailBG   = new Color(0.07f, 0.06f, 0.04f, 1.00f);
    static readonly Color C_Gold       = new Color(0.85f, 0.70f, 0.20f, 1.00f);
    static readonly Color C_Red        = new Color(0.75f, 0.20f, 0.15f, 1.00f);
    static readonly Color C_SliderFill = new Color(0.70f, 0.55f, 0.15f, 1.00f);

    // ── Entry point ───────────────────────────────────────────────────────────
    [MenuItem("Tools/UI/Build MenuCanvas_RE")]
    public static void Build()
    {
        var existing = GameObject.Find("MenuCanvas_RE");
        if (existing != null) Undo.DestroyObjectImmediate(existing);

        // Canvas
        var root = new GameObject("MenuCanvas_RE");
        Undo.RegisterCreatedObjectUndo(root, "Build MenuCanvas_RE");

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        // Dim background
        MakeImg("DimBackground", root.transform, C_Overlay, true);

        // Main panel with slight inset
        var mainPanel = MakeRect("MainPanel", root.transform);
        mainPanel.anchorMin = new Vector2(0.02f, 0.03f);
        mainPanel.anchorMax = new Vector2(0.98f, 0.97f);
        mainPanel.offsetMin = mainPanel.offsetMax = Vector2.zero;
        mainPanel.gameObject.AddComponent<Image>().color = C_Panel;

        // ── Top tab bar ──────────────────────────────────────────────────────
        var topBar = MakeRect("TopBar", mainPanel);
        topBar.anchorMin = new Vector2(0, 1);
        topBar.anchorMax = new Vector2(1, 1);
        topBar.pivot     = new Vector2(0.5f, 1f);
        topBar.sizeDelta = new Vector2(0, 52);
        topBar.gameObject.AddComponent<Image>().color = C_TopBar;

        var tabLayout = topBar.gameObject.AddComponent<HorizontalLayoutGroup>();
        tabLayout.childAlignment      = TextAnchor.MiddleCenter;
        tabLayout.childForceExpandWidth  = true;
        tabLayout.childForceExpandHeight = true;
        tabLayout.spacing = 2;
        tabLayout.padding = new RectOffset(2, 2, 2, 2);

        string[]   tabNames   = { "Equipment", "Inventory", "Map", "Settings" };
        var        tabButtons = new Button[tabNames.Length];
        for (int i = 0; i < tabNames.Length; i++)
            tabButtons[i] = MakeTabButton(tabNames[i], topBar);

        // ── Content area (below tab bar) ─────────────────────────────────────
        var contentArea = MakeRect("ContentArea", mainPanel);
        contentArea.anchorMin = Vector2.zero;
        contentArea.anchorMax = Vector2.one;
        contentArea.offsetMin = Vector2.zero;
        contentArea.offsetMax = new Vector2(0, -52);

        // ── Screens ──────────────────────────────────────────────────────────
        var screens = new UIScreen[]
        {
            BuildEquipmentScreen(contentArea).GetComponent<UIScreen>(),
            BuildInventoryScreen(contentArea).GetComponent<UIScreen>(),
            BuildMapScreen(contentArea).GetComponent<UIScreen>(),
            BuildSettingsScreen(contentArea).GetComponent<UIScreen>(),
        };

        // Show only first screen for editor preview
        for (int i = 0; i < screens.Length; i++)
            screens[i].gameObject.SetActive(i == 0);

        // ── Tab controller ────────────────────────────────────────────────────
        var controller = root.AddComponent<UIMenuTabController>();
        var so = new SerializedObject(controller);

        var propScreens = so.FindProperty("_screens");
        var propTabs    = so.FindProperty("_tabs");
        propScreens.arraySize = screens.Length;
        propTabs.arraySize    = tabButtons.Length;
        for (int i = 0; i < screens.Length; i++)
        {
            propScreens.GetArrayElementAtIndex(i).objectReferenceValue = screens[i];
            propTabs   .GetArrayElementAtIndex(i).objectReferenceValue = tabButtons[i];
        }
        so.ApplyModifiedProperties();

        // Highlight first tab
        tabButtons[0].GetComponent<Image>().color = C_TabActive;

        Selection.activeGameObject = root;
        Debug.Log("[MenuCanvas_RE] Built successfully.");
    }

    // ── Screen builders ───────────────────────────────────────────────────────

    static GameObject BuildEquipmentScreen(RectTransform parent)
    {
        var go = MakeScreenGO("EquipmentScreen", parent);
        go.AddComponent<EquipmentScreen>();
        var rt = go.GetComponent<RectTransform>();

        // ─ Left: character doll area ─────────────────────────────────────────
        var equipView = MakeRect("EquipmentView", rt);
        equipView.anchorMin = new Vector2(0,     0);
        equipView.anchorMax = new Vector2(0.58f, 1);
        equipView.offsetMin = new Vector2(8,  8);
        equipView.offsetMax = new Vector2(-4, -8);
        equipView.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.06f, 0.04f, 0.6f);

        // Title label
        var equipTitle = MakeTMP("EquipmentTitle", equipView, "EQUIPMENT", 11, C_TextDim);
        equipTitle.rectTransform.anchorMin = new Vector2(0, 1);
        equipTitle.rectTransform.anchorMax = new Vector2(1, 1);
        equipTitle.rectTransform.pivot     = new Vector2(0.5f, 1f);
        equipTitle.rectTransform.sizeDelta = new Vector2(0, 22);
        equipTitle.alignment = TextAlignmentOptions.Center;
        equipTitle.fontStyle = FontStyles.Bold;

        // Character silhouette frame (center of left panel)
        var charBorder = MakeImg("CharacterFrame", equipView, C_SlotBorder, false);
        charBorder.rectTransform.anchorMin       = new Vector2(0.5f, 0.5f);
        charBorder.rectTransform.anchorMax       = new Vector2(0.5f, 0.5f);
        charBorder.rectTransform.pivot           = new Vector2(0.5f, 0.5f);
        charBorder.rectTransform.sizeDelta       = new Vector2(170, 260);
        charBorder.rectTransform.anchoredPosition = new Vector2(0, 8);

        var charInner = MakeImg("CharInner", charBorder.transform, new Color(0.06f, 0.05f, 0.03f, 1f), false);
        Stretch(charInner.rectTransform, 2, 2);
        MakeTMP("CharLabel", charInner.rectTransform, "CHARACTER\nSILHOUETTE", 9, C_TextDim)
            .alignment = TextAlignmentOptions.Center;

        // Slots
        var slotsRoot = MakeRect("SlotsContainer", equipView);
        Stretch(slotsRoot, 0, 0);

        MakeEquipSlot("Slot_Head",      slotsRoot, new Vector2(   0,  168), "HEAD"    );
        MakeEquipSlot("Slot_Weapon",    slotsRoot, new Vector2(-185,   20), "WEAPON"  );
        MakeEquipSlot("Slot_Offhand",   slotsRoot, new Vector2( 185,   20), "OFFHAND" );
        MakeEquipSlot("Slot_Body",      slotsRoot, new Vector2(   0,  -90), "BODY"    );
        MakeEquipSlot("Slot_Boots",     slotsRoot, new Vector2(   0, -190), "BOOTS"   );
        MakeEquipSlot("Slot_Ring1",     slotsRoot, new Vector2( -70, -210), "RING"    );
        MakeEquipSlot("Slot_Ring2",     slotsRoot, new Vector2(  70, -210), "RING"    );

        // ─ Right: item detail ─────────────────────────────────────────────────
        BuildDetailPanel("ItemDetail", rt,
            new Vector2(0.58f, 0), new Vector2(1, 1),
            new Vector2(4, 8),    new Vector2(-8, -8));

        return go;
    }

    static GameObject BuildInventoryScreen(RectTransform parent)
    {
        var go = MakeScreenGO("InventoryScreen", parent);
        go.AddComponent<InventoryScreen>();
        var rt = go.GetComponent<RectTransform>();

        // ─ Top currency bar ───────────────────────────────────────────────────
        var coinBar = MakeRect("CurrencyBar", rt);
        coinBar.anchorMin       = new Vector2(0,    1);
        coinBar.anchorMax       = new Vector2(0.63f,1);
        coinBar.pivot           = new Vector2(0,    1);
        coinBar.sizeDelta       = new Vector2(0, 34);
        coinBar.anchoredPosition = new Vector2(8, -8);
        coinBar.gameObject.AddComponent<Image>().color = new Color(0.10f, 0.08f, 0.05f, 1f);

        var coinLayout = coinBar.gameObject.AddComponent<HorizontalLayoutGroup>();
        coinLayout.childAlignment      = TextAnchor.MiddleLeft;
        coinLayout.childForceExpandHeight = true;
        coinLayout.childForceExpandWidth  = false;
        coinLayout.spacing = 6;
        coinLayout.padding = new RectOffset(8, 8, 4, 4);

        var coinIconImg = MakeImg("CoinIcon", coinBar, C_Gold, false);
        coinIconImg.rectTransform.sizeDelta = new Vector2(22, 22);
        coinIconImg.GetComponent<LayoutElement>()?.Equals(null);
        coinIconImg.gameObject.AddComponent<LayoutElement>().preferredWidth = 22;

        var coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/@tempAssets/HollowKnight/Inventory & UI/InventoryCoin.png");
        if (coinSprite) coinIconImg.sprite = coinSprite;

        var coinTxt = MakeTMP("CoinAmount", coinBar, "1,250", 14, C_Gold);
        coinTxt.alignment = TextAlignmentOptions.MidlineLeft;
        coinTxt.fontStyle = FontStyles.Bold;
        coinTxt.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

        // ─ Item grid (left 63%) ───────────────────────────────────────────────
        var gridArea = MakeRect("GridArea", rt);
        gridArea.anchorMin = new Vector2(0,    0);
        gridArea.anchorMax = new Vector2(0.63f,1);
        gridArea.offsetMin = new Vector2(8,  8);
        gridArea.offsetMax = new Vector2(-4, -50);
        gridArea.gameObject.AddComponent<Image>().color = C_SlotBG;

        var scroll    = gridArea.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical   = true;
        scroll.scrollSensitivity = 24;

        var viewport = MakeRect("Viewport", gridArea);
        Stretch(viewport, 0, 0);
        viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        viewport.gameObject.AddComponent<Image>().color = Color.clear;
        scroll.viewport = viewport;

        var gridContent = MakeRect("Content", viewport);
        gridContent.anchorMin = new Vector2(0, 1);
        gridContent.anchorMax = new Vector2(1, 1);
        gridContent.pivot     = new Vector2(0.5f, 1f);
        scroll.content = gridContent;

        var grid = gridContent.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize        = new Vector2(68, 68);
        grid.spacing         = new Vector2(6, 6);
        grid.padding         = new RectOffset(10, 10, 10, 10);
        grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;

        var csf = gridContent.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var emptySlot = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/@tempAssets/HollowKnight/Inventory & UI/Inv_Empty.png");

        for (int i = 0; i < 30; i++)
        {
            var slot    = MakeRect($"Slot_{i:D2}", gridContent);
            var slotImg = slot.gameObject.AddComponent<Image>();
            if (emptySlot) { slotImg.sprite = emptySlot; slotImg.color = Color.white; }
            else            slotImg.color = C_SlotBG;
            slot.gameObject.AddComponent<Button>();
        }

        // ─ Right: item detail ─────────────────────────────────────────────────
        BuildDetailPanel("ItemDetail", rt,
            new Vector2(0.63f, 0), new Vector2(1, 1),
            new Vector2(4, 8),    new Vector2(-8, -8));

        return go;
    }

    static GameObject BuildMapScreen(RectTransform parent)
    {
        var go = MakeScreenGO("MapScreen", parent);
        go.AddComponent<MapScreen>();
        var rt = go.GetComponent<RectTransform>();

        // ─ Map display area ───────────────────────────────────────────────────
        var mapArea = MakeRect("MapArea", rt);
        mapArea.anchorMin = new Vector2(0,    0);
        mapArea.anchorMax = new Vector2(0.78f,1);
        mapArea.offsetMin = new Vector2(8,  8);
        mapArea.offsetMax = new Vector2(-4, -8);
        mapArea.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.04f, 0.03f, 1f);

        // Map raw image (placeholder for actual map texture)
        var mapRaw = MakeRect("MapImage", mapArea);
        Stretch(mapRaw, 12, 12);
        var rawImg = mapRaw.gameObject.AddComponent<RawImage>();
        rawImg.color = new Color(0.12f, 0.10f, 0.07f, 1f);

        // Grid lines overlay for map feel
        MakeTMP("MapPlaceholder", mapRaw, "MAP\n(Assign RawImage.texture)", 14,
            new Color(0.30f, 0.25f, 0.15f, 0.40f)).alignment = TextAlignmentOptions.Center;

        // Player dot
        var playerDot = MakeRect("PlayerMarker", mapRaw);
        playerDot.anchorMin       = playerDot.anchorMax = new Vector2(0.5f, 0.5f);
        playerDot.pivot           = new Vector2(0.5f, 0.5f);
        playerDot.sizeDelta       = new Vector2(12, 12);
        playerDot.anchoredPosition = Vector2.zero;
        var dotImg = playerDot.gameObject.AddComponent<Image>();
        dotImg.color = C_Gold;

        // Current area bar at bottom
        var areaBar = MakeRect("AreaBar", mapArea);
        areaBar.anchorMin = new Vector2(0, 0);
        areaBar.anchorMax = new Vector2(1, 0);
        areaBar.pivot     = new Vector2(0.5f, 0f);
        areaBar.sizeDelta = new Vector2(0, 36);
        areaBar.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.06f, 0.04f, 0.92f);

        var areaTxt = MakeTMP("AreaName", areaBar, "Current Area : Unknown", 12, C_Text);
        Stretch(areaTxt.rectTransform, 0, 0);
        areaTxt.alignment = TextAlignmentOptions.Center;

        // ─ Right: legend panel ────────────────────────────────────────────────
        var legend = MakeRect("LegendPanel", rt);
        legend.anchorMin = new Vector2(0.78f, 0);
        legend.anchorMax = new Vector2(1f,    1);
        legend.offsetMin = new Vector2(4,  8);
        legend.offsetMax = new Vector2(-8, -8);
        legend.gameObject.AddComponent<Image>().color = C_DetailBG;

        var legendTitle = MakeTMP("LegendTitle", legend, "LEGEND", 13, C_Gold);
        legendTitle.rectTransform.anchorMin       = new Vector2(0,    1);
        legendTitle.rectTransform.anchorMax       = new Vector2(1,    1);
        legendTitle.rectTransform.pivot           = new Vector2(0.5f, 1f);
        legendTitle.rectTransform.sizeDelta       = new Vector2(-16, 28);
        legendTitle.rectTransform.anchoredPosition = new Vector2(0, -10);
        legendTitle.alignment = TextAlignmentOptions.Center;
        legendTitle.fontStyle = FontStyles.Bold;

        // Divider
        var div = MakeImg("LegendDivider", legend, C_Divider, false);
        div.rectTransform.anchorMin       = new Vector2(0.05f, 1);
        div.rectTransform.anchorMax       = new Vector2(0.95f, 1);
        div.rectTransform.pivot           = new Vector2(0.5f,  1);
        div.rectTransform.sizeDelta       = new Vector2(0, 1);
        div.rectTransform.anchoredPosition = new Vector2(0, -40);

        (string text, Color color)[] entries =
        {
            ("▲  Player",      C_Gold),
            ("■  Boss",        C_Red),
            ("◆  Shop",        new Color(0.4f, 0.8f, 0.4f, 1f)),
            ("○  Save Point",  new Color(0.3f, 0.6f, 1.0f, 1f)),
            ("★  Secret",      new Color(0.9f, 0.8f, 0.2f, 1f)),
            ("✦  Warp",        new Color(0.7f, 0.4f, 1.0f, 1f)),
        };

        for (int i = 0; i < entries.Length; i++)
        {
            var entry = MakeTMP($"Entry_{i}", legend, entries[i].text, 11, entries[i].color);
            entry.rectTransform.anchorMin       = new Vector2(0,    1);
            entry.rectTransform.anchorMax       = new Vector2(1,    1);
            entry.rectTransform.pivot           = new Vector2(0.5f, 1);
            entry.rectTransform.sizeDelta       = new Vector2(-16, 24);
            entry.rectTransform.anchoredPosition = new Vector2(0, -(50 + i * 28f));
            entry.alignment = TextAlignmentOptions.MidlineLeft;
        }

        return go;
    }

    static GameObject BuildSettingsScreen(RectTransform parent)
    {
        var go = MakeScreenGO("SettingsScreen", parent);
        go.AddComponent<SettingsScreen>();
        var rt = go.GetComponent<RectTransform>();

        // Centered scroll container
        var content = MakeRect("SettingsContent", rt);
        content.anchorMin = new Vector2(0.08f, 0.02f);
        content.anchorMax = new Vector2(0.92f, 0.98f);
        content.offsetMin = content.offsetMax = Vector2.zero;

        var vLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vLayout.childAlignment       = TextAnchor.UpperCenter;
        vLayout.spacing              = 10;
        vLayout.padding              = new RectOffset(24, 24, 20, 20);
        vLayout.childForceExpandWidth  = true;
        vLayout.childForceExpandHeight = false;

        // Main title
        AddTitle(content, "SETTINGS");

        // ── Audio ──────────────────────────────────────────────────────────
        AddSectionHeader(content, "▸ AUDIO");
        AddSliderRow(content, "Music Volume",   0.70f);
        AddSliderRow(content, "SFX Volume",     1.00f);
        AddSliderRow(content, "Ambient Volume", 0.50f);
        AddRowDivider(content);

        // ── Display ────────────────────────────────────────────────────────
        AddSectionHeader(content, "▸ DISPLAY");
        AddToggleRow(content, "Fullscreen", true);
        AddToggleRow(content, "VSync",      false);
        AddRowDivider(content);

        // ── Controls ───────────────────────────────────────────────────────
        AddSectionHeader(content, "▸ CONTROLS");
        AddActionButton(content, "Remap Controls");
        AddActionButton(content, "Reset to Default");

        return go;
    }

    // ── Common: item detail right panel ───────────────────────────────────────

    static void BuildDetailPanel(string name, RectTransform parent,
        Vector2 ancMin, Vector2 ancMax, Vector2 offMin, Vector2 offMax)
    {
        var panel = MakeRect(name, parent);
        panel.anchorMin = ancMin;
        panel.anchorMax = ancMax;
        panel.offsetMin = offMin;
        panel.offsetMax = offMax;
        panel.gameObject.AddComponent<Image>().color = C_DetailBG;

        // Section title
        var panelTitle = MakeTMP("PanelTitle", panel, "ITEM DETAIL", 10, C_TextDim);
        panelTitle.rectTransform.anchorMin       = new Vector2(0,    1);
        panelTitle.rectTransform.anchorMax       = new Vector2(1,    1);
        panelTitle.rectTransform.pivot           = new Vector2(0.5f, 1);
        panelTitle.rectTransform.sizeDelta       = new Vector2(-8,   20);
        panelTitle.rectTransform.anchoredPosition = new Vector2(0,   -6);
        panelTitle.alignment = TextAlignmentOptions.Center;

        // Icon background box (top 36% of panel)
        var iconBG = MakeImg("ItemIconBG", panel, C_SlotBG, false);
        iconBG.rectTransform.anchorMin = new Vector2(0.12f, 0.60f);
        iconBG.rectTransform.anchorMax = new Vector2(0.88f, 0.94f);
        iconBG.rectTransform.offsetMin = iconBG.rectTransform.offsetMax = Vector2.zero;

        // Inner border
        var iconBorder = MakeImg("IconBorder", iconBG.rectTransform, C_SlotBorder, false);
        Stretch(iconBorder.rectTransform, 0, 0);
        iconBorder.gameObject.AddComponent<Outline>().effectColor = C_SlotBorder;

        var iconPlaceholder = MakeTMP("IconPlaceholder", iconBG.rectTransform, "?", 36, C_TextDim);
        Stretch(iconPlaceholder.rectTransform, 0, 0);
        iconPlaceholder.alignment = TextAlignmentOptions.Center;

        // Item name
        var itemName = MakeTMP("ItemName", panel, "— Empty —", 14, C_Text);
        itemName.rectTransform.anchorMin = new Vector2(0,     0.55f);
        itemName.rectTransform.anchorMax = new Vector2(1,     0.60f);
        itemName.rectTransform.offsetMin = new Vector2(8,     0);
        itemName.rectTransform.offsetMax = new Vector2(-8,    0);
        itemName.alignment = TextAlignmentOptions.Center;
        itemName.fontStyle = FontStyles.Bold;

        // Divider
        var div = MakeImg("Divider", panel, C_Divider, false);
        div.rectTransform.anchorMin = new Vector2(0.04f, 0.535f);
        div.rectTransform.anchorMax = new Vector2(0.96f, 0.535f);
        div.rectTransform.sizeDelta = new Vector2(0, 1);

        // Stats block
        var stats = MakeTMP("ItemStats", panel,
            "ATK  +0\nDEF  +0\nSPD  +0\nMAG  +0", 11, C_TextDim);
        stats.rectTransform.anchorMin = new Vector2(0,    0.30f);
        stats.rectTransform.anchorMax = new Vector2(1,    0.53f);
        stats.rectTransform.offsetMin = new Vector2(14,   0);
        stats.rectTransform.offsetMax = new Vector2(-14,  0);
        stats.alignment = TextAlignmentOptions.TopLeft;
        stats.lineSpacing = 6;

        // Description
        var desc = MakeTMP("ItemDescription", panel,
            "Select an item to\nview its details.", 10, new Color(0.50f, 0.46f, 0.35f, 1f));
        desc.rectTransform.anchorMin = new Vector2(0,    0.04f);
        desc.rectTransform.anchorMax = new Vector2(1,    0.29f);
        desc.rectTransform.offsetMin = new Vector2(10,   0);
        desc.rectTransform.offsetMax = new Vector2(-10,  0);
        desc.alignment = TextAlignmentOptions.TopLeft;
        desc.enableWordWrapping = true;
    }

    // ── Equipment slot ────────────────────────────────────────────────────────

    static void MakeEquipSlot(string name, RectTransform parent, Vector2 offset, string label)
    {
        // Outer = border color
        var outer = MakeRect(name, parent);
        outer.anchorMin        = outer.anchorMax = new Vector2(0.5f, 0.5f);
        outer.pivot            = new Vector2(0.5f, 0.5f);
        outer.sizeDelta        = new Vector2(66, 66);
        outer.anchoredPosition = offset;
        outer.gameObject.AddComponent<Image>().color = C_SlotBorder;
        outer.gameObject.AddComponent<Button>();

        // Inner = dark fill
        var inner = MakeImg("Inner", outer, C_SlotBG, false);
        Stretch(inner.rectTransform, 2, 2);

        // Label at bottom
        var txt = MakeTMP("Label", inner.rectTransform, label, 7, C_TextDim);
        txt.rectTransform.anchorMin       = new Vector2(0,    0);
        txt.rectTransform.anchorMax       = new Vector2(1,    0);
        txt.rectTransform.pivot           = new Vector2(0.5f, 0);
        txt.rectTransform.sizeDelta       = new Vector2(0,   13);
        txt.rectTransform.anchoredPosition = new Vector2(0,    2);
        txt.alignment = TextAlignmentOptions.Bottom;
    }

    // ── Settings helpers ──────────────────────────────────────────────────────

    static void AddTitle(RectTransform parent, string text)
    {
        var t  = MakeTMP("Title_" + text, parent, text, 20, C_Gold);
        t.alignment  = TextAlignmentOptions.Center;
        t.fontStyle  = FontStyles.Bold;
        t.gameObject.AddComponent<LayoutElement>().preferredHeight = 38;
    }

    static void AddSectionHeader(RectTransform parent, string text)
    {
        var t = MakeTMP("Header_" + text, parent, text, 13, C_TextDim);
        t.alignment = TextAlignmentOptions.MidlineLeft;
        t.fontStyle = FontStyles.Bold;
        t.gameObject.AddComponent<LayoutElement>().preferredHeight = 28;
    }

    static void AddSliderRow(RectTransform parent, string label, float value)
    {
        var row = MakeRect("Row_" + label.Replace(" ", ""), parent);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 36;

        var hg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hg.childAlignment       = TextAnchor.MiddleLeft;
        hg.childForceExpandHeight = true;
        hg.childForceExpandWidth  = false;
        hg.spacing = 12;

        // Label
        var lbl = MakeTMP(label + "_Label", row, label, 12, C_Text);
        lbl.alignment = TextAlignmentOptions.MidlineLeft;
        lbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 150;

        // Slider container
        var sliderGO = new GameObject(label + "_Slider");
        sliderGO.transform.SetParent(row, false);
        var sliderRT = sliderGO.AddComponent<RectTransform>();
        var sliderLE = sliderGO.AddComponent<LayoutElement>();
        sliderLE.preferredWidth = 180;
        sliderLE.flexibleWidth  = 1;

        var slider         = sliderGO.AddComponent<Slider>();
        slider.minValue    = 0;
        slider.maxValue    = 1;
        slider.value       = value;
        slider.wholeNumbers = false;

        // BG track
        var bg = MakeRect("Background", sliderRT);
        bg.anchorMin = new Vector2(0,    0.35f);
        bg.anchorMax = new Vector2(1,    0.65f);
        bg.offsetMin = bg.offsetMax = Vector2.zero;
        bg.gameObject.AddComponent<Image>().color = new Color(0.15f, 0.12f, 0.07f, 1f);

        // Fill area + fill
        var fillArea = MakeRect("Fill Area", sliderRT);
        fillArea.anchorMin = new Vector2(0,    0.35f);
        fillArea.anchorMax = new Vector2(1,    0.65f);
        fillArea.offsetMin = new Vector2(4,    0);
        fillArea.offsetMax = new Vector2(-4,   0);

        var fill = MakeRect("Fill", fillArea);
        Stretch(fill, 0, 0);
        fill.gameObject.AddComponent<Image>().color = C_SliderFill;
        slider.fillRect = fill;

        // Handle area + handle
        var handleArea = MakeRect("Handle Slide Area", sliderRT);
        Stretch(handleArea, 0, 0);

        var handle = MakeRect("Handle", handleArea);
        handle.anchorMin       = handle.anchorMax = new Vector2(0, 0.5f);
        handle.pivot           = new Vector2(0.5f, 0.5f);
        handle.sizeDelta       = new Vector2(14, 26);
        var handleImg          = handle.gameObject.AddComponent<Image>();
        handleImg.color        = C_Gold;
        slider.handleRect      = handle;
        slider.targetGraphic   = handleImg;

        // Value % text
        var valTxt = MakeTMP(label + "_Value", row, $"{(int)(value * 100)}%", 11, C_Text);
        valTxt.alignment = TextAlignmentOptions.Center;
        valTxt.gameObject.AddComponent<LayoutElement>().preferredWidth = 42;
    }

    static void AddToggleRow(RectTransform parent, string label, bool on)
    {
        var row = MakeRect("Row_" + label.Replace(" ", ""), parent);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 34;

        var hg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hg.childAlignment       = TextAnchor.MiddleLeft;
        hg.childForceExpandHeight = true;
        hg.childForceExpandWidth  = false;
        hg.spacing = 12;

        var lbl = MakeTMP(label + "_Label", row, label, 12, C_Text);
        lbl.alignment = TextAlignmentOptions.MidlineLeft;
        lbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 150;

        // Toggle pill
        var tGO = new GameObject(label + "_Toggle");
        tGO.transform.SetParent(row, false);
        var tRT = tGO.AddComponent<RectTransform>();
        var tLE = tGO.AddComponent<LayoutElement>();
        tLE.preferredWidth  = 56;
        tLE.preferredHeight = 26;

        var tImg   = tGO.AddComponent<Image>();
        tImg.color = on ? C_SliderFill : new Color(0.20f, 0.16f, 0.10f, 1f);

        var toggle        = tGO.AddComponent<Toggle>();
        toggle.isOn       = on;
        toggle.targetGraphic = tImg;
        toggle.graphic    = tImg;

        // ON/OFF label inside the pill
        var checkTxt = MakeTMP("CheckLabel", tRT, on ? "ON" : "OFF", 11, Color.white);
        Stretch(checkTxt.rectTransform, 0, 0);
        checkTxt.alignment = TextAlignmentOptions.Center;
        checkTxt.fontStyle = FontStyles.Bold;
    }

    static void AddActionButton(RectTransform parent, string label)
    {
        var row = MakeRect("Row_" + label.Replace(" ", ""), parent);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 38;

        var hg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hg.childAlignment       = TextAnchor.MiddleLeft;
        hg.childForceExpandHeight = false;
        hg.childForceExpandWidth  = false;
        hg.spacing = 0;

        var btnGO = new GameObject(label + "_Button");
        btnGO.transform.SetParent(row, false);
        var btnLE = btnGO.AddComponent<LayoutElement>();
        btnLE.preferredWidth  = 220;
        btnLE.preferredHeight = 34;

        var btnImg   = btnGO.AddComponent<Image>();
        btnImg.color = C_TabInact;
        btnGO.AddComponent<Button>();

        var btnTxt = MakeTMP("Label", btnGO.GetComponent<RectTransform>(),
            label.ToUpper(), 12, C_TabText);
        Stretch(btnTxt.rectTransform, 0, 0);
        btnTxt.alignment = TextAlignmentOptions.Center;
        btnTxt.fontStyle = FontStyles.Bold;
    }

    static void AddRowDivider(RectTransform parent)
    {
        var div = MakeImg("Divider", parent, C_Divider, false);
        div.rectTransform.sizeDelta = new Vector2(0, 1);
        div.gameObject.AddComponent<LayoutElement>().preferredHeight = 1;
    }

    // ── Tab button ────────────────────────────────────────────────────────────

    static Button MakeTabButton(string label, RectTransform parent)
    {
        var go  = new GameObject("Tab_" + label);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();

        var img  = go.AddComponent<Image>();
        img.color = C_TabInact;

        var btn    = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor      = C_TabInact;
        colors.highlightedColor = new Color(0.38f, 0.30f, 0.17f, 1f);
        colors.pressedColor     = C_TabActive;
        colors.selectedColor    = C_TabActive;
        btn.colors = colors;

        var txt = MakeTMP("Label", go.GetComponent<RectTransform>(),
            label.ToUpper(), 12, C_TabText);
        Stretch(txt.rectTransform, 0, 0);
        txt.alignment = TextAlignmentOptions.Center;
        txt.fontStyle = FontStyles.Bold;

        return btn;
    }

    // ── Primitive helpers ─────────────────────────────────────────────────────

    static GameObject MakeScreenGO(string name, RectTransform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Stretch(go.AddComponent<RectTransform>(), 0, 0);
        return go;
    }

    static RectTransform MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    static Image MakeImg(string name, Transform parent, Color color, bool stretch)
    {
        var rt  = MakeRect(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        if (stretch) Stretch(rt, 0, 0);
        return img;
    }

    static TextMeshProUGUI MakeTMP(string name, Transform parent, string text, float size, Color color)
    {
        var rt  = MakeRect(name, parent);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text     = text;
        tmp.fontSize = size;
        tmp.color    = color;
        return tmp;
    }

    /// <summary>Fill parent entirely with optional inset.</summary>
    static void Stretch(RectTransform rt, float insetH, float insetV)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2( insetH,  insetV);
        rt.offsetMax = new Vector2(-insetH, -insetV);
    }
}
