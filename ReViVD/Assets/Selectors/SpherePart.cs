using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Revivd {

    public class SpherePart : SelectorPart {

        public float distance = 2f;
        public float radius = 0.5f;
        public float upOffset = 0f;
        public float rightOffset = 0f;

        private Vector3 sphereCenter = new Vector3();

        protected override void CreatePrimitive() {
            primitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            primitive.transform.localScale = new Vector3(radius * 2, radius * 2, radius * 2);
        }

        public override void Attach() {
            primitive.transform.parent = SteamVR_ControllerManager.Instance.right.transform;
            primitive.transform.localPosition = new Vector3(rightOffset, upOffset, distance);
        }

        public override void UpdatePrimitive() {
            sphereCenter = primitive.transform.position;
        }

        public override void FindDistrictsToCheck() {
            Visualization viz = Visualization.Instance;
            Vector3 sphereCenter_viz = viz.transform.InverseTransformPoint(sphereCenter);

            int[] d0 = viz.FindDistrictCoords(sphereCenter_viz);
           
            districtsToCheck.Add(d0);

            bool foundMoreDistricts = false;
            int dist = 1;
            float halfDiag = Mathf.Sqrt(Mathf.Pow(viz.districtSize[0], 2) + Mathf.Pow(viz.districtSize[1], 2) + Mathf.Pow(viz.districtSize[2], 2)) / 2;
            float margin = Mathf.Max(viz.districtSize[0], viz.districtSize[1], viz.districtSize[2]);

            do {
                foundMoreDistricts = false;
                for (int i = d0[0] - dist; i <= d0[0] + dist; i++) {
                    for (int j = d0[1] - dist; j <= d0[1] + dist; j++) {
                        for (int k = d0[2] - dist; k <= d0[2] + dist; k += 2 * dist) {
                            int[] d2 = new int[] { i, j, k };
                            if ((viz.getDistrictCenter(d2) - sphereCenter_viz).magnitude < radius + halfDiag + margin) {
                                districtsToCheck.Add(d2);
                                foundMoreDistricts = true;
                            }
                        }
                    }
                }
                for (int i = d0[0] - dist; i <= d0[0] + dist; i++) {
                    for (int j = d0[1] - dist; j <= d0[1] + dist; j += 2 * dist) {
                        for (int k = d0[2] - dist; k <= d0[2] + dist; k++) {
                            int[] d2 = new int[] { i, j, k };
                            if ((viz.getDistrictCenter(d2) - sphereCenter_viz).magnitude < radius + halfDiag + margin) {
                                districtsToCheck.Add(d2);
                                foundMoreDistricts = true;
                            }
                        }
                    }
                }
                for (int i = d0[0] - dist; i <= d0[0] + dist; i += 2 * dist) {
                    for (int j = d0[1] - dist; j <= d0[1] + dist; j++) {
                        for (int k = d0[2] - dist; k <= d0[2] + dist; k++) {
                            int[] d2 = new int[] { i, j, k };
                            if ((viz.getDistrictCenter(d2) - sphereCenter_viz).magnitude < radius + halfDiag + margin) {
                                districtsToCheck.Add(d2);
                                foundMoreDistricts = true;
                            }
                        }
                    }
                }

                dist++;
            } while (foundMoreDistricts);
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

        public override void FindTouchedRibbons() {
            foreach (Atom a in ribbonsToCheck) {
                float radius;
                if (!a.path.specialRadii.TryGetValue(a.indexInPath, out radius))
                    radius = a.path.baseRadius;
                if (DistancePointSegment(sphereCenter, a.path.transform.TransformPoint(a.point), a.path.transform.TransformPoint(a.path.AtomsAsBase[a.indexInPath + 1].point)) < this.radius + radius) {
                    touchedRibbons.Add(a);
                }
            }
        }
    }

}