using UnityEngine;

/// <summary>
/// Attach to the root Skill Tree UI panel.
/// When panel is enabled, it notifies TutorialManager to show lobby skill-tree text once.
/// </summary>
public class SkillTreeTutorialHook : MonoBehaviour
{
    void OnEnable()
    {
        TutorialManager.Instance?.OnSkillTreeOpened();
    }
}
