using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main menu UI: Supabase sign-in, save slot list (max 3), load / create / delete with confirmation.
/// </summary>
public class MainMenuCloudPanel : MonoBehaviour
{
    const int MaxSaves = 3;

    [SerializeField] private MainMenuManager mainMenu;

    private CloudBackendConfig _config;
    private SupabaseApi _api;

    private TMP_Text _status;
    private TMP_InputField _email;
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

    private readonly List<GameObject> _listRows = new List<GameObject>();

    void Awake()
    {
        if (mainMenu == null) mainMenu = GetComponent<MainMenuManager>();
        CloudCoroutineHost.EnsureExists();
        _config = Resources.Load<CloudBackendConfig>("CloudBackendConfig");
        CloudSaveRuntime.SetConfig(_config);
        BuildUi();
    }

    void Start()
    {
        if (_config != null && _config.IsConfigured)
            StartCoroutine(TrySilentRefresh());
    }

    private IEnumerator TrySilentRefresh()
    {
        if (string.IsNullOrEmpty(CloudSaveRuntime.LoadRefreshToken())) yield break;
        string token = null;
        string err = null;
        yield return CloudAuthBroker.EnsureAccess((t, e) => { token = t; err = e; });
        if (!string.IsNullOrEmpty(token))
        {
            SetStatus($"Signed in as {CloudSaveRuntime.LoadUserEmail()}");
            if (_signIn != null) _signIn.gameObject.SetActive(false);
            if (_signUp != null) _signUp.gameObject.SetActive(false);
            if (_logout != null) _logout.gameObject.SetActive(true);
            yield return RefreshListRoutine();
        }
    }

