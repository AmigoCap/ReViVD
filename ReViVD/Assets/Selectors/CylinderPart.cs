using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Revivd {

    public class CylinderPart : SelectorPart {

        public float initialLength = 5f;
        public float initialRadius = 0.3f;
        public Vector3 initialHandOffset = Vector3.zero;

        private float length;
        private float radius;
        private Vector3 handOffset;


        protected override void CreatePrimitive() {
            length = initialLength;
            radius = initialRadius;
            handOffset = initialHandOffset;
            primitive = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        }

        protected override void UpdatePrimitive() {
            primitive.transform.localPosition = new Vector3(0, 0, length / 2) + handOffset;
            primitive.transform.localRotation = Quaternion.Euler(90, 0, 0);
            primitive.transform.localScale = new Vector3(radius, length / 2, radius);
        }

        public override void ResetScale() {
            radius = initialRadius;
            length = initialLength;
            UpdatePrimitive();
        }

        protected override void UpdateManualModifications() {
            if (SelectorManager.Instance.InverseMode) {
                radius -= radius * SteamVR_ControllerManager.RightController.Shoulder * SelectorManager.Instance.creationGrowthCoefficient * Time.deltaTime;
            }
            else {
                radius += radius * SteamVR_ControllerManager.RightController.Shoulder * SelectorManager.Instance.creationGrowthCoefficient * Time.deltaTime;
            }

            handOffset.z += Mathf.Max(Mathf.Abs(handOffset.z), SelectorManager.Instance.minCreationMovement) * SelectorManager.Instance.creationMovementCoefficient * SteamVR_ControllerManager.RightController.Joystick.y * Time.deltaTime;

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
                            handOffset.y += Mathf.Max(Mathf.Abs(handOffset.y), SelectorManager.Instance.minCreationMovement) * SelectorManager.Instance.creationMovementCoefficient * Time.deltaTime;
                        }
                        else {
                            handOffset.y -= Mathf.Max(Mathf.Abs(handOffset.y), SelectorManager.Instance.minCreationMovement) * SelectorManager.Instance.creationMovementCoefficient * Time.deltaTime;
                        }
                    }
                }
            }

            length += length * SelectorManager.Instance.creationGrowthCoefficient * SteamVR_ControllerManager.LeftController.Joystick.y * Time.deltaTime;

        }

        protected override void ParseRibbonsToCheck() {
            Vector3 saberStart = primitive.transform.position - primitive.transform.up * length / 2;
            Vector3 saberEnd = primitive.transform.position + primitive.transform.up * length / 2;
            
            foreach (Atom a in checkedRibbons) {
                if (!a.path.specialRadii.TryGetValue(a.indexInPath, out float radius))
                    radius = a.path.baseRadius;
                if (ClosestDistanceBetweenSegments(a.path.transform.TransformPoint(a.point), a.path.transform.TransformPoint(a.path.AtomsAsBase[a.indexInPath + 1].point), saberStart, saberEnd) < this.radius / 2 + radius) {
                    touchedRibbons.Add(a);
                }
            }
        }

        private float Determinant(Vector3 a, Vector3 b, Vector3 c) {
            return a.x * b.y * c.z + a.y * b.z * c.x + a.z * b.x * c.y - c.x * b.y * a.z - c.y * b.z * a.x - c.z * b.x * a.y;
        }

        private float ClosestDistanceBetweenSegments(Vector3 a0, Vector3 a1, Vector3 b0, Vector3 b1) {
            //Issu de https://stackoverflow.com/questions/2824478/shortest-distance-between-two-line-segments

            Vector3 line1Closest;
            Vector3 line2Closest;
            float distance;

            var A = a1 - a0;
            var B = b1 - b0;
            float magA = A.magnitude;
            float magB = B.magnitude;

            var _A = A / magA;
            var _B = B / magB;

            var cross = Vector3.Cross(_A, _B);
            var denom = cross.magnitude * cross.magnitude;

            if (denom == 0) {
                var d0 = Vector3.Dot(_A, (b0 - a0));

                var d1 = Vector3.Dot(_A, (b1 - a0));

                if (d0 <= 0 && 0 >= d1) {
                    if (Mathf.Abs(d0) < Mathf.Abs(d1)) {
                        line1Closest = a0;
                        line2Closest = b0;
                        distance = (a0 - b0).magnitude;

                        return distance;
                    }
                    line1Closest = a0;
                    line2Closest = b1;
                    distance = (a0 - b1).magnitude;

                    return distance;
                }

                else if (d0 >= magA && magA <= d1) {
                    if (Mathf.Abs(d0) < Mathf.Abs(d1)) {
                        line1Closest = a1;
                        line2Closest = b0;
                        distance = (a1 - b0).magnitude;

                        return distance;
                    }
                    line1Closest = a1;
                    line2Closest = b1;
                    distance = (a1 - b1).magnitude;

                    return distance;
                }

                line1Closest = Vector3.zero;
                line2Closest = Vector3.zero;
                distance = (((d0 * _A) + a0) - b0).magnitude;
                return distance;
            }


            // Lines criss-cross: Calculate the projected closest points
            var t = (b0 - a0);
            var detA = Determinant(t, _B, cross);
            var detB = Determinant(t, _A, cross);

            var t0 = detA / denom;
            var t1 = detB / denom;

            var pA = a0 + (_A * t0); // Projected closest point on segment A
            var pB = b0 + (_B * t1); // Projected closest point on segment B


            // Clamp projections
            if (t0 < 0)
                pA = a0;
            else if (t0 > magA)
                pA = a1;

            if (t1 < 0)
                pB = b0;
            else if (t1 > magB)
                pB = b1;

            float dot;
            // Clamp projection A
            if (t0 < 0 || t0 > magA) {
                dot = Vector3.Dot(_B, (pA - b0));
                if (dot < 0)
                    dot = 0;
                else if (dot > magB)
                    dot = magB;
                pB = b0 + (_B * dot);
            }
            // Clamp projection B
            if (t1 < 0 || t1 > magB) {
                dot = Vector3.Dot(_A, (pB - a0));
                if (dot < 0)
                    dot = 0;
                else if (dot > magA)
                    dot = magA;
                pA = a0 + (_A * dot);
            }

            line1Closest = pA;
            line2Closest = pB;
            distance = (pA - pB).magnitude;
            return distance;
        }

    }

}