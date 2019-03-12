using System;

using System.Collections.Generic;
using UnityEngine;

public class Filters : MonoBehaviour {

    public HashSet<Atom> ribbonsToCheck = new HashSet<Atom>();
    public HashSet<Atom> selectedRibbons = new HashSet<Atom>();

    GameObject leftHand;
    GameObject rightHand;

    Visualization viz;

    public float saberLength = 2f;
    public float saberThickness = 0.15f;

	// Use this for initialization
	void Start () {
        leftHand = GameObject.Find("leftHand");
        rightHand = GameObject.Find("rightHand");
        viz = GameObject.Find("airTraffic").GetComponent<Visualization>();

        GameObject saber = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        saber.transform.parent = rightHand.transform;
        saber.transform.localPosition = saber.transform.InverseTransformDirection(rightHand.transform.forward) * saberLength / 2;
        saber.transform.localRotation = Quaternion.Euler(90, 0, 0);
        saber.transform.localScale = new Vector3(saberThickness, saberLength/2, saberThickness);
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

    // Update is called once per frame
    void Update () {
        Vector3 saberStart = rightHand.transform.position;
        Vector3 saberEnd = rightHand.transform.position + rightHand.transform.forward * saberLength;

        Vector3 saberStart_viz = viz.transform.InverseTransformPoint(saberStart);
        Vector3 saberEnd_viz = viz.transform.InverseTransformPoint(saberEnd);
        int[] saberStartDistrict = viz.FindDistrict(saberStart_viz);
        int[] saberEndDistrict = viz.FindDistrict(saberEnd_viz);

        List<int[]> cutDistricts = Tools.Bresenham(saberStartDistrict, saberEndDistrict);
        HashSet<int[]> cutDistricts_extended = new HashSet<int[]>(new CoordsEqualityComparer());
        int[] maxDistrict = new int[] { viz.districts.GetLength(0), viz.districts.GetLength(1), viz.districts.GetLength(2) };
        foreach (int[] d in cutDistricts) {
            for (int i = d[0]-1; i <= d[0]+1; i++) {
                for (int j = d[1]-1; j <= d[1]+1; j++) {
                    for (int k = d[2]-1; k <= d[2]+1; k++) {
                        int[] d2 = new int[] { i, j, k };
                        if (Tools.IsWithin(d2, new int[] { 0, 0, 0 }, maxDistrict)) {
                            cutDistricts_extended.Add(d2);
                        }
                    }
                }
            }
        }

        foreach (Atom a in ribbonsToCheck) {
            a.shouldHighlight = false;
            a.highlightColor = new Color32(255, 240, 20, 255);
        }

        ribbonsToCheck.Clear();
        foreach (int[] d in cutDistricts_extended) {
            foreach (Atom a in viz.districts[d[0], d[1], d[2]].atoms_segment)
                ribbonsToCheck.Add(a);            
        }

        selectedRibbons.Clear();
        foreach (Atom a in ribbonsToCheck) {
            a.shouldHighlight = true;
            float radius;
            if (!a.path.specialRadii.TryGetValue(a.indexInPath, out radius))
                radius = a.path.baseRadius;
            if (ClosestDistanceBetweenSegments(a.path.transform.TransformPoint(a.point), a.path.transform.TransformPoint(a.path.AtomsAsBase[a.indexInPath + 1].point), saberStart, saberEnd) < saberThickness / 2 + radius) {
                a.highlightColor = new Color32(0, 255, 0, 255);
                selectedRibbons.Add(a);
            }
        }
    }
}