    private void BuildUi()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        var root = new GameObject("CloudAccountPanel");
        root.transform.SetParent(canvas.transform, false);
        var rt = root.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(24f, 0f);
        rt.sizeDelta = new Vector2(420f, 520f);

        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.06f, 0.08f, 0.94f);

        float y = -16f;
        _status = AddText(root.transform, "CloudStatus", new Vector2(12f, y), 14, Color.gray, TextAlignmentOptions.TopLeft);
        _status.rectTransform.sizeDelta = new Vector2(396f, 56f);
        y -= 64f;

        if (_config == null || !_config.IsConfigured)
        {
            _status.text = "Cloud saves: add Resources/CloudBackendConfig (see Docs/CloudSave_Setup.md).";
            _status.color = new Color(1f, 0.7f, 0.4f);
            return;
        }

        _api = new SupabaseApi(_config.supabaseUrl, _config.supabaseAnonKey);

        _email = AddInput(root.transform, "Email", new Vector2(12f, y), "email");
        y -= 52f;
        _password = AddInput(root.transform, "Password", new Vector2(12f, y), "password", true);
        y -= 56f;

        _signIn = AddButton(root.transform, "SignIn", new Vector2(12f, y), "Sign in", OnClickSignIn);
        _signUp = AddButton(root.transform, "SignUp", new Vector2(150f, y), "Sign up", OnClickSignUp);
        y -= 44f;

        _logout = AddButton(root.transform, "Logout", new Vector2(12f, y), "Log out", OnClickLogout);
        _logout.gameObject.SetActive(false);
        y -= 52f;

        AddText(root.transform, "SavesLabel", new Vector2(12f, y), 16, new Color(0.9f, 0.75f, 0.2f), TextAlignmentOptions.Left).text = "SAVE SLOTS (max 3)";
        y -= 28f;

        var listGo = new GameObject("SaveList");
        listGo.transform.SetParent(root.transform, false);
        var listRt = listGo.AddComponent<RectTransform>();
        listRt.anchorMin = new Vector2(0f, 1f);
        listRt.anchorMax = new Vector2(1f, 1f);
        listRt.pivot = new Vector2(0.5f, 1f);
        listRt.anchoredPosition = new Vector2(0f, y);
        listRt.sizeDelta = new Vector2(-24f, 200f);
        var scroll = listGo.AddComponent<ScrollRect>();
        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(listGo.transform, false);
        var mask = viewport.AddComponent<RectMask2D>();
        var vRt = viewport.AddComponent<RectTransform>();
        vRt.anchorMin = Vector2.zero;
        vRt.anchorMax = Vector2.one;
        vRt.offsetMin = Vector2.zero;
        vRt.offsetMax = Vector2.zero;
        scroll.viewport = vRt;

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        _listRoot = content.transform;
        var cRt = content.AddComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0f, 1f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot = new Vector2(0.5f, 1f);
        cRt.anchoredPosition = Vector2.zero;
        var fit = content.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var layout = content.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 4f;
        scroll.content = cRt;

        y -= 212f;
        _slotName = AddInput(root.transform, "SlotNameInput", new Vector2(12f, y), "New save name");
        y -= 48f;
        _createSave = AddButton(root.transform, "CreateSave", new Vector2(12f, y), "Create new save", OnClickCreateSave);
        _refresh = AddButton(root.transform, "Refresh", new Vector2(200f, y), "Refresh list", OnClickRefresh);

        BuildDeleteConfirm(root.transform);
    }

    private void BuildDeleteConfirm(Transform parent)
    {
        _deleteConfirm = new GameObject("DeleteConfirm");
        _deleteConfirm.transform.SetParent(parent, false);
        var rt = _deleteConfirm.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = _deleteConfirm.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.75f);
        _deleteConfirm.SetActive(false);

        var box = new GameObject("Box");
        box.transform.SetParent(_deleteConfirm.transform, false);
        var boxRt = box.AddComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0.5f, 0.5f);
        boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.sizeDelta = new Vector2(320f, 160f);
        var boxBg = box.AddComponent<Image>();
        boxBg.color = new Color(0.12f, 0.1f, 0.14f, 1f);

        var txt = AddText(box.transform, "Q", Vector2.zero, 18, Color.white, TextAlignmentOptions.Center);
        txt.rectTransform.anchorMin = new Vector2(0f, 0.45f);
        txt.rectTransform.anchorMax = new Vector2(1f, 1f);
        txt.text = "Delete this save permanently?";

        AddButton(box.transform, "Yes", new Vector2(40f, 24f), "Delete", ConfirmDeleteYes);
        AddButton(box.transform, "No", new Vector2(180f, 24f), "Cancel", ConfirmDeleteNo);
    }

    private void OnClickRefresh() => StartCoroutine(RefreshListRoutine());

    private void OnClickSignIn() => StartCoroutine(SignInRoutine());

    private void OnClickSignUp() => StartCoroutine(SignUpRoutine());

    private void OnClickLogout()
    {
        CloudSaveRuntime.ClearSession();
        _logout.gameObject.SetActive(false);
        if (_signIn != null) _signIn.gameObject.SetActive(true);
        if (_signUp != null) _signUp.gameObject.SetActive(true);
        ClearList();
        SetStatus("Signed out.");
    }

    private void OnClickCreateSave() => StartCoroutine(CreateSaveRoutine());

    private IEnumerator SignInRoutine()
    {
        SetStatus("Signing in…");
        SupabaseAuthResponse auth = null;
        string err = null;
        yield return _api.SignIn(_email.text.Trim(), _password.text, (a, e) => { auth = a; err = e; });
        if (!string.IsNullOrEmpty(err))
        {
            SetStatus(err, true);
            yield break;
        }
        FinishAuth(auth);
    }

    private IEnumerator SignUpRoutine()
    {
        SetStatus("Creating account…");
        SupabaseAuthResponse auth = null;
        string err = null;
        yield return _api.SignUp(_email.text.Trim(), _password.text, (a, e) => { auth = a; err = e; });
        if (!string.IsNullOrEmpty(err))
        {
            SetStatus(err, true);
            yield break;
        }
        if (auth?.AccessToken == null)
        {
            SetStatus("Check your email to confirm, then sign in.");
            yield break;
        }
        FinishAuth(auth);
    }

    private void FinishAuth(SupabaseAuthResponse auth)
    {
        CloudSaveRuntime.StoreTokens(auth.AccessToken, auth.ExpiresIn, auth.RefreshToken);
        CloudSaveRuntime.SetUserEmail(auth.User?.Email ?? _email.text.Trim());
        if (_signIn != null) _signIn.gameObject.SetActive(false);
        if (_signUp != null) _signUp.gameObject.SetActive(false);
        _logout.gameObject.SetActive(true);
        SetStatus($"Signed in as {CloudSaveRuntime.LoadUserEmail()}");
        StartCoroutine(RefreshListRoutine());
    }

    private IEnumerator RefreshListRoutine()
    {
        if (_listRoot == null) yield break;

        string token = null;
        string err = null;
        yield return CloudAuthBroker.EnsureAccess((t, e) => { token = t; err = e; });
        if (!string.IsNullOrEmpty(err))
        {
            SetStatus(err, true);
            yield break;
        }

        GameSaveRowDto[] rows = null;
        yield return _api.ListGameSaves(token, (r, e) => { rows = r; err = e; });
        if (!string.IsNullOrEmpty(err))
        {
            SetStatus(err, true);
            yield break;
        }

        RenderList(rows);
        bool atCap = rows != null && rows.Length >= MaxSaves;
        _createSave.interactable = !atCap;
        if (atCap)
            SetStatus($"{CloudSaveRuntime.LoadUserEmail()} — 3/3 saves. Delete one to create another.");
        else
            SetStatus($"{CloudSaveRuntime.LoadUserEmail()} — {rows?.Length ?? 0}/3 saves.");
    }

    private void RenderList(GameSaveRowDto[] rows)
    {
        ClearList();
        if (rows == null || rows.Length == 0)
        {
            var t = AddText(_listRoot, "Empty", Vector2.zero, 14, Color.gray, TextAlignmentOptions.Center);
            t.text = "No saves yet. Create one below.";
            _listRows.Add(t.gameObject);
            return;
        }

        foreach (var row in rows)
        {
            string label = row.SlotLabel;
            string updated = row.UpdatedAt ?? "";
            if (row.TryGetDocument(out var doc, out _))
            {
                int world = doc.run?.activeWorld ?? 0;
                int wave = doc.run?.waveIndex ?? 0;
                label = $"{row.SlotLabel}  ·  W{wave + 1}  ·  world {world}";
            }

            var rowGo = new GameObject("Row_" + row.Id);
            rowGo.transform.SetParent(_listRoot, false);
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

            string id = row.Id;
            AddSmallButton(rowGo.transform, "Load", () => StartCoroutine(LoadSaveRoutine(id)));
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

    private IEnumerator LoadSaveRoutine(string id)
    {
        string token = null;
        string err = null;
        yield return CloudAuthBroker.EnsureAccess((t, e) => { token = t; err = e; });
        if (!string.IsNullOrEmpty(err)) { SetStatus(err, true); yield break; }

        GameSaveRowDto[] rows = null;
        yield return _api.ListGameSaves(token, (r, e) => { rows = r; err = e; });
        GameSaveRowDto row = null;
        if (rows != null)
            foreach (var x in rows)
                if (x.Id == id) { row = x; break; }
        if (row == null || !row.TryGetDocument(out var doc, out err))
        {
            SetStatus(err ?? "Save not found.", true);
            yield break;
        }

        CloudSaveRuntime.ActiveSaveId = id;
        CloudSaveRuntime.PendingHydrate = doc;
        PlayerPrefs.SetString("Cloud_ActiveSlotLabel", row.SlotLabel ?? "Save");
        PlayerPrefs.Save();
        bool needAutoTutorial = doc.run != null && !doc.run.tutorialCompleted && doc.run.activeWorld != 1;
        MainMenuManager.SetShouldRunTutorialForLoadedSave(needAutoTutorial);
        mainMenu.LoadMainSceneFromCloud();
    }

    private IEnumerator CreateSaveRoutine()
    {
        string token = null;
        string err = null;
        yield return CloudAuthBroker.EnsureAccess((t, e) => { token = t; err = e; });
        if (!string.IsNullOrEmpty(err)) { SetStatus(err, true); yield break; }

        GameSaveRowDto[] existing = null;
        yield return _api.ListGameSaves(token, (r, e) => { existing = r; err = e; });
        if (!string.IsNullOrEmpty(err)) { SetStatus(err, true); yield break; }
        if (existing != null && existing.Length >= MaxSaves)
        {
            SetStatus("You already have 3 saves. Delete one first.", true);
            yield break;
        }

        string name = string.IsNullOrWhiteSpace(_slotName.text) ? "Save" : _slotName.text.Trim();
        if (name.Length > 40) name = name.Substring(0, 40);
        var doc = GameSaveSerializer.CreateNewRun();
        PlayerPrefs.SetInt("Meta_TutorialCompleted", 0);
        PlayerPrefs.Save();

        GameSaveRowDto created = null;
        yield return _api.CreateGameSave(token, name, doc, (r, e) => { created = r; err = e; });
        if (!string.IsNullOrEmpty(err))
        {
            SetStatus(err.Contains("MAX_SAVES") ? "Server: max 3 saves per account." : err, true);
            yield break;
        }

        CloudSaveRuntime.ActiveSaveId = created.Id;
        CloudSaveRuntime.PendingHydrate = doc;
        PlayerPrefs.SetString("Cloud_ActiveSlotLabel", name);
        PlayerPrefs.Save();
        bool needAutoTutorial = doc.run != null && !doc.run.tutorialCompleted && doc.run.activeWorld != 1;
        MainMenuManager.SetShouldRunTutorialForLoadedSave(needAutoTutorial);
        mainMenu.LoadMainSceneFromCloud();
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
            StartCoroutine(DeleteRoutine(_pendingDeleteId));
        _pendingDeleteId = null;
    }

    private IEnumerator DeleteRoutine(string id)
    {
        string token = null;
        string err = null;
        yield return CloudAuthBroker.EnsureAccess((t, e) => { token = t; err = e; });
        if (!string.IsNullOrEmpty(err)) { SetStatus(err, true); yield break; }

        bool ok = false;
        yield return _api.DeleteGameSave(token, id, (s, e) => { ok = s; err = e; });
        if (!ok) { SetStatus(err ?? "Delete failed", true); yield break; }
        if (CloudSaveRuntime.ActiveSaveId == id) CloudSaveRuntime.ActiveSaveId = null;
        SetStatus("Save deleted.");
        yield return RefreshListRoutine();
    }

    private void SetStatus(string msg, bool error = false)
    {
        if (_status == null) return;
        _status.text = msg;
        _status.color = error ? new Color(1f, 0.45f, 0.35f) : Color.gray;
    }

    private static TMP_InputField AddInput(Transform parent, string name, Vector2 pos, string placeholder, bool password = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(-24f, 36f);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.18f);
        var input = go.AddComponent<TMP_InputField>();
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
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
        var phGo = new GameObject("Placeholder");
        phGo.transform.SetParent(go.transform, false);
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
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
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
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(120f, 32f);
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
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(52f, 28f);
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
}
