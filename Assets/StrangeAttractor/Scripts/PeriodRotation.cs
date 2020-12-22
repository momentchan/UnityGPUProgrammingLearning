using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PeriodRotation : MonoBehaviour
{
    [SerializeField] protected Vector3 rotateSpeed;
    
    void Update()
    {
        transform.Rotate(Vector3.right * rotateSpeed.x, Space.Self);
        transform.Rotate(Vector3.up * rotateSpeed.y, Space.Self);
        transform.Rotate(Vector3.forward * rotateSpeed.z, Space.Self);
    }
}
