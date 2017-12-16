using UnityEngine;

public class DummyTagAlong : MonoBehaviour
{
    public float Radius = 1.0f;
    public float Speed = 2.0f;

    private Vector3 perfectPosition;
    private Vector3 targetPosition;
    private float initialDistanceToCamera;
    private bool displayRadius = false;
    private bool displayTargetPosition = false;

    void Start()
    {
        initialDistanceToCamera = Vector3.Distance(this.transform.position, Camera.main.transform.position);
    }

    void Update()
    {
        displayRadius = true; // display gizmos as soon as we hit play
        Vector3 currentPos = this.transform.position;
        perfectPosition = Camera.main.transform.position + Camera.main.transform.forward * initialDistanceToCamera;

        Vector3 offsetDir = currentPos - perfectPosition;
        displayTargetPosition = (offsetDir.magnitude > Radius);

        if (displayTargetPosition)
        {
            targetPosition = perfectPosition + offsetDir.normalized * Radius;
            this.transform.position = Vector3.Lerp(currentPos, targetPosition, Speed * Time.deltaTime);
        }
    }

    public void OnDrawGizmos()
    {
        Color oldColor = Gizmos.color;

        if (displayRadius)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(perfectPosition, Radius);
        }

        if (displayTargetPosition)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetPosition, 0.1f);
        }

        Gizmos.color = oldColor;
    }
}