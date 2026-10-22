using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

public class RockDrop : MonoBehaviour
{
    [Header("References")]
    public Transform water;
    public Camera gameCamera;

    [Header("Starting Position")]
    public float startHeight = 6f;
    public float initialFallSpeed = 1f;
    public float contactOffset = 0.74f;

    [Header("Mouse Z Movement")]
    public float minZ = -4f;
    public float maxZ = 4f;
    public float zFollowSpeed = 8f;

    [Header("Gravity Settings")]
    public float riseGravity = 8f;
    public float peakGravity = 2f;
    public float fallGravity = 10f;
    public float peakThreshold = 2f;

    [Header("Difficulty")]
    public float speedIncreasePerBounce = 0.05f;
    public float maxSpeedMultiplier = 3f;

    [Header("Bounce Heights")]
    public float startingMaxHeight = 5f;
    public float minimumMaxHeight = 1f;
    public float absoluteMaxHeight = 8f;
    public float heightLossPerBounce = 0.2f;
    public float perfectHeightBonus = 0.5f;

    [Header("Bounce Quality")]
    public float greatHeightMultiplier = 0.75f;
    public float missHeightMultiplier = 0.30f;

    [Header("Timing")]
    public float perfectWindow = 0.12f;
    public float greatWindow = 0.30f;

    [Header("Feedback")]
    public float feedbackDuration = 0.7f;
    /// In order from perfect to bad.
    public Sprite[] feedbackSprites;
    public Image feedbackImage;

    [Header("Timing Bar UI")]
    public float barWidth = 400f;
    public float barHeight = 24f;
    public float barBottomMargin = 65f;
    public float barDisplayWindow = 1f;

    private float verticalSpeed;
    private float contactY;
    public float currentMaxHeight;
    public float speedMultiplier = 1f;
    private float lockedX;

    private int totalBounces;

    private enum TimingResult
    {
        None,
        Perfect,
        Great,
        Miss
    }

    private TimingResult recordedTiming = TimingResult.None;
    public bool hasClicked;

    /// <summary>
    /// Fired when the rock touches the water, before the bounce height
    /// is calculated. Passes the rock's position at impact.
    /// </summary>
    public event System.Action<Vector3> Bounced;

    private Color feedbackColor = Color.white;

    private Texture2D whiteTexture;

    void Start()
    {
        lockedX = transform.position.x;

        if (gameCamera == null)
            gameCamera = Camera.main;

        whiteTexture = Texture2D.whiteTexture;

        RestartGame();
    }

    void Update()
    {
        MoveWithMouse();

        bool clicked = Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame;

        if (clicked && !hasClicked)
        {
            hasClicked = true;

            float timeUntilImpact = GetTimeUntilImpact();

            if (verticalSpeed >= 0f)
                recordedTiming = TimingResult.Miss;
            else if (timeUntilImpact <= perfectWindow)
                recordedTiming = TimingResult.Perfect;
            else if (timeUntilImpact <= greatWindow)
                recordedTiming = TimingResult.Great;
            else
                recordedTiming = TimingResult.Miss;
        }

        float gravity = GetCurrentGravity(verticalSpeed);

        verticalSpeed -= gravity * Time.deltaTime;

        float nextY = transform.position.y +
            verticalSpeed * Time.deltaTime;

        if (verticalSpeed < 0f && nextY <= contactY)
        {
            Vector3 pos = transform.position;
            pos.y = contactY;
            transform.position = pos;

            ApplyBounce();
            return;
        }

        Vector3 position = transform.position;
        position.y = nextY;
        transform.position = position;
    }

    void MoveWithMouse()
    {
        if (Mouse.current == null || gameCamera == null)
            return;

        float mouseY = Mouse.current.position.ReadValue().y;

        float normalizedY = Mathf.Clamp01(
            mouseY / Screen.height
        );

        float targetZ = Mathf.Lerp(minZ, maxZ, normalizedY);

        Vector3 pos = transform.position;

        pos.x = lockedX;

        pos.z = Mathf.Lerp(
            pos.z,
            targetZ,
            zFollowSpeed * Time.deltaTime
        );

        transform.position = pos;
    }

    float GetCurrentGravity(float speed)
    {
        float baseGravity;

        if (Mathf.Abs(speed) < peakThreshold)
            baseGravity = peakGravity;
        else if (speed > 0f)
            baseGravity = riseGravity;
        else
            baseGravity = fallGravity;

        return baseGravity * speedMultiplier;
    }

    public float GetTimeUntilImpact()
    {
        float predictedY = transform.position.y;
        float predictedSpeed = verticalSpeed;

        const float step = 0.01f;
        const float maxTime = 15f;

        for (float t = 0f; t < maxTime; t += step)
        {
            float gravity = GetCurrentGravity(predictedSpeed);

            predictedSpeed -= gravity * step;
            predictedY += predictedSpeed * step;

            if (predictedY <= contactY && predictedSpeed < 0f)
                return t + step;
        }

        return maxTime;
    }

