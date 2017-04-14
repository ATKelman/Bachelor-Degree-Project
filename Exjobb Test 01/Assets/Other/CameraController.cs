using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    void Update()
    {
        float xAxisValue = Input.GetAxis("Horizontal");
        float yAxisValue = Input.GetAxis("Vertical");
        float zAxisValue = Input.GetAxis("Mouse ScrollWheel");
        if (Camera.current != null)
        {
            Camera.current.transform.Translate(new Vector3(xAxisValue, yAxisValue, zAxisValue));
        }
    }
}
