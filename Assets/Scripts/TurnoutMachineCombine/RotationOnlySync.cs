using UnityEngine;

public class RotationOnlySync : MonoBehaviour
{
    [SerializeField] private Transform target;
    
    private void Update()
    {
        if (target != null)
        {
            transform.rotation = target.rotation;
        }
    }
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    
    public void RemoveTarget()
    {
        target = null;
    }
}