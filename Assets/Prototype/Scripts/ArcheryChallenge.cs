using UnityEngine;
using UnityEngine.UI;

public sealed class ArcheryChallenge : MonoBehaviour
{
    private enum RoundState { Ready, Running, Cleared, TimedOut, Finished }

    private static readonly (string Name, int Seconds, float HitWindow)[] Stages =
    {
        ("Warm-up", 25, 3f),
        ("Snap shot", 20, 2.5f),
        ("Quick draw", 15, 2f)
    };

    private const int HitsPerRound = 4;
    private const float SpawnPause = 0.65f;
    private static readonly Color32 Ink = new Color32(232, 240, 245, 255);
    private static readonly Color32 Muted = new Color32(166, 189, 204, 255);
    private static readonly Color32 Gold = new Color32(255, 207, 129, 255);

    [SerializeField] private MovingTarget popUpTarget;
    [SerializeField] private Vector3[] spawnOffsets;
    [SerializeField] private Sprite roundedPanel;

    private Vector3 targetOrigin;
    private int previousSpot = -1;
    private RoundState state;
    private int stage;
    private int hits;
    private int roundScore;
    private int bankedScore;
    private int displayedSeconds;
    private float deadline;
    private float nextRoundAt;
    private float nextAppearanceAt;
    private Text title;
    private Text hitsValue;
    private Text scoreValue;
    private Text timerValue;
    private Text feedback;
    private Text instructions;

    private int Score => bankedScore + roundScore;
    private bool InRound => state == RoundState.Ready || state == RoundState.Running;

    private void Start()
    {
        targetOrigin = popUpTarget.transform.position;
        popUpTarget.Hit += RecordHit;
        popUpTarget.Lowered += ScheduleNextTarget;
        popUpTarget.gameObject.SetActive(false);

        BuildScoreboard();
        PrepareRound();
    }

    private void RaiseTarget()
    {
        int spot = Random.Range(0, spawnOffsets.Length);
        if (spot == previousSpot && spawnOffsets.Length > 1)
            spot = (spot + Random.Range(1, spawnOffsets.Length)) % spawnOffsets.Length;

        previousSpot = spot;
        popUpTarget.RiseAt(targetOrigin + spawnOffsets[spot], Stages[stage].HitWindow);
    }

    private void ScheduleNextTarget()
    {
        nextAppearanceAt = Time.time + SpawnPause;
    }

    private void OnDestroy()
    {
        if (popUpTarget == null)
            return;

        popUpTarget.Hit -= RecordHit;
        popUpTarget.Lowered -= ScheduleNextTarget;
    }

