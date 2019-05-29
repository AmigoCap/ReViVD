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
        public float rotx = 0f;
        public float roty = 0f;
        public float rotz = 0f;

        private Vector3 cuboidCenter = new Vector3();
        private Vector3 extendedScale = new Vector3();

        protected override void CreatePrimitive() {
            primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            primitive.transform.localScale = new Vector3(width, length, height);
        }

        protected override void AttachToHand() {
            primitive.transform.parent = SteamVR_ControllerManager.Instance.right.transform;
            primitive.transform.localPosition = new Vector3(rightOffset, upOffset, distance);
            primitive.transform.localRotation = Quaternion.Euler(rotx, roty, rotz);
        }

        protected override void UpdatePrimitive() {
            cuboidCenter = primitive.transform.position;
        }

        protected override void FindDistrictsToCheck() {
            Visualization viz = Visualization.Instance;
            Vector3 cuboidCenter_viz = viz.transform.InverseTransformPoint(cuboidCenter);

            extendedScale = (primitive.transform.localScale + viz.districtSize) / 2;

            int[] d0 = viz.FindDistrictCoords(cuboidCenter_viz);

            checkedDistricts.Add(d0);

            bool foundMoreDistricts = false;
            int dist = 1;

            Vector3 P1 = cuboidCenter + Quaternion.Euler(primitive.transform.eulerAngles) * extendedScale;
            Vector3 P2 = cuboidCenter + Quaternion.Euler(primitive.transform.eulerAngles) * Vector3.Scale(extendedScale, new Vector3(1, 1, -1));
            Vector3 P3 = cuboidCenter + Quaternion.Euler(primitive.transform.eulerAngles) * Vector3.Scale(extendedScale, new Vector3(1, -1, 1));
            Vector3 P4 = cuboidCenter + Quaternion.Euler(primitive.transform.eulerAngles) * Vector3.Scale(extendedScale, new Vector3(-1, 1, 1));


            do {
                foundMoreDistricts = false;
                for (int i = d0[0] - dist; i <= d0[0] + dist; i++) {
                    for (int j = d0[1] - dist; j <= d0[1] + dist; j++) {
                        for (int k = d0[2] - dist; k <= d0[2] + dist; k += 2 * dist) {
                            int[] d2 = new int[] { i, j, k };
                            if (IsInCuboid(viz.getDistrictCenter(d2), P1, P2, P3, P4)) {
                                checkedDistricts.Add(d2);
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
                                checkedDistricts.Add(d2);
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
                                checkedDistricts.Add(d2);
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

        protected override void ParseRibbonsToCheck() {
            foreach (Atom a in checkedRibbons) {
                if (CollisionWithCuboid(a)) {
                    touchedRibbons.Add(a);
                }
            }
        }

        private bool CollisionWithCuboid(Atom a) {

            float radius;
            if (!a.path.specialRadii.TryGetValue(a.indexInPath, out radius))
                radius = a.path.baseRadius;

            extendedScale = primitive.transform.localScale / 2;// + new Vector3(radius, radius, radius) ;

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