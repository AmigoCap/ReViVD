using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Revivd {

    public class Movement : MonoBehaviour {
        private static Movement _instance;

        public static Movement Instance { get { return _instance; } }

        public bool FullControlMode = false;

        public float horizontalSensitivity = 150;
        public float verticalSensitivity = 150;
        public float trimSensitivity = 0.3f;
        public float maxTrimVelocity = 150;
        public float baseSpeed = 20;
        public float speedExponent = 1.5f;
        public bool invertVerticalControl = false;
        public bool doJoystickControls = true;


        Transform camTrans;
        Vector2 oldTouchPos = Vector2.zero;
        float trimToDo = 0;

        private void Awake() {
            if (_instance != null && _instance != this) {
                Destroy(_instance.gameObject);
            }
            _instance = this;
        }

        // Use this for initialization
        void Start() {
            camTrans = Camera.main.transform;
        }

        void PrintVect(Vector3 vect) {
            Debug.Log(vect.x.ToString() + ' ' + vect.y.ToString() + ' ' + vect.z.ToString());
        }

        private void AdjustSpeed(SteamVR_TrackedController sender) {
            if (!Visualization.Instance.Loaded || SelectorManager.Instance == null || SelectorManager.Instance.CurrentControlMode != SelectorManager.ControlMode.SelectionMode)
                return;

            if (SelectorManager.Instance.InverseMode) {
                baseSpeed /= speedExponent;
                Logger.Instance?.LogEvent("-SPEED," + baseSpeed.ToString(Logger.nfi));
            }
            else {
                baseSpeed *= speedExponent;
                Logger.Instance?.LogEvent("+SPEED," + baseSpeed.ToString(Logger.nfi));
            }
        }

        private void OnEnable() {
            SteamVR_ControllerManager.LeftController.Gripped += AdjustSpeed;
        }

        private void OnDisable() {
            SteamVR_ControllerManager.LeftController.Gripped -= AdjustSpeed;

        }

        // Update is called once per frame
        void Update() {
            if (!Visualization.Instance.Loaded)
                return;

            if (SelectorManager.Instance.CurrentControlMode == SelectorManager.ControlMode.SelectionMode) {
                Vector3 camRot;
                Vector3 camStr;

                if (FullControlMode) {
                    camRot = Time.deltaTime * (horizontalSensitivity * SteamVR_ControllerManager.RightController.Joystick.x * camTrans.up + verticalSensitivity * (invertVerticalControl ? 1 : -1) * SteamVR_ControllerManager.RightController.Joystick.y * camTrans.right);

                    Vector2 touchPos = new Vector2(SteamVR_ControllerManager.RightController.Pad.x, -SteamVR_ControllerManager.RightController.Pad.y);
                    if (!oldTouchPos.Equals(Vector2.zero) && !touchPos.Equals(Vector2.zero)) {
                        trimToDo -= trimSensitivity * Vector2.SignedAngle(oldTouchPos, touchPos);
                    }
                    oldTouchPos.Set(touchPos.x, touchPos.y);
                    if (trimToDo != 0) {
                        float step = Mathf.Max(Mathf.Min(trimToDo, maxTrimVelocity * Time.deltaTime), -maxTrimVelocity * Time.deltaTime);
                        camRot += step * camTrans.forward;
                        trimToDo -= step;
                    }

                    camStr = Time.deltaTime * baseSpeed * (SteamVR_ControllerManager.LeftController.Joystick.y * camTrans.forward + SteamVR_ControllerManager.LeftController.Joystick.x * camTrans.right);
                }
                else {
                    camRot = Time.deltaTime * (horizontalSensitivity * SteamVR_ControllerManager.RightController.Joystick.x * Vector3.up);
                    camStr = Time.deltaTime * baseSpeed * (SteamVR_ControllerManager.LeftController.Joystick.y * camTrans.forward + SteamVR_ControllerManager.LeftController.Joystick.x * camTrans.right + SteamVR_ControllerManager.RightController.Joystick.y * Vector3.up);
                }


                this.transform.Translate(camStr + Vector3.Cross(camTrans.position - transform.position, camRot * 0.0174533f), Space.World);
                this.transform.Rotate(camRot, Space.World);
            }
        }
    }

}