    private void Update()
    {
        if (state == RoundState.Running)
        {
            int seconds = Mathf.Max(0, Mathf.CeilToInt(deadline - Time.time));
            if (seconds == 0)
                EndRound(RoundState.TimedOut);
            else if (seconds != displayedSeconds)
                RefreshTimer(seconds);
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

        if (InRound && !popUpTarget.gameObject.activeSelf && Time.time >= nextAppearanceAt)
            RaiseTarget();
    }

    private void PrepareRound()
    {
        hits = 0;
        roundScore = 0;
        state = RoundState.Ready;
        nextAppearanceAt = Time.time + 1f;
        feedback.text = "Watch for the next target";
        RefreshScoreboard();
    }

    private void RecordHit(int points)
    {
        if (state == RoundState.Running && Time.time >= deadline)
            EndRound(RoundState.TimedOut);

        if (!InRound)
            return;

        if (state == RoundState.Ready)
        {
            state = RoundState.Running;
            deadline = Time.time + Stages[stage].Seconds;
        }

        hits++;
        roundScore += points;
        feedback.text = points == MovingTarget.BullseyePoints ? $"BULLSEYE  +{points}" : $"LAST HIT  +{points}";

        if (hits >= HitsPerRound)
            EndRound(stage == Stages.Length - 1 ? RoundState.Finished : RoundState.Cleared);
        else
            RefreshScoreboard();
    }

    private void EndRound(RoundState result)
    {
        state = result;
        nextRoundAt = Time.time + (result == RoundState.Finished ? 6f : 4f);
        popUpTarget.Lower();
        RefreshScoreboard();
    }

    private void RefreshScoreboard()
    {
        title.text = $"ROUND {stage + 1}/{Stages.Length}   {Stages[stage].Name.ToUpperInvariant()}";
        hitsValue.text = $"{hits} / {HitsPerRound}";
        scoreValue.text = Score.ToString("00");
        instructions.text = state switch
        {
            RoundState.Ready => "Hit an upright target to start",
            RoundState.Running => "Shoot before the target drops",
            RoundState.Cleared => "Round cleared. Next round in 4s.",
            RoundState.TimedOut => "Time's up. Retrying in 4s.",
            _ => "Challenge complete. New run in 6s."
        };

        int seconds = state == RoundState.Ready
            ? Stages[stage].Seconds
            : Mathf.Max(0, Mathf.CeilToInt(deadline - Time.time));
        RefreshTimer(seconds);
    }

    private void RefreshTimer(int seconds)
    {
        displayedSeconds = seconds;
        timerValue.text = $"00:{seconds:00}";
        timerValue.color = state == RoundState.Running && seconds <= 5 ? Gold : Ink;
    }

    private void BuildScoreboard()
    {
        var panel = new GameObject("Challenge Scoreboard", typeof(RectTransform), typeof(Canvas), typeof(Image));
        var rect = (RectTransform)panel.transform;
        rect.SetParent(transform, false);
        rect.localScale = Vector3.one * 0.0024f;
        rect.sizeDelta = new Vector2(500f, 218f);
        panel.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        StylePanel(panel.GetComponent<Image>(), 23f, new Color32(74, 91, 104, 247));
        AddPanel(rect, "Background", new Rect(-248f, -107f, 496f, 214f), 21f, new Color32(16, 24, 33, 255));
        var scoreCard = AddPanel(rect, "Score Card", new Rect(81f, -36f, 147f, 117f), 16f, new Color32(51, 42, 30, 255));
        AddPanel(rect, "Divider", new Rect(-222f, -50f, 450f, 1.5f), 0f, new Color32(74, 91, 104, 255));

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        AddLabel(rect, font, "ARROWX  /  TARGET RANGE", new Rect(-222f, 68f, 288f, 22f), 15, Muted);
        title = AddLabel(rect, font, "Round", new Rect(-222f, 31f, 290f, 34f), 23, Ink);
        AddLabel(rect, font, "HITS", new Rect(-222f, 1f, 116f, 18f), 15, Muted);
        hitsValue = AddLabel(rect, font, "Hit Count", new Rect(-222f, -34f, 116f, 34f), 29, Ink);
        AddLabel(rect, font, "TIME LEFT", new Rect(-90f, 1f, 150f, 18f), 15, Muted);
        timerValue = AddLabel(rect, font, "Timer", new Rect(-90f, -34f, 150f, 34f), 29, Ink);
        AddLabel(scoreCard, font, "TOTAL SCORE", new Rect(-73.5f, 21f, 147f, 24f), 15, Gold, TextAnchor.MiddleCenter);
        scoreValue = AddLabel(scoreCard, font, "Score", new Rect(-73.5f, -40f, 147f, 62f), 48, Gold, TextAnchor.MiddleCenter);
        feedback = AddLabel(rect, font, "Hit Feedback", new Rect(-222f, -78f, 450f, 24f), 20, Gold);
        instructions = AddLabel(rect, font, "Instructions", new Rect(-222f, -101f, 450f, 21f), 16, Muted);
    }

    private RectTransform AddPanel(RectTransform parent, string name, Rect bounds, float radius, Color color)
    {
        var item = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)item.transform;
        Place(rect, parent, bounds);
        StylePanel(item.GetComponent<Image>(), radius, color);
        return rect;
    }

    private void StylePanel(Image image, float radius, Color color)
    {
        if (radius > 0f && roundedPanel != null)
        {
            image.sprite = roundedPanel;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = roundedPanel.border.x / radius;
        }

        image.color = color;
        image.raycastTarget = false;
    }

    private static Text AddLabel(RectTransform parent, Font font, string label, Rect bounds,
        int size, Color color, TextAnchor alignment = TextAnchor.MiddleLeft)
    {
        var item = new GameObject(label, typeof(RectTransform), typeof(Text));
        Place((RectTransform)item.transform, parent, bounds);
        var text = item.GetComponent<Text>();
        text.text = label;
        text.font = font;
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static void Place(RectTransform item, RectTransform parent, Rect bounds)
    {
        item.SetParent(parent, false);
        item.anchoredPosition = bounds.center;
        item.sizeDelta = bounds.size;
    }
}
