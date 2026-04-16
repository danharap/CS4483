using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Local JSON account gate for the main menu: sign-in, save slots, and run hydration.
/// </summary>
public class MainMenuLocalAccountPanel : MonoBehaviour
{
    public static MainMenuLocalAccountPanel Instance { get; private set; }

    const int MaxSaves = LocalAccountDatabase.MaxSavesPerUser;

    [SerializeField] private MainMenuManager mainMenu;

    TMP_Text _status;
    TMP_Text _accountProgressText;
    TMP_InputField _username;
    TMP_InputField _password;
    TMP_InputField _slotName;
    Button _signIn;
    Button _signUp;
    Button _logout;
    Button _createSave;
    Button _refresh;
    Button _continueToMenu;
    Transform _scrollContent;
    GameObject _deleteConfirm;
    string _pendingDeleteId;
    GameObject _gateOverlay;
    RectTransform _gatePanelRt;
    GameObject _authRoot;
    GameObject _signedRoot;
    readonly List<GameObject> _listRows = new List<GameObject>();

    void Awake()
    {
        Instance = this;
        if (mainMenu == null) mainMenu = GetComponent<MainMenuManager>();
        BuildUi();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        mainMenu?.SetMainMenuLocked(true);
        // Always require sign-in on app launch so local sessions do not carry across teammates/machines.
        LocalSaveRuntime.SignOut();
        PresentAuthGate();
    }

    /// <summary>Re-opens the account / save overlay from the main menu footer.</summary>
    public void OpenAccountPanel()
    {
        if (_gateOverlay == null) return;
        _gateOverlay.SetActive(true);
        if (LocalSaveRuntime.IsSignedIn)
        {
            SetAuthVisible(false);
            SetSignedVisible(true);
            if (_continueToMenu != null) _continueToMenu.gameObject.SetActive(true);
            RefreshList();
            SetStatus($"Signed in as {LocalAccountDatabase.GetUserEmail(LocalSaveRuntime.ActiveUserId)}", false);
            mainMenu?.SetMainMenuLocked(true);
        }
        else
        {
            PresentAuthGate();
        }
    }

    void PresentAuthGate()
    {
        ShowGate(true);
        SetAuthVisible(true);
        SetSignedVisible(false);
        if (_signIn != null) _signIn.gameObject.SetActive(true);
        if (_signUp != null) _signUp.gameObject.SetActive(true);
        if (_logout != null) _logout.gameObject.SetActive(false);
        if (_continueToMenu != null) _continueToMenu.gameObject.SetActive(false);
        SetAccountProgressTextVisible(false);
        SetStatus("Sign in with a local profile to manage saves and play.", false);
    }