    void ApplyBounce()
    {
        totalBounces++;

        speedMultiplier = Mathf.Min(
            maxSpeedMultiplier,
            1f + totalBounces * speedIncreasePerBounce
        );

        Bounced?.Invoke(transform.position);

        // No clamping from above in case of a bonus for maxHeight.
        currentMaxHeight = Mathf.Max(
            currentMaxHeight - heightLossPerBounce,
            minimumMaxHeight
        );

        float targetHeight = currentMaxHeight;

        if (recordedTiming == TimingResult.Perfect)
        {
            // If maxHeight is already above limit, don't clamp down to keep
            // the bonus maxHeight.
            currentMaxHeight = Mathf.Max(
                currentMaxHeight,
                Mathf.Min(
                    currentMaxHeight + perfectHeightBonus,
                    absoluteMaxHeight
                )
            );

            targetHeight = currentMaxHeight;
        }
        else if (recordedTiming == TimingResult.Great)
        {
            targetHeight =
                currentMaxHeight * greatHeightMultiplier;
        }
        else
        {
            targetHeight =
                currentMaxHeight * missHeightMultiplier;
        }

        ShowFeedback();

        verticalSpeed = CalculateBounceVelocity(targetHeight);

        recordedTiming = TimingResult.None;
        hasClicked = false;
    }

    float CalculateBounceVelocity(float targetHeight)
    {
        float low = 0f;
        float high = 30f;

        for (int i = 0; i < 20; i++)
        {
            float mid = (low + high) / 2f;

            if (SimulateBounceHeight(mid) < targetHeight)
                low = mid;
            else
                high = mid;
        }

        return (low + high) / 2f;
    }

    float SimulateBounceHeight(float startSpeed)
    {
        float height = 0f;
        float speed = startSpeed;

        const float step = 0.005f;

        for (int i = 0; i < 4000; i++)
        {
            speed -= GetCurrentGravity(speed) * step;

            if (speed <= 0f)
                break;

            height += speed * step;
        }

        return height;
    }

    float SimulateBounceDuration(float startSpeed)
    {
        float height = 0f;
        float speed = startSpeed;

        const float step = 0.005f;

        for (int i = 0; i < 4000; i++)
        {
            speed -= GetCurrentGravity(speed) * step;

            if (height <= 0f)
                return i * step;

            height += speed * step;
        }

        return 1f;
    }

    void ShowFeedback()
    {
        feedbackImage.sprite = feedbackSprites[(int) recordedTiming];
        feedbackImage.enabled = true;
        StartCoroutine(HideFeedback(feedbackDuration));
    }

    IEnumerator HideFeedback(float duration) {
        yield return new WaitForSeconds(duration);
        feedbackImage.enabled = false;
    }

    void RestartGame()
    {
        contactY = (water != null ? water.position.y : -1f)
            + contactOffset;

        Vector3 pos = transform.position;
        pos.x = lockedX;
        pos.y = contactY + startHeight;
        transform.position = pos;

        verticalSpeed = -initialFallSpeed;
        currentMaxHeight = startingMaxHeight;
        speedMultiplier = 1f;

        totalBounces = 0;
        recordedTiming = TimingResult.None;
        hasClicked = false;
    }

    void DrawRect(Rect rect, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(rect, whiteTexture);
        GUI.color = Color.white;
    }

    void DrawTimingBar()
    {
        float timeUntilImpact = GetTimeUntilImpact();

        float x = (Screen.width - barWidth) / 2f;
        float y = Screen.height - barBottomMargin - barHeight;

        float displayWindow = Mathf.Max(
            barDisplayWindow,
            greatWindow + 0.01f
        );

        // Background
        DrawRect(
            new Rect(x, y, barWidth, barHeight),
            new Color(0.15f, 0.18f, 0.22f, 0.9f)
        );

        // Great zone
        float greatStart =
            1f - greatWindow / displayWindow;

        float perfectStart =
            1f - perfectWindow / displayWindow;

        DrawRect(
            new Rect(
                x + greatStart * barWidth,
                y,
                (perfectStart - greatStart) * barWidth,
                barHeight
            ),
            new Color(1f, 0.8f, 0.1f)
        );

        // Perfect zone
        DrawRect(
            new Rect(
                x + perfectStart * barWidth,
                y,
                (1f - perfectStart) * barWidth,
                barHeight
            ),
            new Color(0.2f, 0.9f, 0.3f)
        );

        // Moving indicator
        float progress = Mathf.Clamp01(
            1f - timeUntilImpact / displayWindow
        );

        float indicatorX = x + progress * barWidth;

        DrawRect(
            new Rect(
                indicatorX - 2f,
                y - 7f,
                4f,
                barHeight + 14f
            ),
            Color.white
        );

        // Label
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 15;
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.normal.textColor = Color.white;

        GUI.Label(
            new Rect(x, y - 30f, barWidth, 25f),
            hasClicked ? "TIMING LOCKED" : "CLICK BEFORE IMPACT",
            labelStyle
        );
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 22;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.black;

        GUI.Label(
            new Rect(20, 20, 350, 35),
            "Bounces: " + totalBounces,
            style
        );

        GUI.Label(
            new Rect(20, 55, 350, 35),
            "Max Height: " + currentMaxHeight.ToString("F1"),
            style
        );

        GUI.Label(
            new Rect(20, 90, 350, 35),
            "Speed: " + speedMultiplier.ToString("F1") + "x",
            style
        );

        DrawTimingBar();
    }
}
