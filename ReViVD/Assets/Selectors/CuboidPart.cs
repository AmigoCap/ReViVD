using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Revivd {

    public class CuboidPart : SelectorPart {

        public float distance = 4f;
        public float width = 1f;
        public float length = 1f;
        public float height = 1f;
        public float upOffset = 0f;
        public float rightOffset = 0f;

        private Vector3 cuboidCenter = new Vector3();
        private Vector3 scale = new Vector3();

        protected override void CreatePrimitive() {
            primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            primitive.transform.localScale = new Vector3(width, length, height);
        }

        protected override void AttachToHand() {
            primitive.transform.parent = SteamVR_ControllerManager.Instance.right.transform;
            primitive.transform.localPosition = new Vector3(rightOffset, upOffset, distance);
        }

        public override void UpdatePrimitive() {
            cuboidCenter = primitive.transform.position;
        }

        public override void FindDistrictsToCheck() {
            Visualization viz = Visualization.Instance;
            Vector3 cuboidCenter_viz = viz.transform.InverseTransformPoint(cuboidCenter);

            scale = (primitive.transform.localScale + viz.districtSize) / 2;

            int[] d0 = viz.FindDistrictCoords(cuboidCenter_viz);

            districtsToCheck.Add(d0);

            bool foundMoreDistricts = false;
            int dist = 1;

            Vector3 P1 = cuboidCenter + Quaternion.Euler(primitive.transform.eulerAngles) * scale;
            Vector3 P2 = cuboidCenter + Quaternion.Euler(primitive.transform.eulerAngles) * Vector3.Scale(scale, new Vector3(1, 1, -1));
            Vector3 P3 = cuboidCenter + Quaternion.Euler(primitive.transform.eulerAngles) * Vector3.Scale(scale, new Vector3(1, -1, 1));
            Vector3 P4 = cuboidCenter + Quaternion.Euler(primitive.transform.eulerAngles) * Vector3.Scale(scale, new Vector3(-1, 1, 1));


            do {
                foundMoreDistricts = false;
                for (int i = d0[0] - dist; i <= d0[0] + dist; i++) {
                    for (int j = d0[1] - dist; j <= d0[1] + dist; j++) {
                        for (int k = d0[2] - dist; k <= d0[2] + dist; k += 2 * dist) {
                            int[] d2 = new int[] { i, j, k };
                            if (IsInCuboid(viz.getDistrictCenter(d2), P1, P2, P3, P4)) {
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
                            if (IsInCuboid(viz.getDistrictCenter(d2), P1, P2, P3, P4)) {
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
                            if (IsInCuboid(viz.getDistrictCenter(d2), P1, P2, P3, P4)) {
                                districtsToCheck.Add(d2);
                                foundMoreDistricts = true;
                            }
                        }
                    }
                }
                dist++;
            } while (foundMoreDistricts);
        }

        private bool IsInCuboid(Vector3 P, Vector3 P1, Vector3 P2, Vector3 P3, Vector3 P4) {
            float a = Vector3.Dot(P - P1, P2 - P1);
            float b = Vector3.Dot(P - P1, P3 - P1);
            float c = Vector3.Dot(P - P1, P4 - P1);
            return ((a >= 0) && (a <= Vector3.Dot(P2 - P1, P2 - P1)) && (b >= 0) && (b <= Vector3.Dot(P3 - P1, P3 - P1)) && (c >= 0) && (c <= Vector3.Dot(P4 - P1, P4 - P1)));
        }

        public override void FindTouchedRibbons() {
            foreach (Atom a in ribbonsToCheck) {
                if (CollisionWithCuboid(a)) {
                    touchedRibbons.Add(a);
                }
            }
        }

        private bool CollisionWithCuboid(Atom a) {

            float radius;
            if (!a.path.specialRadii.TryGetValue(a.indexInPath, out radius))
                radius = a.path.baseRadius;

            scale = primitive.transform.localScale / 2 ;

            //Segment in the world space coordinates
            Vector3 b = a.path.transform.TransformPoint(a.point);  //Coordinates World Space
            Vector3 c = a.path.transform.TransformPoint(a.path.AtomsAsBase[a.indexInPath + 1].point); //Coordinates World Space
            //and then in the right hand space coordinates
            Vector3 bc = SteamVR_ControllerManager.Instance.right.transform.InverseTransformPoint(b);
            Vector3 cc = SteamVR_ControllerManager.Instance.right.transform.InverseTransformPoint(c);

            Vector3 mc = (bc + cc) / 2; //midpoint vector of the segment
            Vector3 l = bc - mc; 
            Vector3 lext = new Vector3(Mathf.Abs(l.x), Mathf.Abs(l.y), Mathf.Abs(l.z));//extent vector of the segment

            mc = primitive.transform.localPosition - mc;

            if (Mathf.Abs(mc.x) >  scale.x + lext.x) return false;
            if (Mathf.Abs(mc.y) >  scale.y + lext.y) return false;
            if (Mathf.Abs(mc.z) >  scale.z + lext.z) return false;

            if (Mathf.Abs(mc.y * l.z - mc.z * l.y) > (scale.y * lext.z + scale.z * lext.y)) return false;
            if (Mathf.Abs(mc.x * l.z - mc.z * l.x) > (scale.x * lext.z + scale.z * lext.x)) return false;
            if (Mathf.Abs(mc.x * l.y - mc.y * l.x) > (scale.x * lext.y + scale.y * lext.x)) return false;

            return true;
        }

    }
}