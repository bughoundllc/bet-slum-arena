using UnityEngine;

public class Spinner : MonoBehaviour
{
    public float RotationSpeed = 2f;

    // Update is called once per frame
    void Update()
    {
        transform.RotateAround(Vector3.zero, Vector3.up, RotationSpeed * Time.deltaTime);
    }
}
