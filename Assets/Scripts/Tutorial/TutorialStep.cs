using System;

public enum TutorialTriggerType
{
    Move,
    Attack,
    KillEnemy,
    OpenUpgrade,
    SelectUpgrade
}

[Serializable]
public class TutorialStep
{
    public string instruction;
    public TutorialTriggerType triggerType;

    public TutorialStep(string instruction, TutorialTriggerType triggerType)
    {
        this.instruction = instruction;
        this.triggerType = triggerType;
    }
}
