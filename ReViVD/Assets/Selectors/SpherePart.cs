using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;

namespace Revivd {

    public class SpherePart : SelectorPart {

        private float initialRadius;
        private Vector3 initialHandOffset = Vector3.zero;

        public float radius = 0.3f;
        public Vector3 handOffset = Vector3.zero;

        public override string GetLogString() {
            return "SPHERE," + handOffset.x.ToString(Logger.nfi) + ','
                             + handOffset.y.ToString(Logger.nfi) + ','
                             + handOffset.z.ToString(Logger.nfi) + ','
                             + radius.ToString(Logger.nfi);
        }

        protected override void CreatePrimitive() {
            initialRadius = radius;
            initialHandOffset = handOffset;
            primitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        }

        protected override void UpdatePrimitive() {
            primitive.transform.localPosition = handOffset;
            primitive.transform.localRotation = Quaternion.identity;
            primitive.transform.localScale = new Vector3(radius * 2, radius * 2, radius * 2);
        }

        protected override void UpdateManualModifications() {
            radius += (SelectorManager.Instance.InverseMode ? -1 : 1) * radius * SteamVR_ControllerManager.RightController.Shoulder * SelectorManager.Instance.creationGrowthCoefficient * Time.deltaTime;

            handOffset.z += Mathf.Max(Mathf.Abs(handOffset.z), SelectorManager.Instance.minCreationMovement) * SelectorManager.Instance.creationMovementCoefficient * (SteamVR_ControllerManager.LeftController.Joystick.y + SteamVR_ControllerManager.RightController.Joystick.y) * Time.deltaTime;

            if (SteamVR_ControllerManager.LeftController.gripped) {
                radius = initialRadius;
            }

            if (SteamVR_ControllerManager.RightController.gripped) {
                handOffset = initialHandOffset;
            }
        }

        protected override void ParseRibbonsToCheck() {
            Vector3 sphereCenter = primitive.transform.position;

            foreach (Atom a in ribbonsToCheck) {
                if (!a.path.specialRadii.TryGetValue(a.indexInPath, out float ribbonRadius))
                    ribbonRadius = a.path.baseRadius;
                if (DistancePointSegment(sphereCenter, a.path.transform.TransformPoint(a.point), a.path.transform.TransformPoint(a.path.AtomsAsBase[a.indexInPath + 1].point)) < radius + ribbonRadius) {
                    touchedRibbons.Add(a);
                }
            }
        }

        private float DistancePointSegment(Vector3 point, Vector3 a, Vector3 b) {
            if (Vector3.Dot(point - a, b - a) <= 0) {
                return (point - a).magnitude;
            }
            if (Vector3.Dot(point - b, a - b) <= 0) {
                return (point - b).magnitude;
            }
            return Vector3.Cross(b - a, point - a).magnitude / (b - a).magnitude;
        }
    }

}