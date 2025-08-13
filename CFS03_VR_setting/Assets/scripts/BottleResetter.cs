using UnityEngine;

public class BottleResetter : MonoBehaviour
{
    [SerializeField] Transform bottleTransform;
    Vector3 initialPosition;
    Vector3 initialRotation;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        initialPosition = bottleTransform.position;
        initialRotation = bottleTransform.localEulerAngles;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            bottleTransform.position = initialPosition;
            bottleTransform.localEulerAngles = initialRotation;
        }
    }
}
