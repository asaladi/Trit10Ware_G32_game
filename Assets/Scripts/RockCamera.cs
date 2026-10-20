
using UnityEngine;

public class RockCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform rock;

    [Header("Camera Position")]
    public float cameraDistance = -25f;
    public float cameraHeight = 8f;

    [Header("Vertical Camera Movement")]
    public float followSmoothness = 3f;
    public float verticalFollowAmount = 0.2f;

    [Header("Dynamic Zoom")]
    public float baseOrthographicSize = 9f;
    public float zoomOutPerHeight = 0.25f;
    public float zoomSmoothness = 3f;

    private Camera cam;

    private float startingRockY;
    private float lockedCameraX;
    private Quaternion lockedRotation;

    void Start()
    {
        cam = GetComponent<Camera>();

        if (rock == null)
        {
            Debug.LogError("RockCamera: Assign the Rock!");
            enabled = false;
            return;
        }

        startingRockY = rock.position.y;

        lockedCameraX = transform.position.x;
        lockedRotation = transform.rotation;

        // Set the camera's starting height and distance.
        transform.position = new Vector3(
            lockedCameraX,
            cameraHeight,
            cameraDistance
        );

        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = baseOrthographicSize;
        }
    }

    void LateUpdate()
    {
        float heightDifference =
            rock.position.y - startingRockY;

        // Follow the rock vertically while staying higher.
        Vector3 targetPosition = new Vector3(
            lockedCameraX,
            cameraHeight +
                heightDifference * verticalFollowAmount,
            cameraDistance
        );

        float followT = 1f - Mathf.Exp(
            -followSmoothness * Time.deltaTime
        );

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            followT
        );

        // Zoom out as the rock jumps higher.
        float targetZoom =
            baseOrthographicSize +
            Mathf.Max(0f, heightDifference) * zoomOutPerHeight;

        float zoomT = 1f - Mathf.Exp(
            -zoomSmoothness * Time.deltaTime
        );

        cam.orthographicSize = Mathf.Lerp(
            cam.orthographicSize,
            targetZoom,
            zoomT
        );

        // Keep camera rotation fixed.
        transform.rotation = lockedRotation;
    }
}
