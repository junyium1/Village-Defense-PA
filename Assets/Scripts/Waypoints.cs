using UnityEngine;

public class Waypoints : MonoBehaviour
{
    public static Transform[] points;

    void Awake()
    {
        // Récupère automatiquement tous les enfants pour créer le chemin
        points = new Transform[transform.childCount];
        for (int i = 0; i < points.Length; i++)
        {
            points[i] = transform.GetChild(i);
        }
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red; 

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform currentPoint = transform.GetChild(i);
            
            
            Gizmos.DrawSphere(currentPoint.position, 0.5f); 

            
            if (i < transform.childCount - 1)
            {
                Transform nextPoint = transform.GetChild(i + 1);
                Gizmos.DrawLine(currentPoint.position, nextPoint.position);
            }
        }
    }
}