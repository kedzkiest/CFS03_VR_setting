using UnityEngine;
using StretchSense;

public class StretchSenseGripperConverter : MonoBehaviour
{
    [SerializeField] HandEngineClient handEngine;

    [Space]

    [SerializeField] Transform gripperRightBend1;
    [SerializeField] Transform gripperRightBend2;
    [SerializeField] Transform gripperLeftBend1;
    [SerializeField] Transform gripperLeftBend2;


    readonly float maxOpenStretchSenseMiddleBend1YRotation = 28.68f;
    readonly float maxOpenStretchSenseMiddleBend2YRotation = 14.79f;



    // Update is called once per frame
    void Update()
    {
        Vector3 gripperRightBend1EulerAngle = new Vector3(0,ConvertMiddleBend1(handEngine.R_MIDDLEBEND1),0);

        Vector3 gripperRightBend2EulerAngle = new Vector3(0,ConvertMiddleBend2(handEngine.R_MIDDLEBEND2),0);

        gripperRightBend1.localEulerAngles = gripperRightBend1EulerAngle;
        gripperRightBend2.localEulerAngles = gripperRightBend2EulerAngle;

        gripperLeftBend1.localEulerAngles = -gripperRightBend1EulerAngle;
        gripperLeftBend2.localEulerAngles = -gripperRightBend2EulerAngle;
    }

    float ConvertMiddleBend1(float stretchSenceMiddleBend1){
        return maxOpenStretchSenseMiddleBend1YRotation - stretchSenceMiddleBend1 * maxOpenStretchSenseMiddleBend1YRotation * 2;
    }

    float ConvertMiddleBend2(float stretchSenceMiddleBend2)
    {
        return maxOpenStretchSenseMiddleBend2YRotation - stretchSenceMiddleBend2 * (maxOpenStretchSenseMiddleBend2YRotation + 40);
    }
}
