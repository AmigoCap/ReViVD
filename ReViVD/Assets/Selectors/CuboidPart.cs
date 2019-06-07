using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Revivd {

    public class CuboidPart : SelectorPart {
        public Vector3 initialHandOffset = new Vector3(0f, 0f, 4f);
        public Vector3 initialSize = new Vector3(1f, 1f, 1f);
        private Vector3 handOffset;
        private Vector3 size;

        protected override void CreatePrimitive() {
            handOffset = initialHandOffset;
            size = initialSize;
            primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
        }
        
        protected override void UpdatePrimitive() {
            primitive.transform.localPosition = handOffset;
            primitive.transform.localRotation = Quaternion.identity;
            primitive.transform.localScale = size;
        }

        protected override void UpdateManualModifications() {

            if (SelectorManager.Instance.InverseMode) {
                size -= size * SteamVR_ControllerManager.RightController.Shoulder * SelectorManager.Instance.creationGrowthCoefficient * Time.deltaTime;
            }
            else {
                size += size * SteamVR_ControllerManager.RightController.Shoulder * SelectorManager.Instance.creationGrowthCoefficient * Time.deltaTime;
            }

            handOffset.z += Mathf.Max(Mathf.Abs(handOffset.z), SelectorManager.Instance.minCreationMovement) * SelectorManager.Instance.creationMovementCoefficient * SteamVR_ControllerManager.RightController.Joystick.y * Time.deltaTime;

            if (SelectorManager.Instance.InverseMode && SteamVR_ControllerManager.LeftController.padPressed) {
                size = initialSize;
            }

            if (SelectorManager.Instance.InverseMode && SteamVR_ControllerManager.RightController.padPressed) {
                handOffset = initialHandOffset;
            }

            if (SteamVR_ControllerManager.RightController.padPressed) {
                if (SteamVR_ControllerManager.RightController.Pad.x >= 0) {
                    if (Mathf.Abs(SteamVR_ControllerManager.RightController.Pad.y) < 0.7071) {
                        handOffset.x += Mathf.Max(Mathf.Abs(handOffset.x), SelectorManager.Instance.minCreationMovement) * SelectorManager.Instance.creationMovementCoefficient * Time.deltaTime;
                    }
                    else {
                        if (SteamVR_ControllerManager.RightController.Pad.y >= 0) {
                            handOffset.y += Mathf.Max(Mathf.Abs(handOffset.y), SelectorManager.Instance.minCreationMovement) * SelectorManager.Instance.creationMovementCoefficient * Time.deltaTime;
                        }
                        else {
                            handOffset.y -= Mathf.Max(Mathf.Abs(handOffset.y), SelectorManager.Instance.minCreationMovement) * SelectorManager.Instance.creationMovementCoefficient * Time.deltaTime;
                        }
                    }
                }
                else {
                    if (Mathf.Abs(SteamVR_ControllerManager.RightController.Pad.y) < 0.7071) {
                        handOffset.x -= Mathf.Max(Mathf.Abs(handOffset.x), SelectorManager.Instance.minCreationMovement) * SelectorManager.Instance.creationMovementCoefficient * Time.deltaTime;
                    }
                    else {
                        if (SteamVR_ControllerManager.RightController.Pad.y >= 0) {
                            handOffset.y += Mathf.Max(Mathf.Abs(handOffset.y), SelectorManager.Instance.minCreationMovement) * SelectorManager.Instance.creationMovementCoefficient  * Time.deltaTime;
                        }
                        else {
                            handOffset.y -= Mathf.Max(Mathf.Abs(handOffset.y), SelectorManager.Instance.minCreationMovement) * SelectorManager.Instance.creationMovementCoefficient * Time.deltaTime;
                        }
                    }
                }
            }

            size.z += size.z * SelectorManager.Instance.creationGrowthCoefficient * SteamVR_ControllerManager.LeftController.Joystick.y * Time.deltaTime;

            if (SteamVR_ControllerManager.LeftController.padPressed) {
                if (SteamVR_ControllerManager.LeftController.Pad.x >= 0) {
                    if (Mathf.Abs(SteamVR_ControllerManager.LeftController.Pad.y) < 0.7071) {
                        size.x += size.x * SelectorManager.Instance.creationMovementCoefficient * Time.deltaTime;
                    }
                    else {
                        if (SteamVR_ControllerManager.LeftController.Pad.y >= 0) {
                            size.y += size.y * SelectorManager.Instance.creationMovementCoefficient * Time.deltaTime;
                        }
                        else {
                            size.y -= size.y * SelectorManager.Instance.creationMovementCoefficient * Time.deltaTime;
                        }
                    }
                }
                else {
                    if (Mathf.Abs(SteamVR_ControllerManager.LeftController.Pad.y) < 0.7071) {
                        size.x -= size.x * SelectorManager.Instance.creationMovementCoefficient * Time.deltaTime;
                    }
                    else {
                        if (SteamVR_ControllerManager.LeftController.Pad.y >= 0) {
                            size.y += size.y * SelectorManager.Instance.creationMovementCoefficient * Time.deltaTime;
                        }
                        else {
                            size.y -= size.y * SelectorManager.Instance.creationMovementCoefficient * Time.deltaTime;
                        }
                    }
                }
            }

        }

        protected override void ParseRibbonsToCheck() {
            foreach (Atom a in checkedRibbons) {
                if (CuboidTouchesRibbon(a)) {
                    touchedRibbons.Add(a);
                }
            }
        }

        private bool CuboidTouchesRibbon(Atom a) {

            if (!a.path.specialRadii.TryGetValue(a.indexInPath, out float radius))
                radius = a.path.baseRadius;

            Vector3 extendedScale = primitive.transform.localScale / 2;// + new Vector3(radius, radius, radius) ;

            //Segment in the world space coordinates
            Vector3 b = a.path.transform.TransformPoint(a.point);  //Coordinates World Space
            Vector3 c = a.path.transform.TransformPoint(a.path.AtomsAsBase[a.indexInPath + 1].point); //Coordinates World Space
            //and then in the right hand space coordinates
            Vector3 bc = primitive.transform.parent.InverseTransformPoint(b);
            Vector3 cc = primitive.transform.parent.InverseTransformPoint(c);

            Vector3 mc = (bc + cc) / 2; //midpoint vector of the segment
            Vector3 l = bc - mc; 
            Vector3 lext = new Vector3(Mathf.Abs(l.x), Mathf.Abs(l.y), Mathf.Abs(l.z));//extent vector of the segment

            mc = primitive.transform.localPosition - mc;

            //Separating axis test: axis = separating axis? 
            if (Mathf.Abs(mc.x) >  extendedScale.x + radius + lext.x) return false;
            if (Mathf.Abs(mc.y) >  extendedScale.y + radius + lext.y) return false;
            if (Mathf.Abs(mc.z) >  extendedScale.z + radius + lext.z) return false;

            bool first = true;
            bool second = true;

            if (Mathf.Abs(mc.y * l.z - mc.z * l.y) > ((extendedScale.y + radius) * lext.z + extendedScale.z * lext.y)) first = false;
            if (Mathf.Abs(mc.y * l.z - mc.z * l.y) > (extendedScale.y * lext.z + (extendedScale.z + radius) * lext.y)) second = false;

            if (!first & !second) return false;
            first = true;
            second = true;

            if (Mathf.Abs(mc.x * l.z - mc.z * l.x) > ((extendedScale.x + radius) * lext.z + extendedScale.z * lext.x)) first = false;
            if (Mathf.Abs(mc.x * l.z - mc.z * l.x) > (extendedScale.x * lext.z + (extendedScale.z + radius) * lext.x)) second = false;

            if (!first & !second) return false;
            first = true;
            second = true;

            if (Mathf.Abs(mc.x * l.y - mc.y * l.x) > ((extendedScale.x + radius) * lext.y + extendedScale.y * lext.x)) first = false;
            if (Mathf.Abs(mc.x * l.y - mc.y * l.x) > (extendedScale.x * lext.y + (extendedScale.y + radius) * lext.x)) second = false;

            if (!first & !second) return false;

            return true;
        }

    }
}