using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Revivd {

    public class CylinderPart : SelectorPart {

        public float saberLength = 5f;
        public float saberThickness = 0.3f;

        private Vector3 saberStart = new Vector3();
        private Vector3 saberEnd = new Vector3();

        protected override void CreatePrimitive() {
            primitive = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            primitive.transform.parent = SteamVR_ControllerManager.Instance.right.transform;
            primitive.transform.localPosition = new Vector3(0, 0, saberLength / 2);
            primitive.transform.localRotation = Quaternion.Euler(90, 0, 0);
            primitive.transform.localScale = new Vector3(saberThickness, saberLength / 2, saberThickness);
        }

        public override void UpdatePrimitive() {
            saberStart = primitive.transform.position - primitive.transform.parent.forward * saberLength / 2;
            saberEnd = primitive.transform.position + primitive.transform.parent.forward * saberLength / 2;
        }

        public override void FindDistrictsToCheck() {
            Visualization viz = Visualization.Instance;
            Vector3 saberStart_viz = viz.transform.InverseTransformPoint(saberStart);
            Vector3 saberEnd_viz = viz.transform.InverseTransformPoint(saberEnd);

            List<int[]> cutDistricts = Tools.Bresenham(viz.FindDistrictCoords(saberStart_viz), viz.FindDistrictCoords(saberEnd_viz));

            foreach (int[] d in cutDistricts) {
                for (int i = d[0] - 1; i <= d[0] + 1; i++) {
                    for (int j = d[1] - 1; j <= d[1] + 1; j++) {
                        for (int k = d[2] - 1; k <= d[2] + 1; k++) {
                            districtsToCheck.Add(new int[] { i, j, k });
                        }
                    }
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

        public override void FindTouchedRibbons() {
            foreach (Atom a in ribbonsToCheck) {
                if (!a.path.specialRadii.TryGetValue(a.indexInPath, out float radius))
                    radius = a.path.baseRadius;
                if (ClosestDistanceBetweenSegments(a.path.transform.TransformPoint(a.point), a.path.transform.TransformPoint(a.path.AtomsAsBase[a.indexInPath + 1].point), saberStart, saberEnd) < saberThickness / 2 + radius) {
                    touchedRibbons.Add(a);
                }
            }
        }
    }

}