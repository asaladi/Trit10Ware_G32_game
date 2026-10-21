using System.Collections;
using UnityEngine;

public class LilyPad : SpecialItem
{
    public float maxHeightBonus = 0.5f;
    public float absoluteMaxHeightIncrease = 0.5f;
    public float duration = 5f;
    public float activationRadius = 1f;

    private static bool isInEffect = false;

    void OnEnable()
    {
        rockDrop.Bounced += OnRockBounced;
    }

    void OnDisable()
    {
        rockDrop.Bounced -= OnRockBounced;
    }

    private void OnRockBounced(Vector3 rockPosition)
    {
        Vector3 offset = rockPosition - transform.position;
        offset.y = 0f;

        if (offset.magnitude <= activationRadius)
            Trigger();
    }

    public override void Trigger()
    {
        if (isInEffect)
            return;

        isInEffect = true;
        rockDrop.absoluteMaxHeight += absoluteMaxHeightIncrease;
        rockDrop.currentMaxHeight = Mathf.Clamp(
            rockDrop.currentMaxHeight + maxHeightBonus,
            rockDrop.minimumMaxHeight,
            rockDrop.absoluteMaxHeight
        );

        // Run on rockDrop so the effect still ends if this pad is destroyed.
        rockDrop.StartCoroutine(CancelEffect(rockDrop, absoluteMaxHeightIncrease, duration));
    }

    private static IEnumerator CancelEffect(RockDrop rock, float increase, float duration)
    {
        yield return new WaitForSeconds(duration);
        rock.absoluteMaxHeight -= increase;

        isInEffect = false;
    }
}
