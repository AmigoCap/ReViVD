using UnityEngine;
using UnityEngine.XR;

public class Controllers : MonoBehaviour
{
    GameObject leftHand;
    GameObject rightHand;

    private void Awake() {
        leftHand = new GameObject("leftHand");
        rightHand = new GameObject("rightHand");
    }

    // Start is called before the first frame update
    void Start()
    {
        leftHand.transform.parent = Camera.main.transform.parent;
        rightHand.transform.parent = Camera.main.transform.parent;
        GameObject leftCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftCube.transform.parent = leftHand.transform;
        leftCube.transform.localPosition = leftCube.transform.InverseTransformDirection(leftHand.transform.forward) * -0.1f;
        leftCube.transform.localScale = new Vector3(0.1f, 0.1f, 0.2f);
        GameObject rightCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightCube.transform.parent = rightHand.transform;
        rightCube.transform.localPosition = rightCube.transform.InverseTransformDirection(rightHand.transform.forward) * -0.1f;
        rightCube.transform.localScale = new Vector3(0.1f, 0.1f, 0.2f);
    }

    // Update is called once per frame
    void Update()
    {
        leftHand.transform.localRotation = InputTracking.GetLocalRotation(XRNode.LeftHand);
        leftHand.transform.localPosition = InputTracking.GetLocalPosition(XRNode.LeftHand) + new Vector3(0, 2, 0);
        rightHand.transform.localRotation = InputTracking.GetLocalRotation(XRNode.RightHand);
        rightHand.transform.localPosition = InputTracking.GetLocalPosition(XRNode.RightHand) + new Vector3(0, 2, 0);
    }
}

/* Notes :
 * InputTracking.GetLocalPosition(XRNode.Head) == Camera.main.transform.localPosition - new Vector3(0, 2, 0)
 * Ceci ne dépend pas de la position de cameraHolder.
 */
