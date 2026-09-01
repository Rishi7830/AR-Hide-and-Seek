using UnityEngine;

public class ColorSphere : MonoBehaviour
{
    public Color sphereColor = Color.white;

    private void OnDrawGizmosSelected()
    {
        // Visual helper in Editor
        Gizmos.color = sphereColor;
        Gizmos.DrawSphere(transform.position, 0.02f);
    }
}