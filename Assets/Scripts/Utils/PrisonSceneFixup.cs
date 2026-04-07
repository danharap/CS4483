using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Runtime safety net for older saved scenes:
/// - Removes leftover tutorial portal/teleport objects from the lobby/menu area.
/// - Removes the tutorial guide NPC from inside the prison/tutorial area.
/// - Ensures the outside of the prison renders as solid black.
/// - Snaps prison structural transforms to a small grid to eliminate visible seams.
/// </summary>
static class PrisonSceneFixup
{
    const float Grid = 0.05f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Apply()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "MainScene") return; // prison/tutorial live inside MainScene in this project

        // Remove any leftover tutorial transport props.
        DestroyByName("LobbyToTutorial_Portal");
        DestroyByName("Tutorial_NPC");

        // Remove tutorial guide NPC from the prison area (talking NPC should live in lobby only).
        DestroyByName("Tutorial_GuideNPC");

        EnsureBlackBackdrop();
        EnsureOutsideBlackPlates();
        RemoveLegacyCellBackWalls();
        SnapPrisonTransforms();
    }

    static void DestroyByName(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go != null) Object.Destroy(go);
    }

    static void EnsureBlackBackdrop()
    {
        // If the builder already created it, leave it alone.
        if (GameObject.Find("Tutorial_VoidBackdrop") != null) return;

        // Attach under TutorialArea if present; otherwise just place at world root.
        Transform parent = null;
        GameObject tutorialArea = GameObject.Find("Tutorial_Prison");
        if (tutorialArea == null) tutorialArea = GameObject.Find("TutorialArea");
        if (tutorialArea != null) parent = tutorialArea.transform;

        GameObject backdrop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backdrop.name = "Tutorial_VoidBackdrop";
        if (parent != null) backdrop.transform.SetParent(parent, false);
        backdrop.transform.position = new Vector3(0f, -2.0f, -40f);
        backdrop.transform.localScale = new Vector3(140f, 1f, 220f);

        var r = backdrop.GetComponent<Renderer>();
        if (r != null)
        {
            var mat = new Material(Shader.Find("Standard")) { color = Color.black };
            r.sharedMaterial = mat;
        }
    }

    /// <summary>
    /// Match editor-built black plates beside/beyond the corridor (hides brown ground from other levels).
    /// </summary>
    static void EnsureOutsideBlackPlates()
    {
        if (GameObject.Find("Tutorial_OutsideBlack_Left") != null) return;

        Transform parent = null;
        GameObject tutorialArea = GameObject.Find("Tutorial_Prison");
        if (tutorialArea == null) tutorialArea = GameObject.Find("TutorialArea");
        if (tutorialArea != null) parent = tutorialArea.transform;
        if (parent == null) return;

        Material mat = new Material(Shader.Find("Standard")) { color = Color.black };

        void Plate(string name, Vector3 pos, Vector3 scale)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            var rend = go.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = mat;
        }

        const float plateThickness = 0.25f;
        const float plateY = -0.12f;
        const float plateZExtent = 100f;
        const float plateXExtent = 45f;
        const float floorHalfX = 48f;
        const float cz = -40f;
        const float floorNorthZ = -47f;
        const float floorSouthZ = -33f;

        Plate("Tutorial_OutsideBlack_Left",
            new Vector3(-floorHalfX - plateXExtent * 0.5f - 0.25f, plateY, cz),
            new Vector3(plateXExtent, plateThickness, plateZExtent));
        Plate("Tutorial_OutsideBlack_Right",
            new Vector3(floorHalfX + plateXExtent * 0.5f + 0.25f, plateY, cz),
            new Vector3(plateXExtent, plateThickness, plateZExtent));
        Plate("Tutorial_OutsideBlack_North",
            new Vector3(0f, plateY, floorNorthZ - plateZExtent * 0.5f - 0.5f),
            new Vector3(plateXExtent * 2f + 20f, plateThickness, plateZExtent));
        Plate("Tutorial_OutsideBlack_South",
            new Vector3(0f, plateY, floorSouthZ + plateZExtent * 0.5f + 0.5f),
            new Vector3(plateXExtent * 2f + 20f, plateThickness, plateZExtent));
    }

    static void RemoveLegacyCellBackWalls()
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t == null) continue;
                if (!t.name.StartsWith("Cell_")) continue;
                Transform back = t.Find("Back");
                if (back != null) Object.Destroy(back.gameObject);
            }
        }
        // Old discrete Cell_* groups removed by rebuild; CellRow_* / PrisonCells_* have no legacy Back child.
    }

    static void SnapPrisonTransforms()
    {
        // Snap only known prison/tutorial structural elements to avoid touching gameplay objects.
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t == null) continue;
                string n = t.name;
                if (!(n.StartsWith("Cell_") ||
                      n.StartsWith("Cell_T") ||
                      n.StartsWith("Cell_B") ||
                      n.StartsWith("Cell_L") ||
                      n.StartsWith("Cell_R") ||
                      n.StartsWith("PrisonCells_") ||
                      n.StartsWith("CellRow_") ||
                      n.StartsWith("T_Wall_") ||
                      n.StartsWith("Tutorial_") ||
                      n == "TutorialArea" ||
                      n == "Tutorial_Prison"))
                    continue;

                Vector3 p = t.position;
                if (!IsFinite(p)) continue;

                t.position = new Vector3(
                    Mathf.Round(p.x / Grid) * Grid,
                    Mathf.Round(p.y / Grid) * Grid,
                    Mathf.Round(p.z / Grid) * Grid);

                // Snap yaw to 90° increments for structural pieces.
                Vector3 e = t.eulerAngles;
                if (IsFinite(e))
                    t.rotation = Quaternion.Euler(
                        Mathf.Round(e.x / 90f) * 90f,
                        Mathf.Round(e.y / 90f) * 90f,
                        Mathf.Round(e.z / 90f) * 90f);

                Vector3 s = t.localScale;
                if (IsFinite(s))
                    t.localScale = new Vector3(
                        Mathf.Round(s.x / Grid) * Grid,
                        Mathf.Round(s.y / Grid) * Grid,
                        Mathf.Round(s.z / Grid) * Grid);
            }
        }
    }

    static bool IsFinite(Vector3 v)
    {
        return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    }
}

