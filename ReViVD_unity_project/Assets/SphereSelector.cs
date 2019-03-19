using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereSelector : Selector {

    public float sphereDistance = 2f;
    public float sphereRadius = 0.5f;

    private Vector3 sphereCenter = new Vector3();

    protected override void CreateSelectorObjects() {
        GameObject saber = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        saber.transform.parent = rightHand.transform;
        saber.transform.localPosition = saber.transform.InverseTransformDirection(rightHand.transform.forward) * sphereDistance;
        saber.transform.localScale = new Vector3(sphereRadius*2, sphereRadius*2, sphereRadius*2);
    }

    protected override void UpdateSelectorGeometry() {
        sphereCenter = rightHand.transform.position + rightHand.transform.forward * sphereDistance;
    }

    protected override void FindDistrictsToCheck() {
        districtsToCheck.Clear();

        Vector3 sphereCenter_viz = viz.transform.InverseTransformPoint(sphereCenter);

        int[] d0 = viz.FindDistrict(sphereCenter_viz);
        int[] minDistrict = new int[] { 0, 0, 0 };
        int[] maxDistrict = new int[] { viz.districts.GetLength(0), viz.districts.GetLength(1), viz.districts.GetLength(2) };

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
                        if (Tools.IsWithin(d2, minDistrict, maxDistrict) && (viz.districts[i, j, k].center - sphereCenter_viz).magnitude < sphereRadius + halfDiag + margin) {
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
                        if (Tools.IsWithin(d2, minDistrict, maxDistrict) && (viz.districts[i, j, k].center - sphereCenter_viz).magnitude < sphereRadius + halfDiag + margin) {
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
                        if (Tools.IsWithin(d2, minDistrict, maxDistrict) && (viz.districts[i, j, k].center - sphereCenter_viz).magnitude < sphereRadius + halfDiag + margin) {
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

    protected override void FindSelectedRibbons() {
        selectedRibbons.Clear();

        foreach (Atom a in ribbonsToCheck) {
            float radius;
            if (!a.path.specialRadii.TryGetValue(a.indexInPath, out radius))
                radius = a.path.baseRadius;
            if (DistancePointSegment(sphereCenter, a.path.transform.TransformPoint(a.point), a.path.transform.TransformPoint(a.path.AtomsAsBase[a.indexInPath + 1].point)) < sphereRadius + radius) {
                selectedRibbons.Add(a);
            }
        }

        Color32 green = new Color32(0, 255, 0, 255);
        foreach (Atom a in selectedRibbons) {
            a.shouldHighlight = true;
            a.highlightColor = green;
        }
    }

    void Start() {
        BaseStart();
    }

    void Update() {
        BaseUpdate();
    }
}
