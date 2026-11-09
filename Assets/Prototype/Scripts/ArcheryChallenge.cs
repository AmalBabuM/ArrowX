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

    [SerializeField] private MovingTarget popUpTarget;
    [SerializeField] private Vector3[] spawnOffsets;

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
    private Text counters;
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
        title.text = $"ROUND {stage + 1}/{Stages.Length}  |  {Stages[stage].Name.ToUpperInvariant()}";
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
        RefreshCounters(seconds);
    }

    private void RefreshCounters(int seconds)
    {
        displayedSeconds = seconds;
        counters.text = $"HITS {hits}/{HitsPerRound}    SCORE {Score}    TIME {seconds:00}s";
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
        feedback = AddLine(rect, font, "Hit Feedback", -18f, 22, new Color(0.48f, 0.91f, 0.67f));
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
