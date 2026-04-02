using UnityEngine;

/// <summary>
/// One-shot trigger events used inside the tutorial hallway.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TutorialHallTrigger : MonoBehaviour
{
    public enum TriggerAction
    {
        FirstGateApproach,
        FirstGatePassed,
        SecondGatePassed,
        OrbAreaStart,
        NpcHint
    }

    [SerializeField] private TriggerAction action = TriggerAction.FirstGateApproach;
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered;

    public void SetActionByIndex(int index)
    {
        if (index < 0) index = 0;
        if (index > 4) index = 4;
        action = (TriggerAction)index;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (triggerOnce && hasTriggered) return;
        hasTriggered = true;

        switch (action)
        {
            case TriggerAction.FirstGateApproach:
                TutorialManager.Instance?.OnReachedFirstGateApproach();
                break;
            case TriggerAction.FirstGatePassed:
                TutorialManager.Instance?.OnPassedFirstGate();
                break;
            case TriggerAction.SecondGatePassed:
                TutorialManager.Instance?.OnPassedSecondGate();
                break;
            case TriggerAction.OrbAreaStart:
                TutorialManager.Instance?.OnEnteredOrbArea();
                break;
            case TriggerAction.NpcHint:
                TutorialManager.Instance?.ShowTemporaryMessage("Press E to speak with the Guide.", 4f);
                break;
        }
    }
}

