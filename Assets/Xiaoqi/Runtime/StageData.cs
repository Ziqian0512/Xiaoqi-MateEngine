using System;

[Serializable]
public class StageData
{
    public int v;
    public string ts;
    public string where;
    public StageSegment seg;
    public string spot;
    public string pose;
    public string prop;
    public StageNext next;
    public StageEvent @event;
    public StagePhone phone;
    public StageFace face;

    [Serializable]
    public class StageSegment
    {
        public string id;
        public string name;
        public string act;
        public string start;
        public string end;
        public float progress;
    }

    [Serializable]
    public class StageNext
    {
        public string at;
        public string where;
        public string spot;
        public string pose;
    }

    [Serializable]
    public class StageEvent
    {
        public string at;
        public string text;
        public string until;
    }

    [Serializable]
    public class StagePhone
    {
        public bool typing;
    }

    [Serializable]
    public class StageFace
    {
        public string top;
        public float level;
        public bool rose;
    }
}
