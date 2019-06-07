﻿using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;

namespace Revivd {

    public class SpherePart : SelectorPart {

        public float radius = 0.5f;
        public Vector3 handOffset = new Vector3(0f, 0f, 2.5f);

        protected override void CreatePrimitive() {
            primitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        }

        protected override void UpdatePrimitive() {
            primitive.transform.localPosition = handOffset;
            primitive.transform.localRotation = Quaternion.identity;
            primitive.transform.localScale = new Vector3(radius * 2, radius * 2, radius * 2);
        }

        protected override void UpdateManualModifications() {
            radius += radius * SteamVR_ControllerManager.RightController.Shoulder * SelectorManager.Instance.creationGrowthCoefficient * Time.deltaTime;
            radius -= radius * SteamVR_ControllerManager.LeftController.Shoulder * SelectorManager.Instance.creationGrowthCoefficient * Time.deltaTime;


            handOffset.z += Mathf.Max(Mathf.Abs(handOffset.z), SelectorManager.Instance.minCreationMovement) * SelectorManager.Instance.creationMovementCoefficient * SteamVR_ControllerManager.RightController.Joystick.y * Time.deltaTime;

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