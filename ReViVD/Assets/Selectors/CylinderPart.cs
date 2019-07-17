using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;
using Unity.Collections;

namespace Revivd {

    public class CylinderPart : SelectorPart {
        private float initialLength;
        private float initialRadius;
        private Vector3 initialHandOffset = Vector3.zero;

        public float length = 5f;
        public float radius = 0.3f;
        public Vector3 handOffset = Vector3.zero;

        public override string GetLogString() {
            return "CYLINDER," + handOffset.x.ToString(Logger.nfi) + ','
                               + handOffset.y.ToString(Logger.nfi) + ','
                               + handOffset.z.ToString(Logger.nfi) + ','
                               + length.ToString(Logger.nfi) + ','
                               + radius.ToString(Logger.nfi);
        }

        protected override void CreatePrimitive() {
            initialLength = length;
            initialRadius = radius;
            initialHandOffset = handOffset;

            primitive = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        }

        protected override void UpdatePrimitive() {
            primitive.transform.localPosition = new Vector3(0, 0, length / 2) + handOffset;
            primitive.transform.localRotation = Quaternion.Euler(90, 0, 0);
            primitive.transform.localScale = new Vector3(radius, length / 2, radius);
            primitive.GetComponent<CapsuleCollider>().height = 2 + radius / length * 2;
        }


        protected override void UpdateManualModifications() {
            radius += (SelectorManager.Instance.InverseMode ? -1 : 1) * radius * SteamVR_ControllerManager.RightController.Shoulder * SelectorManager.Instance.creationGrowthCoefficient * Time.deltaTime;

            handOffset.x += Mathf.Max(Mathf.Abs(handOffset.x), SelectorManager.Instance.minCreationMovement) * SelectorManager.Instance.creationMovementCoefficient * SteamVR_ControllerManager.RightController.Joystick.x * Time.deltaTime;
            handOffset.y += Mathf.Max(Mathf.Abs(handOffset.y), SelectorManager.Instance.minCreationMovement) * SelectorManager.Instance.creationMovementCoefficient * SteamVR_ControllerManager.RightController.Joystick.y * Time.deltaTime;

            length += length * SteamVR_ControllerManager.LeftController.Joystick.y * SelectorManager.Instance.creationGrowthCoefficient * Time.deltaTime;

            if (SteamVR_ControllerManager.LeftController.gripped) {
                radius = initialRadius;
                length = initialLength;
            }

            if (SteamVR_ControllerManager.RightController.gripped) {
                handOffset = initialHandOffset;
            }

            if (SteamVR_ControllerManager.RightController.padPressed) {
                handOffset.z += (SteamVR_ControllerManager.RightController.Pad.y >= 0 ? 1 : -1) * Mathf.Max(Mathf.Abs(handOffset.z), SelectorManager.Instance.minCreationMovement) * SelectorManager.Instance.creationMovementCoefficient * Time.deltaTime;
            }

            if (SteamVR_ControllerManager.LeftController.padPressed) {
                length += (SteamVR_ControllerManager.LeftController.Pad.y >= 0 ? 1 : -1) * length * SelectorManager.Instance.creationGrowthCoefficient * Time.deltaTime;
            }
        }

        protected override void ParseRibbonsToCheck() {
            Vector3 saberStart = primitive.transform.position - primitive.transform.up * length / 2;
            Vector3 saberEnd = primitive.transform.position + primitive.transform.up * length / 2;
            
            foreach (Atom a in ribbonsToCheck) {
                if (!a.path.specialRadii.TryGetValue(a.indexInPath, out float radius))
                    radius = a.path.baseRadius;
                if (ClosestDistanceBetweenSegments(a.path.transform.TransformPoint(a.point), a.path.transform.TransformPoint(a.path.AtomsAsBase[a.indexInPath + 1].point), saberStart, saberEnd) < this.radius / 2 + radius) {
                    touchedRibbons.Add(a);
                }
            }
        }
        
        private float ClosestDistanceBetweenSegments(Vector3 a0, Vector3 a1, Vector3 b0, Vector3 b1) {
            //Issu de https://stackoverflow.com/questions/2824478/shortest-distance-between-two-line-segments

            float Determinant(Vector3 a, Vector3 b, Vector3 c) {
                return a.x * b.y * c.z + a.y * b.z * c.x + a.z * b.x * c.y - c.x * b.y * a.z - c.y * b.z * a.x - c.z * b.x * a.y;
            }

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
                        return (a0 - b0).magnitude;
                    }

                    return (a0 - b1).magnitude;
                }

                else if (d0 >= magA && magA <= d1) {
                    if (Mathf.Abs(d0) < Mathf.Abs(d1)) {
                        return (a1 - b0).magnitude;
                    }
                    
                    return (a1 - b1).magnitude;
                }

                return (((d0 * _A) + a0) - b0).magnitude;
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

            return (pA - pB).magnitude;
        }

    }

}