    void BuildUi()
    {
        Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        _gateOverlay = CreateUi("LocalAccountGateOverlay", canvas.transform);
        var ovRt = _gateOverlay.GetComponent<RectTransform>();
        ovRt.anchorMin = Vector2.zero;
        ovRt.anchorMax = Vector2.one;
        ovRt.offsetMin = Vector2.zero;
        ovRt.offsetMax = Vector2.zero;
        _gateOverlay.AddComponent<Image>().color = PitMenuUiTheme.OverlayDim;
        _gateOverlay.transform.SetAsLastSibling();

        GameObject root = CreateUi("LocalAccountPanel", _gateOverlay.transform);
        _gatePanelRt = root.GetComponent<RectTransform>();
        _gatePanelRt.anchorMin = new Vector2(0.5f, 0.5f);
        _gatePanelRt.anchorMax = new Vector2(0.5f, 0.5f);
        _gatePanelRt.pivot = new Vector2(0.5f, 0.5f);
        _gatePanelRt.sizeDelta = new Vector2(980f, 620f);
        _gatePanelRt.anchoredPosition = Vector2.zero;
        root.AddComponent<Image>().color = PitMenuUiTheme.PanelBase;
        var outline = root.AddComponent<Outline>();
        outline.effectColor = PitMenuUiTheme.PanelRim;
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        var rootV = root.AddComponent<VerticalLayoutGroup>();
        rootV.padding = new RectOffset(22, 22, 20, 18);
        rootV.spacing = 14f;
        rootV.childAlignment = TextAnchor.UpperCenter;
        rootV.childControlHeight = true;
        rootV.childForceExpandHeight = false;
        rootV.childControlWidth = true;
        rootV.childForceExpandWidth = true;

        var header = AddTmp(root.transform, "AccountHeader", 22f, PitMenuUiTheme.GoldAccent, TextAlignmentOptions.Center);
        header.fontStyle = FontStyles.Bold;
        header.text = $"{PitMenuBranding.GameTitle} — local profile";
        header.enableWordWrapping = true;
        header.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;

        var sub = AddTmp(root.transform, "AccountSub", 13f, PitMenuUiTheme.TextMuted, TextAlignmentOptions.Center);
        sub.text = "Profiles and saves stay on this device (JSON). No cloud.";
        sub.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;

        _status = AddTmp(root.transform, "LocalStatus", 13f, PitMenuUiTheme.TextMuted, TextAlignmentOptions.TopLeft);
        _status.enableWordWrapping = true;
        var stLe = _status.gameObject.AddComponent<LayoutElement>();
        stLe.minHeight = 40f;
        stLe.preferredHeight = 44f;

        _accountProgressText = AddTmp(root.transform, "AccountProgress", 13f, PitMenuUiTheme.GoldSoft, TextAlignmentOptions.TopLeft);
        _accountProgressText.enableWordWrapping = false;
        var apLe = _accountProgressText.gameObject.AddComponent<LayoutElement>();
        apLe.minHeight = 24f;
        apLe.preferredHeight = 24f;
        _accountProgressText.gameObject.SetActive(false);

        _authRoot = new GameObject("AuthSection", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        _authRoot.transform.SetParent(root.transform, false);
        var authImg = _authRoot.GetComponent<Image>();
        authImg.color = PitMenuUiTheme.PanelDeep;
        authImg.raycastTarget = false;
        var authV = _authRoot.GetComponent<VerticalLayoutGroup>();
        authV.padding = new RectOffset(16, 16, 14, 14);
        authV.spacing = 10f;
        authV.childAlignment = TextAnchor.UpperLeft;
        authV.childControlHeight = true;
        authV.childForceExpandHeight = false;
        authV.childControlWidth = true;
        authV.childForceExpandWidth = true;
        var authLe = _authRoot.AddComponent<LayoutElement>();
        authLe.flexibleWidth = 1f;
        authLe.minHeight = 210f;
        authLe.preferredHeight = 210f;

        var signLab = AddTmp(_authRoot.transform, "SignInLabel", 14f, PitMenuUiTheme.GoldSoft, TextAlignmentOptions.Left);
        PitMenuUiTheme.StyleSectionHeader(signLab);
        signLab.text = "SIGN IN";

        _username = AddInputRow(_authRoot.transform, "Username", "Profile name");
        if (!string.IsNullOrEmpty(LocalSaveRuntime.LoadRememberedEmail()))
            _username.text = LocalSaveRuntime.LoadRememberedEmail();
        _password = AddInputRow(_authRoot.transform, "Password", "Password", true);

        var authBtnRow = new GameObject("AuthButtons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        authBtnRow.transform.SetParent(_authRoot.transform, false);
        var hAuth = authBtnRow.GetComponent<HorizontalLayoutGroup>();
        hAuth.spacing = 12f;
        hAuth.childAlignment = TextAnchor.MiddleLeft;
        hAuth.childForceExpandWidth = false;
        hAuth.childForceExpandHeight = false;
        hAuth.childControlWidth = true;
        hAuth.childControlHeight = true;
        var authBtnLe = authBtnRow.AddComponent<LayoutElement>();
        authBtnLe.minHeight = 40f;
        authBtnLe.preferredHeight = 40f;

        _signIn = AddLayoutButton(authBtnRow.transform, "SignIn", "Sign in", 140f);
        _signUp = AddLayoutButton(authBtnRow.transform, "SignUp", "Create profile", 160f);
        PitMenuUiTheme.ApplyPrimaryRunButton(_signIn, _signIn.GetComponent<Image>());
        PitMenuUiTheme.ApplyGoldGhostButton(_signUp, _signUp.GetComponent<Image>());
        PitMenuUiTheme.WireMenuButton(_signIn, OnClickSignIn);
        PitMenuUiTheme.WireMenuButton(_signUp, OnClickSignUp);

        _signedRoot = new GameObject("SignedSection", typeof(RectTransform), typeof(VerticalLayoutGroup));
        _signedRoot.transform.SetParent(root.transform, false);
        var sigV = _signedRoot.GetComponent<VerticalLayoutGroup>();
        sigV.padding = new RectOffset(0, 0, 0, 0);
        sigV.spacing = 12f;
        sigV.childAlignment = TextAnchor.UpperLeft;
        sigV.childControlHeight = true;
        sigV.childForceExpandHeight = false;
        sigV.childControlWidth = true;
        sigV.childForceExpandWidth = true;
        var sigLe = _signedRoot.AddComponent<LayoutElement>();
        sigLe.flexibleWidth = 1f;
        sigLe.minHeight = 200f;

        var savesHdr = AddTmp(_signedRoot.transform, "SavesHeader", 14f, PitMenuUiTheme.GoldSoft, TextAlignmentOptions.Left);
        PitMenuUiTheme.StyleSectionHeader(savesHdr);
        savesHdr.text = "SAVE SLOTS (3 max)";

        BuildSaveScroll(_signedRoot.transform);

        var capNote = AddTmp(_signedRoot.transform, "CapNote", 12f, PitMenuUiTheme.TextMuted, TextAlignmentOptions.TopLeft);
        capNote.enableWordWrapping = true;
        capNote.text = "At three saves you must delete one before starting another run in a new slot.";
        var capLe = capNote.gameObject.AddComponent<LayoutElement>();
        capLe.preferredHeight = 36f;

        _slotName = AddInputRow(_signedRoot.transform, "SlotNameInput", "Name for new save slot");
        var createRow = new GameObject("CreateRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        createRow.transform.SetParent(_signedRoot.transform, false);
        var hCr = createRow.GetComponent<HorizontalLayoutGroup>();
        hCr.spacing = 10f;
        hCr.childAlignment = TextAnchor.MiddleLeft;
        hCr.childForceExpandHeight = false;
        hCr.childControlHeight = true;
        var createLe = createRow.AddComponent<LayoutElement>();
        createLe.minHeight = 40f;
        createLe.preferredHeight = 40f;
        _createSave = AddLayoutButton(createRow.transform, "CreateSave", "New save slot", 160f);
        _refresh = AddLayoutButton(createRow.transform, "Refresh", "Refresh list", 120f);
        PitMenuUiTheme.ApplyPrimaryRunButton(_createSave, _createSave.GetComponent<Image>());
        PitMenuUiTheme.ApplyNeutralPanelButton(_refresh, _refresh.GetComponent<Image>());
        PitMenuUiTheme.WireMenuButton(_createSave, OnClickCreateSave);
        PitMenuUiTheme.WireMenuButton(_refresh, OnClickRefresh);

        var bottomRow = new GameObject("BottomRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        bottomRow.transform.SetParent(_signedRoot.transform, false);
        var hBot = bottomRow.GetComponent<HorizontalLayoutGroup>();
        hBot.spacing = 10f;
        hBot.childAlignment = TextAnchor.MiddleCenter;
        hBot.childForceExpandWidth = false;
        hBot.childForceExpandHeight = false;
        hBot.childControlWidth = true;
        hBot.childControlHeight = true;
        var bottomLe = bottomRow.AddComponent<LayoutElement>();
        bottomLe.minHeight = 42f;
        bottomLe.preferredHeight = 42f;

        _logout = AddLayoutButton(bottomRow.transform, "Logout", "Log out", 120f);
        _continueToMenu = AddLayoutButton(bottomRow.transform, "Continue", "Back to menu", 160f);
        PitMenuUiTheme.ApplyNeutralPanelButton(_logout, _logout.GetComponent<Image>());
        PitMenuUiTheme.ApplySecondarySteelButton(_continueToMenu, _continueToMenu.GetComponent<Image>());
        PitMenuUiTheme.WireMenuButton(_logout, OnClickLogout);
        PitMenuUiTheme.WireMenuButton(_continueToMenu, OnContinueToMenu);

        _signedRoot.SetActive(false);
        BuildDeleteConfirm(_gateOverlay.transform);
    }

    void BuildSaveScroll(Transform parent)
    {
        var scrollGo = new GameObject("SaveScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGo.transform.SetParent(parent, false);
        var scrollRt = scrollGo.GetComponent<RectTransform>();
        var scrollBg = scrollGo.GetComponent<Image>();
        scrollBg.color = new Color(0.04f, 0.045f, 0.055f, 0.9f);
        scrollBg.raycastTarget = true;
        var scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;
        var scrollLe = scrollGo.AddComponent<LayoutElement>();
        scrollLe.minHeight = 200f;
        scrollLe.preferredHeight = 240f;
        scrollLe.flexibleHeight = 1f;

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewport.transform.SetParent(scrollGo.transform, false);
        var vpRt = viewport.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = new Vector2(4f, 4f);
        vpRt.offsetMax = new Vector2(-4f, -4f);
        viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0.15f);
        viewport.GetComponent<Image>().raycastTarget = true;

        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        var cRt = content.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0f, 1f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot = new Vector2(0.5f, 1f);
        cRt.anchoredPosition = Vector2.zero;
        cRt.sizeDelta = new Vector2(0f, 0f);
        var v = content.GetComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(6, 6, 6, 6);
        v.spacing = 8f;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlHeight = true;
        v.childForceExpandHeight = false;
        v.childControlWidth = true;
        v.childForceExpandWidth = true;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = cRt;
        scroll.viewport = vpRt;

        _scrollContent = content.transform;
    }

    void BuildDeleteConfirm(Transform parent)
    {
        _deleteConfirm = CreateUi("DeleteConfirm", parent);
        var rt = _deleteConfirm.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        _deleteConfirm.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f);
        _deleteConfirm.SetActive(false);

        var box = CreateUi("Box", _deleteConfirm.transform);
        var boxRt = box.GetComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0.5f, 0.5f);
        boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.sizeDelta = new Vector2(400f, 200f);
        box.AddComponent<Image>().color = PitMenuUiTheme.PanelDeep;
        var bo = box.AddComponent<Outline>();
        bo.effectColor = PitMenuUiTheme.Danger;
        bo.effectDistance = new Vector2(1f, -1f);

        var txt = AddTmp(box.transform, "Q", 17f, PitMenuUiTheme.TextPrimary, TextAlignmentOptions.Center);
        txt.enableWordWrapping = true;
        txt.text = "Erase this save slot forever?\n<size=12><color=#AAAAAA>This cannot be undone.</color></size>";
        var tr = txt.rectTransform;
        tr.anchorMin = new Vector2(0.05f, 0.38f);
        tr.anchorMax = new Vector2(0.95f, 0.95f);
        tr.offsetMin = tr.offsetMax = Vector2.zero;

        var row = new GameObject("DelRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(box.transform, false);
        var rr = row.GetComponent<RectTransform>();
        rr.anchorMin = new Vector2(0.1f, 0.08f);
        rr.anchorMax = new Vector2(0.9f, 0.32f);
        rr.offsetMin = rr.offsetMax = Vector2.zero;
        var hr = row.GetComponent<HorizontalLayoutGroup>();
        hr.spacing = 16f;
        hr.childAlignment = TextAnchor.MiddleCenter;

        var del = AddLayoutButton(row.transform, "Yes", "Delete save", 140f);
        var no = AddLayoutButton(row.transform, "No", "Cancel", 120f);
        PitMenuUiTheme.ApplyDangerButton(del, del.GetComponent<Image>());
        PitMenuUiTheme.ApplyNeutralPanelButton(no, no.GetComponent<Image>());
        PitMenuUiTheme.WireMenuButton(del, ConfirmDeleteYes);
        PitMenuUiTheme.WireMenuButton(no, ConfirmDeleteNo);
    }

    void OnContinueToMenu()
    {
        ShowGate(false);
        mainMenu?.SetMainMenuLocked(false);
    }

    void OnClickRefresh()
    {
        if (!LocalSaveRuntime.IsSignedIn) return;
        LocalAccountDatabase.SaveAccountProfileFromRuntime(LocalSaveRuntime.ActiveUserId);
        RefreshList();
        SetStatus("Save list refreshed.", false);
    }

    void OnClickSignIn()
    {
        string uid = LocalAccountDatabase.SignIn(_username.text, _password.text, out string err);
        if (uid == null)
        {
            SetStatus(err, true);
            return;
        }
        LocalSaveRuntime.ActiveUserId = uid;
        LocalSaveRuntime.SetRememberedEmail(_username.text.Trim());
        ApplySignedInProfile();
        PresentSignedInAfterAuth();
    }

    void OnClickSignUp()
    {
        string uid = LocalAccountDatabase.Register(_username.text, _password.text, out string err);
        if (uid == null)
        {
            SetStatus(err, true);
            return;
        }
        LocalSaveRuntime.ActiveUserId = uid;
        LocalSaveRuntime.SetRememberedEmail(_username.text.Trim());
        ApplySignedInProfile();
        PresentSignedInAfterAuth();
    }

    void OnClickLogout()
    {
        if (LocalSaveRuntime.IsSignedIn)
            LocalAccountDatabase.SaveAccountProfileFromRuntime(LocalSaveRuntime.ActiveUserId);
        LocalSaveRuntime.SignOut();
        mainMenu?.SetMainMenuLocked(true);
        ClearList();
        PresentAuthGate();
        mainMenu?.RefreshFooterAfterAccount();
    }

    void OnClickCreateSave()
    {
        if (!LocalSaveRuntime.IsSignedIn)
        {
            SetStatus("Sign in first.", true);
            return;
        }

        var list = LocalAccountDatabase.ListSaves(LocalSaveRuntime.ActiveUserId);
        if (list.Count >= MaxSaves)
        {
            SetStatus("All three save slots are full. Delete a slot below, then try again.", true);
            return;
        }

        string name = string.IsNullOrWhiteSpace(_slotName.text) ? "Untitled descent" : _slotName.text.Trim();
        if (name.Length > 40) name = name.Substring(0, 40);
        var doc = GameSaveSerializer.CreateNewRun();
        bool tutorialAlreadyCompleted = PlayerPrefs.GetInt("Meta_TutorialCompleted", 0) == 1;
        if (doc.run != null)
            doc.run.tutorialCompleted = tutorialAlreadyCompleted;
        if (LocalAccountDatabase.TryLoadAccountProfile(LocalSaveRuntime.ActiveUserId, out var acc, out var hs, out var st))
        {
            doc.account = acc;
            doc.highScore = hs;
            doc.settings = st;
        }

        if (!LocalAccountDatabase.TryCreateSave(LocalSaveRuntime.ActiveUserId, name, doc, out var slot, out string err))
        {
            SetStatus(err, true);
            return;
        }

        LocalSaveRuntime.ActiveSaveId = slot.id;
        LocalSaveRuntime.PendingHydrate = doc;
        PlayerPrefs.SetString("Local_ActiveSlotLabel", name);
        PlayerPrefs.Save();
        bool needAutoTutorial = doc.run != null && !doc.run.tutorialCompleted && doc.run.activeWorld != 1;
        MainMenuManager.SetShouldRunTutorialForLoadedSave(needAutoTutorial);
        mainMenu?.LoadMainSceneFromAccountSave();
    }

    void RefreshList()
    {
        if (_scrollContent == null) return;
        var rows = LocalAccountDatabase.ListSaves(LocalSaveRuntime.ActiveUserId);
        RenderList(rows);
        bool atCap = rows.Count >= MaxSaves;
        if (_createSave != null) _createSave.interactable = !atCap;
        string em = LocalAccountDatabase.GetUserEmail(LocalSaveRuntime.ActiveUserId);
        if (atCap)
            SetStatus($"{em} — using all {MaxSaves} slots. Delete one to start a new file.", false);
        else if (rows.Count == 0)
            SetStatus($"{em} — no saves yet. Create a slot or pick Back to menu.", false);
        else
            SetStatus($"{em} — {rows.Count}/{MaxSaves} slots in use.", false);

        RefreshAccountProgressText();
    }

    void RenderList(List<LocalAccountDatabase.SaveSlotRecord> rows)
    {
        ClearList();
        if (rows == null || rows.Count == 0)
        {
            var t = AddTmp(_scrollContent, "Empty", 14f, PitMenuUiTheme.TextMuted, TextAlignmentOptions.Center);
            t.text = "No saves yet.\nCreate a named slot below, or load an existing slot after you make one.";
            _listRows.Add(t.gameObject);
            return;
        }

        foreach (var row in rows)
        {
            string progress = "Fresh run";
            if (row.payload?.run != null)
            {
                int wave = row.payload.run.waveIndex + 1;
                int world = row.payload.run.activeWorld;
                progress = $"Wave {wave} · Arena {world}";
            }

            string when = FormatLastPlayed(row.updatedAtIso);
            string title = string.IsNullOrEmpty(row.slotLabel) ? "Unnamed slot" : row.slotLabel;

            var rowGo = new GameObject("Row_" + row.id, typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(SaveSlotRowChrome));
            rowGo.transform.SetParent(_scrollContent, false);
            var bg = rowGo.GetComponent<Image>();
            bg.color = PitMenuUiTheme.SlotRow;
            bg.raycastTarget = true;
            var chrome = rowGo.GetComponent<SaveSlotRowChrome>();
            chrome.Background = bg;
            var h = rowGo.GetComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(12, 12, 10, 10);
            h.spacing = 12f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childForceExpandWidth = false;
            var le = rowGo.AddComponent<LayoutElement>();
            le.minHeight = 76f;
            le.preferredHeight = 76f;

            var textCol = new GameObject("TextCol", typeof(RectTransform), typeof(VerticalLayoutGroup));
            textCol.transform.SetParent(rowGo.transform, false);
            var tv = textCol.GetComponent<VerticalLayoutGroup>();
            tv.spacing = 4f;
            tv.childAlignment = TextAnchor.UpperLeft;
            tv.childControlWidth = true;
            tv.childForceExpandWidth = true;
            var tLe = textCol.AddComponent<LayoutElement>();
            tLe.flexibleWidth = 1f;

            var titleTmp = AddTmp(textCol.transform, "Title", 16f, PitMenuUiTheme.TextPrimary, TextAlignmentOptions.Left);
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.text = title;
            var subTmp = AddTmp(textCol.transform, "Sub", 12f, PitMenuUiTheme.TextMuted, TextAlignmentOptions.Left);
            subTmp.text = $"{progress}\nLast played: {when}";

            string id = row.id;
            var loadBtn = AddLayoutButton(rowGo.transform, "Load", "Load", 88f);
            var delBtn = AddLayoutButton(rowGo.transform, "Del", "Delete", 88f);
            PitMenuUiTheme.ApplySecondarySteelButton(loadBtn, loadBtn.GetComponent<Image>());
            PitMenuUiTheme.ApplyDangerButton(delBtn, delBtn.GetComponent<Image>());
            PitMenuUiTheme.WireMenuButton(loadBtn, () => LoadSave(id));
            PitMenuUiTheme.WireMenuButton(delBtn, () => PromptDelete(id));

            _listRows.Add(rowGo);
        }
    }

    static string FormatLastPlayed(string iso)
    {
        if (string.IsNullOrEmpty(iso)) return "—";
        if (DateTime.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
            return dt.ToLocalTime().ToString("MMM d, yyyy  h:mm tt", CultureInfo.InvariantCulture);
        return iso;
    }

    void ClearList()
    {
        foreach (var go in _listRows)
            if (go != null) Destroy(go);
        _listRows.Clear();
        if (_scrollContent == null) return;
        for (int i = _scrollContent.childCount - 1; i >= 0; i--)
            Destroy(_scrollContent.GetChild(i).gameObject);
    }

    void LoadSave(string id)
    {
        if (!LocalSaveRuntime.IsSignedIn) return;
        var row = LocalAccountDatabase.FindSave(LocalSaveRuntime.ActiveUserId, id);
        if (row?.payload == null)
        {
            SetStatus("That save is missing or unreadable.", true);
            return;
        }
        if (!GameSaveSerializer.TryParse(JsonConvert.SerializeObject(row.payload), out var doc, out string err))
        {
            SetStatus(err ?? "Invalid save data.", true);
            return;
        }

        LocalSaveRuntime.ActiveSaveId = id;
        LocalSaveRuntime.PendingHydrate = doc;
        PlayerPrefs.SetString("Local_ActiveSlotLabel", row.slotLabel ?? "Save");
        PlayerPrefs.Save();
        bool needAutoTutorial = doc.run != null && !doc.run.tutorialCompleted && doc.run.activeWorld != 1;
        MainMenuManager.SetShouldRunTutorialForLoadedSave(needAutoTutorial);
        mainMenu?.LoadMainSceneFromAccountSave();
    }

    void PromptDelete(string id)
    {
        _pendingDeleteId = id;
        _deleteConfirm.SetActive(true);
    }

    void ConfirmDeleteNo() => _deleteConfirm.SetActive(false);

    void ConfirmDeleteYes()
    {
        _deleteConfirm.SetActive(false);
        if (!string.IsNullOrEmpty(_pendingDeleteId))
            DeleteSave(_pendingDeleteId);
        _pendingDeleteId = null;
    }

    void DeleteSave(string id)
    {
        if (!LocalAccountDatabase.TryDeleteSave(LocalSaveRuntime.ActiveUserId, id, out string err))
        {
            SetStatus(err, true);
            return;
        }
        if (LocalSaveRuntime.ActiveSaveId == id) LocalSaveRuntime.ActiveSaveId = null;
        SetStatus("Save slot removed.", false);
        RefreshList();
    }

    void SetStatus(string msg, bool error)
    {
        if (_status == null) return;
        _status.text = msg;
        _status.color = error ? PitMenuUiTheme.TextError : PitMenuUiTheme.TextMuted;
    }

    void PresentSignedInAfterAuth()
    {
        SetAuthVisible(false);
        SetSignedVisible(true);
        if (_signIn != null) _signIn.gameObject.SetActive(false);
        if (_signUp != null) _signUp.gameObject.SetActive(false);
        if (_logout != null) _logout.gameObject.SetActive(true);
        if (_continueToMenu != null) _continueToMenu.gameObject.SetActive(true);
        mainMenu?.SetMainMenuLocked(true);
        ShowGate(true);
        RefreshList();
        mainMenu?.RefreshFooterAfterAccount();
        mainMenu?.RefreshAccountMetaDisplay();
    }

    void RefreshLoggedInUiStartup()
    {
        SetAuthVisible(false);
        SetSignedVisible(true);
        if (_signIn != null) _signIn.gameObject.SetActive(false);
        if (_signUp != null) _signUp.gameObject.SetActive(false);
        if (_logout != null) _logout.gameObject.SetActive(true);
        if (_continueToMenu != null) _continueToMenu.gameObject.SetActive(false);
        mainMenu?.SetMainMenuLocked(false);
        ShowGate(false);
        RefreshList();
        mainMenu?.RefreshFooterAfterAccount();
        mainMenu?.RefreshAccountMetaDisplay();
    }

    void RefreshAccountProgressText()
    {
        if (_accountProgressText == null) return;
        if (!LocalSaveRuntime.IsSignedIn)
        {
            _accountProgressText.gameObject.SetActive(false);
            return;
        }

        _accountProgressText.gameObject.SetActive(true);
        if (LocalAccountDatabase.TryLoadAccountProfile(LocalSaveRuntime.ActiveUserId, out var acc, out _, out _))
            _accountProgressText.text = $"Account Lv {Mathf.Max(1, acc.accountLevel)}   •   Unspent Skill Points: {Mathf.Max(0, acc.skillPoints)}";
        else
            _accountProgressText.text = "Account Lv 1   •   Unspent Skill Points: 0";
    }

    void SetAccountProgressTextVisible(bool visible)
    {
        if (_accountProgressText != null) _accountProgressText.gameObject.SetActive(visible);
    }

    void SetAuthVisible(bool v)
    {
        if (_authRoot != null) _authRoot.SetActive(v);
    }

    void SetSignedVisible(bool v)
    {
        if (_signedRoot != null) _signedRoot.SetActive(v);
    }

    void ShowGate(bool show)
    {
        if (_gateOverlay != null) _gateOverlay.SetActive(show);
    }

    static GameObject CreateUi(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static TMP_InputField AddInputRow(Transform parent, string name, string placeholder, bool password = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        var img = go.GetComponent<Image>();
        img.color = PitMenuUiTheme.InputBg;
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 40f;
        le.preferredHeight = 40f;
        var input = go.GetComponent<TMP_InputField>();
        var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(go.transform, false);
        var text = textGo.GetComponent<TextMeshProUGUI>();
        text.fontSize = 15f;
        text.color = PitMenuUiTheme.TextPrimary;
        var trt = text.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(10f, 6f);
        trt.offsetMax = new Vector2(-10f, -6f);
        input.textComponent = text;
        input.textViewport = trt;
        if (password) input.contentType = TMP_InputField.ContentType.Password;
        var phGo = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        phGo.transform.SetParent(go.transform, false);
        var ph = phGo.GetComponent<TextMeshProUGUI>();
        ph.fontSize = 15f;
        ph.color = new Color(PitMenuUiTheme.TextMuted.r, PitMenuUiTheme.TextMuted.g, PitMenuUiTheme.TextMuted.b, 0.5f);
        ph.text = placeholder;
        var prt = ph.GetComponent<RectTransform>();
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.offsetMin = new Vector2(10f, 6f);
        prt.offsetMax = new Vector2(-10f, -6f);
        input.placeholder = ph;
        PitMenuUiTheme.StyleInputField(input);
        return input;
    }

    static TMP_Text AddTmp(Transform parent, string name, float size, Color c, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.fontSize = size;
        t.color = c;
        t.alignment = align;
        t.enableWordWrapping = true;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, size + 10f);
        return t;
    }

    static Button AddLayoutButton(Transform parent, string name, string label, float width)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, 36f);
        var img = go.GetComponent<Image>();
        var b = go.GetComponent<Button>();
        b.targetGraphic = img;
        var le = go.AddComponent<LayoutElement>();
        le.minWidth = width;
        le.preferredWidth = width;
        le.minHeight = 36f;
        le.preferredHeight = 36f;
        le.flexibleWidth = 0f;
        le.flexibleHeight = 0f;
        var txtGo = new GameObject("T", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGo.transform.SetParent(go.transform, false);
        var txt = txtGo.GetComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = 13f;
        txt.fontStyle = FontStyles.Bold;
        txt.color = PitMenuUiTheme.TextPrimary;
        txt.alignment = TextAlignmentOptions.Center;
        var tr = txt.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;
        return b;
    }

    void ApplySignedInProfile()
    {
        if (!LocalSaveRuntime.IsSignedIn) return;
        if (!LocalAccountDatabase.TryLoadAccountProfile(LocalSaveRuntime.ActiveUserId, out var acc, out var hs, out var settings))
            return;
        AccountProgression.Instance?.OverwriteFromSave(acc);
        HighScoreManager.Instance?.ApplyFromSave(hs);
        SettingsManager.ApplyFromSave(settings);
    }

    sealed class SaveSlotRowChrome : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Image Background;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Background != null) Background.color = PitMenuUiTheme.SlotRowHover;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (Background != null) Background.color = PitMenuUiTheme.SlotRow;
        }
    }
}
