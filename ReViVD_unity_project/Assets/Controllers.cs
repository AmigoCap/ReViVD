using UnityEngine;
using UnityEngine.XR;

public class Controllers : MonoBehaviour
{
    GameObject leftHand;
    GameObject rightHand;

    // Start is called before the first frame update
    void Start()
    {
        leftHand = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftHand.transform.localScale = new Vector3(0.1f, 0.1f, 0.2f);
        leftHand.transform.parent = Camera.main.transform.parent;
        rightHand = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightHand.transform.localScale = new Vector3(0.1f, 0.1f, 0.2f);
        rightHand.transform.parent = Camera.main.transform.parent;
    }

    // Update is called once per frame
    void Update()
    {
        leftHand.transform.localRotation = InputTracking.GetLocalRotation(XRNode.LeftHand);
        leftHand.transform.localPosition = InputTracking.GetLocalPosition(XRNode.LeftHand) + new Vector3(0, 2, 0) - (0.1f * leftHand.transform.forward);
        rightHand.transform.localRotation = InputTracking.GetLocalRotation(XRNode.RightHand);
        rightHand.transform.localPosition = InputTracking.GetLocalPosition(XRNode.RightHand) + new Vector3(0, 2, 0) - (0.1f * rightHand.transform.forward);

    }
}

/* Notes :
 * InputTracking.GetLocalPosition(XRNode.Head) == Camera.main.transform.localPosition - new Vector3(0, 2, 0)
 * Ceci ne dépend pas de la position de cameraHolder.
 */
