using System.Collections.Generic;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Local JSON account gate for the main menu.
/// Username/password is required before New Game / Load Game become clickable.
/// </summary>
public class MainMenuLocalAccountPanel : MonoBehaviour
{
    const int MaxSaves = LocalAccountDatabase.MaxSavesPerUser;

    [SerializeField] private MainMenuManager mainMenu;

    private TMP_Text _status;
    private TMP_InputField _username;
    private TMP_InputField _password;
    private TMP_InputField _slotName;
    private Button _signIn;
    private Button _signUp;
    private Button _logout;
    private Button _createSave;
    private Button _refresh;
    private Transform _listRoot;
    private GameObject _deleteConfirm;
    private string _pendingDeleteId;
    private GameObject _gateOverlay;
    private GameObject _gatePanel;

    private readonly List<GameObject> _listRows = new List<GameObject>();

    void Awake()
    {
        if (mainMenu == null) mainMenu = GetComponent<MainMenuManager>();
        BuildUi();
    }

    void Start()
    {
        mainMenu?.SetMainMenuLocked(true);

        if (LocalSaveRuntime.IsSignedIn &&
            LocalAccountDatabase.FindUser(LocalAccountDatabase.LoadOrCreate(), LocalSaveRuntime.ActiveUserId) != null)
        {
            ApplySignedInProfile();
            RefreshLoggedInUi();
        }
        else
        {
            LocalSaveRuntime.SignOut();
            ShowGate(true);
            SetStatus("Sign in to continue.");
        }
    }

    private void BuildUi()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        _gateOverlay = CreateUi("LocalAccountGateOverlay", canvas.transform, Vector2.zero);
        RectTransform ovRt = _gateOverlay.GetComponent<RectTransform>();
        ovRt.anchorMin = Vector2.zero;
        ovRt.anchorMax = Vector2.one;
        ovRt.offsetMin = Vector2.zero;
        ovRt.offsetMax = Vector2.zero;
        _gateOverlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

