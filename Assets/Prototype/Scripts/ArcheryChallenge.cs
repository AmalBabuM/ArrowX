using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class ArcheryChallenge : MonoBehaviour
{
    private enum RoundState { Ready, Running, Cleared, TimedOut, Finished }

    private static readonly (string Name, int Seconds, float Speed)[] Stages =
    {
        ("Warm-up", 25, 1f),
        ("Crosswind", 20, 1.45f),
        ("Quick draw", 15, 1.9f)
    };

    private static readonly string[] LaneNames = { "LEFT", "CENTER", "RIGHT" };
    private const int PointsPerTarget = 100;

    [SerializeField] private MovingTarget centerTarget;
    [SerializeField] private Vector3 leftOffset = new Vector3(0.75f, 0f, -1.45f);
    [SerializeField] private Vector3 rightOffset = new Vector3(0.45f, 0f, 1.45f);

    private readonly HashSet<MovingTarget> cleared = new HashSet<MovingTarget>();
    private MovingTarget[] targets;
    private RoundState state;
    private int stage;
    private int bankedScore;
    private int displayedSeconds;
    private float deadline;
    private float nextRoundAt;
    private Text title;
    private Text counters;
    private Text lanes;
    private Text instructions;

    private int Score => bankedScore + cleared.Count * PointsPerTarget;

    private void Start()
    {
        targets = new[]
        {
            SpawnTarget("Challenge Target (Left)", leftOffset, 1.8f),
            centerTarget,
            SpawnTarget("Challenge Target (Right)", rightOffset, 3.6f)
        };

        foreach (var target in targets)
            target.KnockedDown += RecordHit;

        BuildScoreboard();
        PrepareRound();
    }

    private MovingTarget SpawnTarget(string targetName, Vector3 offset, float phase)
    {
        var source = centerTarget.transform;
        var target = Instantiate(centerTarget, source.position + offset, source.rotation, source.parent);
        target.name = targetName;
        target.SetPhase(phase);
        return target;
    }

    private void OnDestroy()
    {
        if (targets == null)
            return;

        foreach (var target in targets)
            target.KnockedDown -= RecordHit;
    }

    private void Update()
    {
        if (state == RoundState.Running)
        {
            int seconds = Mathf.Max(0, Mathf.CeilToInt(deadline - Time.time));
            if (seconds == 0)
                EndRound(RoundState.TimedOut);
            else if (seconds != displayedSeconds)
                RefreshCounters(seconds);
        }
        else if (state != RoundState.Ready && Time.time >= nextRoundAt)
        {
            if (state == RoundState.Cleared)
            {
                bankedScore = Score;
                stage++;
            }
            else if (state == RoundState.Finished)
            {
                bankedScore = 0;
                stage = 0;
            }

            PrepareRound();
        }
    }

    private void PrepareRound()
    {
        cleared.Clear();
        state = RoundState.Ready;
        foreach (var target in targets)
            target.SpeedMultiplier = Stages[stage].Speed;

        RefreshScoreboard();
    }

    private void RecordHit(MovingTarget target)
    {
        if (state == RoundState.Running && Time.time >= deadline)
            EndRound(RoundState.TimedOut);

        if ((state != RoundState.Ready && state != RoundState.Running) || !cleared.Add(target))
            return;

        if (state == RoundState.Ready)
        {
            state = RoundState.Running;
            deadline = Time.time + Stages[stage].Seconds;
        }

        if (cleared.Count == targets.Length)
            EndRound(stage == Stages.Length - 1 ? RoundState.Finished : RoundState.Cleared);
        else
            RefreshScoreboard();
    }

    private void EndRound(RoundState result)
    {
        state = result;
        nextRoundAt = Time.time + (result == RoundState.Finished ? 6f : 4f);
        RefreshScoreboard();
    }

    private void RefreshScoreboard()
    {
        title.text = $"ROUND {stage + 1}/{Stages.Length}  |  {Stages[stage].Name.ToUpperInvariant()}";
        var markers = new string[targets.Length];
        for (int i = 0; i < targets.Length; i++)
            markers[i] = cleared.Contains(targets[i]) ? $"<color=#79E9AC>{LaneNames[i]} OK</color>" : LaneNames[i];
        lanes.text = string.Join("    /    ", markers);

        instructions.text = state switch
        {
            RoundState.Ready => "Hit a target to start the timer",
            RoundState.Running => "Knock down each lane once",
            RoundState.Cleared => "Round cleared. Next round in 4s.",
            RoundState.TimedOut => "Time's up. Retrying in 4s.",
            _ => "Challenge complete. New run in 6s."
        };

        int seconds = state == RoundState.Ready
            ? Stages[stage].Seconds
            : Mathf.Max(0, Mathf.CeilToInt(deadline - Time.time));
        RefreshCounters(seconds);
    }

    private void RefreshCounters(int seconds)
    {
        displayedSeconds = seconds;
        counters.text = $"TARGETS {targets.Length - cleared.Count}    SCORE {Score}    TIME {seconds:00}s";
    }

    private void BuildScoreboard()
    {
        var panel = new GameObject("Challenge Scoreboard", typeof(RectTransform), typeof(Canvas), typeof(Image));
        var rect = (RectTransform)panel.transform;
        rect.SetParent(transform, false);
        rect.localScale = Vector3.one * 0.002f;
        rect.sizeDelta = new Vector2(490f, 168f);
        panel.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        var background = panel.GetComponent<Image>();
        background.color = new Color(0.035f, 0.055f, 0.08f, 0.94f);
        background.raycastTarget = false;

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        title = AddLine(rect, font, "Round", 57f, 27, new Color(1f, 0.8f, 0.45f));
        counters = AddLine(rect, font, "Counters", 18f, 23, Color.white);
        lanes = AddLine(rect, font, "Lanes", -18f, 22, Color.white);
        instructions = AddLine(rect, font, "Instructions", -54f, 22, new Color(0.78f, 0.86f, 0.95f));
    }

    private static Text AddLine(RectTransform parent, Font font, string label, float y, int size, Color color)
    {
        var line = new GameObject(label, typeof(RectTransform), typeof(Text));
        var rect = (RectTransform)line.transform;
        rect.SetParent(parent, false);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(470f, 36f);

        var text = line.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }
}
