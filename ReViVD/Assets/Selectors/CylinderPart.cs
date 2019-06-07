using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;
using Unity.Collections;

namespace Revivd {

    public class CylinderPart : SelectorPart {

        public float length = 5f;
        public float radius = 0.3f;
        public Vector3 handOffset = Vector3.zero;


        protected override void CreatePrimitive() {
            primitive = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        }

        protected override void UpdatePrimitive() {
            primitive.transform.localPosition = new Vector3(0, 0, length / 2) + handOffset;
            primitive.transform.localRotation = Quaternion.Euler(90, 0, 0);
            primitive.transform.localScale = new Vector3(radius, length / 2, radius);
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

            length += length * SelectorManager.Instance.creationGrowthCoefficient * SteamVR_ControllerManager.LeftController.Joystick.y * Time.deltaTime;

        }

        DistanceToSaberJob job;
        JobHandle handle;

        protected override void StartCheckingRibbons() {
            NativeArray<Vector3> APoints = new NativeArray<Vector3>(ribbonsToCheck.Count, Allocator.TempJob);
            NativeArray<Vector3> BPoints = new NativeArray<Vector3>(ribbonsToCheck.Count, Allocator.TempJob);
            var it = ribbonsToCheck.GetEnumerator();
            for (int i = 0; i < APoints.Length; i++) {
                it.MoveNext();
                APoints[i] = it.Current.path.transform.TransformPoint(it.Current.point);
                BPoints[i] = it.Current.path.transform.TransformPoint(it.Current.path.AtomsAsBase[it.Current.indexInPath].point);
            }

            Vector3 saberStart = primitive.transform.position - primitive.transform.up * length / 2;
            Vector3 saberEnd = primitive.transform.position + primitive.transform.up * length / 2;

            job = new DistanceToSaberJob() {
                APoints = APoints,
                BPoints = BPoints,
                distances = new NativeArray<float>(APoints.Length, Allocator.TempJob),
                saberStart = saberStart,
                saberEnd = saberEnd,
                saberDiffMagnitude = (saberStart - saberEnd).magnitude,
                saberDiffNormalized = (saberStart - saberEnd).normalized
            };

            handle = job.Schedule(APoints.Length, 32);

            return;
        }

        protected override void FinishCheckingRibbons() {
            handle.Complete();
            job.APoints.Dispose();
            job.BPoints.Dispose();
            var it = job.distances.GetEnumerator();
            foreach (Atom a in ribbonsToCheck) {
                it.MoveNext();
                if (!a.path.specialRadii.TryGetValue(a.indexInPath, out float radius))
                    radius = a.path.baseRadius;
                if (it.Current < this.radius / 2 + radius)
                    touchedRibbons.Add(a);
            }
            job.distances.Dispose();
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

        private struct DistanceToSaberJob : IJobParallelFor {
            [ReadOnly] public NativeArray<Vector3> APoints;
            [ReadOnly] public NativeArray<Vector3> BPoints;

            public NativeArray<float> distances;

            [ReadOnly] public Vector3 saberStart;
            [ReadOnly] public Vector3 saberEnd;
            [ReadOnly] public float saberDiffMagnitude;
            [ReadOnly] public Vector3 saberDiffNormalized;

            public void Execute(int i) {
                //Adapted from https://stackoverflow.com/questions/2824478/shortest-distance-between-two-line-segments

                float Determinant(Vector3 a, Vector3 b, Vector3 c) {
                    return a.x * b.y * c.z + a.y * b.z * c.x + a.z * b.x * c.y - c.x * b.y * a.z - c.y * b.z * a.x - c.z * b.x * a.y;
                }

                var pointsDiff = BPoints[i] - APoints[i];
                float pointsDiffMagnitude = pointsDiff.magnitude;
                var pointsDiffNormalized = pointsDiff / pointsDiffMagnitude;

                var cross = Vector3.Cross(saberDiffNormalized, pointsDiffNormalized);
                var denom = cross.magnitude * cross.magnitude;

                if (denom == 0) {
                    var d0 = Vector3.Dot(saberDiffNormalized, (APoints[i] - saberStart));

                    var d1 = Vector3.Dot(saberDiffNormalized, (BPoints[i] - saberStart));

                    if (d0 <= 0 && 0 >= d1) {
                        if (Mathf.Abs(d0) < Mathf.Abs(d1)) {
                            distances[i] = (saberStart - APoints[i]).magnitude;
                            return;
                        }

                        distances[i] = (saberStart - BPoints[i]).magnitude;
                        return;
                    }

                    else if (d0 >= saberDiffMagnitude && saberDiffMagnitude <= d1) {
                        if (Mathf.Abs(d0) < Mathf.Abs(d1)) {
                            distances[i] = (saberEnd - APoints[i]).magnitude;
                            return;
                        }

                        distances[i] = (saberEnd - BPoints[i]).magnitude;
                        return;
                    }

                    distances[i] = (((d0 * saberDiffNormalized) + saberStart) - APoints[i]).magnitude;
                    return;
                }


                // Lines criss-cross: Calculate the projected closest points
                var t = (APoints[i] - saberStart);
                var detA = Determinant(t, pointsDiffNormalized, cross);
                var detB = Determinant(t, saberDiffNormalized, cross);

                var t0 = detA / denom;
                var t1 = detB / denom;

                var pA = saberStart + (saberDiffNormalized * t0); // Projected closest point on segment A
                var pB = APoints[i] + (pointsDiffNormalized * t1); // Projected closest point on segment B


                // Clamp projections
                if (t0 < 0)
                    pA = saberStart;
                else if (t0 > saberDiffMagnitude)
                    pA = saberEnd;

                if (t1 < 0)
                    pB = APoints[i];
                else if (t1 > pointsDiffMagnitude)
                    pB = BPoints[i];

                float dot;
                // Clamp projection A
                if (t0 < 0 || t0 > saberDiffMagnitude) {
                    dot = Vector3.Dot(pointsDiffNormalized, (pA - APoints[i]));
                    if (dot < 0)
                        dot = 0;
                    else if (dot > pointsDiffMagnitude)
                        dot = pointsDiffMagnitude;
                    pB = APoints[i] + (pointsDiffNormalized * dot);
                }
                // Clamp projection B
                if (t1 < 0 || t1 > pointsDiffMagnitude) {
                    dot = Vector3.Dot(saberDiffNormalized, (pB - saberStart));
                    if (dot < 0)
                        dot = 0;
                    else if (dot > saberDiffMagnitude)
                        dot = saberDiffMagnitude;
                    pA = saberStart + (saberDiffNormalized * dot);
                }

                distances[i] = (pA - pB).magnitude;
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