        GameObject root = CreateUi("LocalAccountPanel", _gateOverlay.transform, new Vector2(480f, 560f));
        _gatePanel = root;
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        root.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.08f, 0.96f);

        float y = -20f;
        AddText(root.transform, "Title", new Vector2(0f, y), 26f, new Color(0.95f, 0.8f, 0.2f), TextAlignmentOptions.Center).text = "ACCOUNT LOGIN";
        y -= 44f;
        _status = AddText(root.transform, "LocalStatus", new Vector2(12f, y), 13, Color.gray, TextAlignmentOptions.TopLeft);
        _status.rectTransform.sizeDelta = new Vector2(456f, 72f);
        y -= 80f;

        _username = AddInput(root.transform, "Username", new Vector2(12f, y), "username");
        if (!string.IsNullOrEmpty(LocalSaveRuntime.LoadRememberedEmail()))
            _username.text = LocalSaveRuntime.LoadRememberedEmail();
        y -= 52f;
        _password = AddInput(root.transform, "Password", new Vector2(12f, y), "password", true);
        y -= 56f;

        _signIn = AddButton(root.transform, "SignIn", new Vector2(12f, y), "Sign in", OnClickSignIn);
        _signUp = AddButton(root.transform, "SignUp", new Vector2(150f, y), "Create", OnClickSignUp);
        y -= 44f;

        _logout = AddButton(root.transform, "Logout", new Vector2(12f, y), "Log out", OnClickLogout);
        _logout.gameObject.SetActive(false);
        y -= 52f;

        AddText(root.transform, "SavesLabel", new Vector2(12f, y), 16, new Color(0.9f, 0.75f, 0.2f), TextAlignmentOptions.Left).text =
            "SAVE SLOTS (max 3)";
        y -= 28f;

        GameObject listPanel = CreateUi("SaveList", root.transform, new Vector2(396f, 200f));
        RectTransform listRt = listPanel.GetComponent<RectTransform>();
        listRt.anchorMin = new Vector2(0f, 1f);
        listRt.anchorMax = new Vector2(0f, 1f);
        listRt.pivot = new Vector2(0f, 1f);
        listRt.anchoredPosition = new Vector2(12f, y);
        listPanel.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.16f, 0.6f);

        GameObject content = CreateUi("Content", listPanel.transform, new Vector2(384f, 188f));
        RectTransform cRt = content.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0f, 1f);
        cRt.anchorMax = new Vector2(0f, 1f);
        cRt.pivot = new Vector2(0f, 1f);
        cRt.anchoredPosition = new Vector2(6f, -6f);
        var layout = content.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.spacing = 4f;
        _listRoot = content.transform;

        y -= 212f;
        _slotName = AddInput(root.transform, "SlotNameInput", new Vector2(12f, y), "new save name");
        y -= 48f;
        _createSave = AddButton(root.transform, "CreateSave", new Vector2(12f, y), "Create save", OnClickCreateSave);
        _refresh = AddButton(root.transform, "Refresh", new Vector2(150f, y), "Refresh", OnClickRefresh);

        BuildDeleteConfirm(root.transform);
    }

    private void BuildDeleteConfirm(Transform parent)
    {
        _deleteConfirm = CreateUi("DeleteConfirm", parent, Vector2.zero);
        var rt = _deleteConfirm.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        _deleteConfirm.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
        _deleteConfirm.SetActive(false);

        var box = CreateUi("Box", _deleteConfirm.transform, new Vector2(320f, 160f));
        var boxRt = box.GetComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0.5f, 0.5f);
        boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        box.AddComponent<Image>().color = new Color(0.12f, 0.1f, 0.14f, 1f);

        var txt = AddText(box.transform, "Q", Vector2.zero, 18, Color.white, TextAlignmentOptions.Center);
        txt.rectTransform.anchorMin = new Vector2(0f, 0.45f);
        txt.rectTransform.anchorMax = new Vector2(1f, 1f);
        txt.text = "Delete this save permanently?";

        AddButton(box.transform, "Yes", new Vector2(40f, 24f), "Delete", ConfirmDeleteYes);
        AddButton(box.transform, "No", new Vector2(180f, 24f), "Cancel", ConfirmDeleteNo);
    }

    private void OnClickRefresh()
    {
        if (!LocalSaveRuntime.IsSignedIn) return;
        LocalAccountDatabase.SaveAccountProfileFromRuntime(LocalSaveRuntime.ActiveUserId);
        RefreshList();
    }

    private void OnClickSignIn()
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
        RefreshLoggedInUi();
    }

    private void OnClickSignUp()
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
        RefreshLoggedInUi();
    }

    private void OnClickLogout()
    {
        if (LocalSaveRuntime.IsSignedIn)
            LocalAccountDatabase.SaveAccountProfileFromRuntime(LocalSaveRuntime.ActiveUserId);
        LocalSaveRuntime.SignOut();
        mainMenu?.SetMainMenuLocked(true);
        ShowGate(true);
        _logout.gameObject.SetActive(false);
        if (_signIn != null) _signIn.gameObject.SetActive(true);
        if (_signUp != null) _signUp.gameObject.SetActive(true);
        ClearList();
        SetStatus("Signed out. Data file is kept on disk.");
    }

    private void OnClickCreateSave()
    {
        if (!LocalSaveRuntime.IsSignedIn)
        {
            SetStatus("Sign in first.", true);
            return;
        }

        var list = LocalAccountDatabase.ListSaves(LocalSaveRuntime.ActiveUserId);
        if (list.Count >= MaxSaves)
        {
            SetStatus("3/3 saves — delete one before creating another.", true);
            return;
        }

        string name = string.IsNullOrWhiteSpace(_slotName.text) ? "Save" : _slotName.text.Trim();
        if (name.Length > 40) name = name.Substring(0, 40);
        var doc = GameSaveSerializer.CreateNewRun();
        if (LocalAccountDatabase.TryLoadAccountProfile(LocalSaveRuntime.ActiveUserId, out var acc, out var hs, out var st))
        {
            doc.account = acc;
            doc.highScore = hs;
            doc.settings = st;
        }
        PlayerPrefs.SetInt("Meta_TutorialCompleted", 0);
        PlayerPrefs.Save();

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

    private void RefreshList()
    {
        if (_listRoot == null) return;
        var rows = LocalAccountDatabase.ListSaves(LocalSaveRuntime.ActiveUserId);
        RenderList(rows);
        bool atCap = rows.Count >= MaxSaves;
        if (_createSave != null) _createSave.interactable = !atCap;
        string em = LocalAccountDatabase.GetUserEmail(LocalSaveRuntime.ActiveUserId);
        if (atCap)
            SetStatus($"{em} — 3/3 saves. Delete one to create another.");
        else
            SetStatus($"{em} — {rows.Count}/3 saves.\n{Application.persistentDataPath}/CS4483/");
    }

    private void RenderList(List<LocalAccountDatabase.SaveSlotRecord> rows)
    {
        ClearList();
        if (rows == null || rows.Count == 0)
        {
            var t = AddText(_listRoot, "Empty", Vector2.zero, 14, Color.gray, TextAlignmentOptions.Center);
            t.text = "No saves yet. Create one below.";
            _listRows.Add(t.gameObject);
            return;
        }

        foreach (var row in rows)
        {
            string label = row.slotLabel;
            string updated = row.updatedAtIso ?? "";
            if (row.payload?.run != null)
            {
                int wave = row.payload.run.waveIndex;
                int world = row.payload.run.activeWorld;
                label = $"{row.slotLabel}  ·  W{wave + 1}  ·  world {world}";
            }

            var rowGo = CreateUi("Row_" + row.id, _listRoot, new Vector2(380f, 42f));
            var h = rowGo.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleLeft;
            h.spacing = 6f;
            h.childForceExpandWidth = false;

            var le = rowGo.AddComponent<LayoutElement>();
            le.minHeight = 36f;
            le.preferredHeight = 36f;

            var lt = AddText(rowGo.transform, "txt", Vector2.zero, 12, Color.white, TextAlignmentOptions.Left);
            lt.text = $"{label}\n<size=10><color=#888>{updated}</color></size>";
            var leT = lt.gameObject.AddComponent<LayoutElement>();
            leT.flexibleWidth = 1f;

            string id = row.id;
            AddSmallButton(rowGo.transform, "Load", () => LoadSave(id));
            AddSmallButton(rowGo.transform, "Del", () => PromptDelete(id));

            _listRows.Add(rowGo);
        }
    }

    private void ClearList()
    {
        foreach (var go in _listRows)
            if (go != null) Destroy(go);
        _listRows.Clear();
        foreach (Transform c in _listRoot)
            Destroy(c.gameObject);
    }

    private void LoadSave(string id)
    {
        if (!LocalSaveRuntime.IsSignedIn) return;
        var row = LocalAccountDatabase.FindSave(LocalSaveRuntime.ActiveUserId, id);
        if (row?.payload == null)
        {
            SetStatus("Save missing or corrupt.", true);
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

    private void PromptDelete(string id)
    {
        _pendingDeleteId = id;
        _deleteConfirm.SetActive(true);
    }

    private void ConfirmDeleteNo() => _deleteConfirm.SetActive(false);

    private void ConfirmDeleteYes()
    {
        _deleteConfirm.SetActive(false);
        if (!string.IsNullOrEmpty(_pendingDeleteId))
            DeleteSave(_pendingDeleteId);
        _pendingDeleteId = null;
    }

    private void DeleteSave(string id)
    {
        if (!LocalAccountDatabase.TryDeleteSave(LocalSaveRuntime.ActiveUserId, id, out string err))
        {
            SetStatus(err, true);
            return;
        }
        if (LocalSaveRuntime.ActiveSaveId == id) LocalSaveRuntime.ActiveSaveId = null;
        SetStatus("Save deleted.");
        RefreshList();
    }

    private void SetStatus(string msg, bool error = false)
    {
        if (_status == null) return;
        _status.text = msg;
        _status.color = error ? new Color(1f, 0.45f, 0.35f) : Color.gray;
    }

    private void RefreshLoggedInUi()
    {
        string username = LocalAccountDatabase.GetUserEmail(LocalSaveRuntime.ActiveUserId);
        SetStatus($"Signed in as {username}\nData file: {Application.persistentDataPath}/CS4483/local_accounts.json");
        if (_signIn != null) _signIn.gameObject.SetActive(false);
        if (_signUp != null) _signUp.gameObject.SetActive(false);
        if (_logout != null) _logout.gameObject.SetActive(true);
        mainMenu?.SetMainMenuLocked(false);
        ShowGate(false);
        RefreshList();
    }

    private void ShowGate(bool show)
    {
        if (_gateOverlay != null) _gateOverlay.SetActive(show);
    }

    private static GameObject CreateUi(string name, Transform parent, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = size;
        return go;
    }

    private static TMP_InputField AddInput(Transform parent, string name, Vector2 pos, string placeholder, bool password = false)
    {
        var go = CreateUi(name, parent, new Vector2(456f, 36f));
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        go.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.18f);
        var input = go.AddComponent<TMP_InputField>();
        var textGo = CreateUi("Text", go.transform, Vector2.zero);
        var text = textGo.AddComponent<TextMeshProUGUI>();
        text.fontSize = 14;
        text.color = Color.white;
        var trt = text.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(8f, 4f);
        trt.offsetMax = new Vector2(-8f, -4f);
        input.textComponent = text;
        input.textViewport = trt;
        if (password) input.contentType = TMP_InputField.ContentType.Password;
        var phGo = CreateUi("Placeholder", go.transform, Vector2.zero);
        var ph = phGo.AddComponent<TextMeshProUGUI>();
        ph.fontSize = 14;
        ph.color = new Color(1f, 1f, 1f, 0.35f);
        ph.text = placeholder;
        var prt = ph.GetComponent<RectTransform>();
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.offsetMin = new Vector2(8f, 4f);
        prt.offsetMax = new Vector2(-8f, -4f);
        input.placeholder = ph;
        return input;
    }

    private static TMP_Text AddText(Transform parent, string name, Vector2 pos, float size, Color c, TextAlignmentOptions align)
    {
        var go = CreateUi(name, parent, Vector2.zero);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = size;
        t.color = c;
        t.alignment = align;
        return t;
    }

    private static Button AddButton(Transform parent, string name, Vector2 pos, string label, UnityEngine.Events.UnityAction onClick)
    {
        var go = CreateUi(name, parent, new Vector2(120f, 32f));
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.25f, 0.22f, 0.35f);
        var b = go.AddComponent<Button>();
        b.targetGraphic = img;
        b.onClick.AddListener(onClick);
        var txt = AddText(go.transform, "T", Vector2.zero, 13, Color.white, TextAlignmentOptions.Center);
        txt.rectTransform.anchorMin = Vector2.zero;
        txt.rectTransform.anchorMax = Vector2.one;
        txt.text = label;
        return b;
    }

    private static void AddSmallButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        var go = CreateUi("Btn_" + label, parent, new Vector2(52f, 28f));
        var img = go.AddComponent<Image>();
        img.color = new Color(0.3f, 0.25f, 0.4f);
        var b = go.AddComponent<Button>();
        b.targetGraphic = img;
        b.onClick.AddListener(onClick);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 52f;
        var t = AddText(go.transform, "t", Vector2.zero, 11, Color.white, TextAlignmentOptions.Center);
        t.rectTransform.anchorMin = Vector2.zero;
        t.rectTransform.anchorMax = Vector2.one;
        t.text = label;
    }

    private void ApplySignedInProfile()
    {
        if (!LocalSaveRuntime.IsSignedIn) return;
        if (!LocalAccountDatabase.TryLoadAccountProfile(LocalSaveRuntime.ActiveUserId, out var acc, out var hs, out var settings))
            return;
        AccountProgression.Instance?.OverwriteFromSave(acc);
        HighScoreManager.Instance?.ApplyFromSave(hs);
        SettingsManager.ApplyFromSave(settings);
    }
}
