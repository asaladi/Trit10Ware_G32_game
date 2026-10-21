
using UnityEngine;

public class RockShadow : MonoBehaviour
{
    public Transform rock;
    public float waterHeight = -1f;

    public float minScale = 0.5f;
    public float maxScale = 1.0f;
    public float maxHeight = 3f;

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (rock == null) return;

        transform.position = new Vector3(
            rock.position.x,
            waterHeight + 0.02f,
            rock.position.z
        );

        float height = Mathf.Max(
            0f, rock.position.y - waterHeight
        );

        float t = Mathf.Clamp01(height / maxHeight);

        float scale = Mathf.Lerp(maxScale, minScale, t);

        transform.localScale = new Vector3(
            scale * 1.2f,
            scale * 0.65f,
            1f
        );

        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = Mathf.Lerp(0.7f, 0.15f, t);
            spriteRenderer.color = color;
        }
    }
}
