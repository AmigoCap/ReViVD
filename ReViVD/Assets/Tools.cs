using System;
using System.Collections.Generic;
using UnityEngine;

namespace Revivd {

    public static class Tools {
        private static float startTime = 0;
        private static float time = 0;
        private static string clockString = "";
        public static void StartClock() {
            clockString = "Started Clock\n";
            time = Time.realtimeSinceStartup;
            startTime = time;
        }
        public static void AddClockStop(string message) {
            float delta = Time.realtimeSinceStartup - time;
            clockString += delta.ToString("F4") + " - " + message + '\n';

            time = Time.realtimeSinceStartup;
        }
        public static void EndClock(string message = "End") {
            AddClockStop(message);
            float totalDelta = Time.realtimeSinceStartup - startTime;
            clockString += totalDelta.ToString("F4") + " - " + "Total";
            Debug.Log(clockString);
        }

        public static List<int[]> Bresenham(int[] start, int[] end) {
            List<int[]> results = new List<int[]> {
                (int[])start.Clone()
            };

            int Dx = Math.Abs(end[0] - start[0]), Dy = Math.Abs(end[1] - start[1]), Dz = Math.Abs(end[2] - start[2]);
            int xs = end[0] > start[0] ? 1 : -1, ys = end[1] > start[1] ? 1 : -1, zs = end[2] > start[2] ? 1 : -1;

            if (Dx >= Dy && Dx >= Dz) {
                int p1 = 2 * Dy - Dx, p2 = 2 * Dz - Dx;
                while (start[0] != end[0]) {
                    start[0] += xs;
                    if (p1 >= 0) {
                        start[1] += ys;
                        p1 -= 2 * Dx;
                    }
                    if (p2 >= 0) {
                        start[2] += zs;
                        p2 -= 2 * Dx;
                    }
                    p1 += 2 * Dy;
                    p2 += 2 * Dz;
                    results.Add((int[])start.Clone());
                }
            }
            else if (Dy >= Dx && Dy >= Dz) {
                int p1 = 2 * Dx - Dy, p2 = 2 * Dz - Dy;
                while (start[1] != end[1]) {
                    start[1] += ys;
                    if (p1 >= 0) {
                        start[0] += xs;
                        p1 -= 2 * Dy;
                    }
                    if (p2 >= 0) {
                        start[2] += zs;
                        p2 -= 2 * Dy;
                    }
                    p1 += 2 * Dx;
                    p2 += 2 * Dz;
                    results.Add((int[])start.Clone());
                }
            }
            else {
                int p1 = 2 * Dy - Dz, p2 = 2 * Dx - Dz;
                while (start[2] != end[2]) {
                    start[2] += zs;
                    if (p1 >= 0) {
                        start[1] += ys;
                        p1 -= 2 * Dz;
                    }
                    if (p2 >= 0) {
                        start[0] += xs;
                        p2 -= 2 * Dz;
                    }
                    p1 += 2 * Dy;
                    p2 += 2 * Dx;
                    results.Add((int[])start.Clone());
                }
            }

            return results;
        }

        public static int Mod(int x, int m) {
            int r = x % m;
            return r < 0 ? r + m : r;
        }

        public static int Pow(int num, int exp) {
            return exp == 0 ? 1 : num * Pow(num, exp - 1);
        }

        public static float FMod(float x, float m) {
            float r = x % m;
            return r < 0 ? r + m : r;
        }

        public static float MaxAbs(float f, float max) {
            return f > max ? max : (f < -max ? -max : f);
        }

        public static float MinAbs(float f, float min) {
            if (f > 0)
                return f < min ? min : f;
            return f > -min ? -min : f;
        }

        public static float Limit(float f, float lower, float upper) {
            return f < lower ? lower : (f > upper ? upper : f);
        }

        public static string CoordsToString(int[] c) {
            string str = '['  + c[0].ToString() + ' ' + c[1].ToString() + ' ' + c[2].ToString() + ']';
            return str;
        }

        public static List<int[]> Amanatides(Vector3 start, Vector3 end) {
            List<int[]> L = new List<int[]>();
            int[] startD = Visualization.Instance.FindDistrictCoords(start);
            int[] endD = Visualization.Instance.FindDistrictCoords(end);
            L.Add((int[])startD.Clone());

            Vector3 v = (end - start).normalized;
            ref Vector3 dSize = ref Visualization.Instance.districtSize;
            Vector3 delta = new Vector3(Mathf.Abs(dSize.x / v.x), Mathf.Abs(dSize.y / v.y), Mathf.Abs(dSize.z / v.z));
            int[] stepD = { v.x < 0 ? -1 : 1, v.y < 0 ? -1 : 1, v.z < 0 ? -1 : 1 };

            Vector3 max = new Vector3(
                Mathf.Abs(((v.x < 0 ? 0 : dSize.x) - FMod(start.x, dSize.x)) / v.x),
                Mathf.Abs(((v.y < 0 ? 0 : dSize.y) - FMod(start.y, dSize.y)) / v.y),
                Mathf.Abs(((v.z < 0 ? 0 : dSize.z) - FMod(start.z, dSize.z)) / v.z));

            CoordsEqualityComparer comparer = new CoordsEqualityComparer();

            while (!comparer.Equals(startD, endD) && L.Count < 50) {
                if (Math.Abs(startD[0] - endD[0]) + Math.Abs(startD[1] - endD[1]) + Math.Abs(startD[2] - endD[2]) == 1) //Adjacent à la fin : utile pour contrer les imprécisions flottantes
                    endD.CopyTo(startD, 0);
                else if (max.x < max.y) {
                    if (max.x < max.z) {
                        startD[0] += stepD[0];
                        max.x += delta.x;
                    }
                    else {
                        startD[2] += stepD[2];
                        max.z += delta.z;
                    }
                }
                else {
                    if (max.y < max.z) {
                        startD[1] += stepD[1];
                        max.y += delta.y;
                    }
                    else {
                        startD[2] += stepD[2];
                        max.z += delta.z;
                    }
                }

                L.Add((int[])startD.Clone());
            }

            return L;
        }

        public static HashSet<int[]> Amanatides(Vector3 start, Vector3 end, float lineThickness) {
            CoordsEqualityComparer comparer = new CoordsEqualityComparer();
            HashSet<int[]> H = new HashSet<int[]>(comparer);
            int[] startD = Visualization.Instance.FindDistrictCoords(start);
            int[] endD = Visualization.Instance.FindDistrictCoords(end);
            H.Add((int[])startD.Clone());

            Vector3 v = (end - start).normalized;
            Vector3 dSize = Visualization.Instance.districtSize;
            Vector3 delta = new Vector3(Mathf.Abs(dSize.x / v.x), Mathf.Abs(dSize.y / v.y), Mathf.Abs(dSize.z / v.z));
            int[] stepD = { v.x < 0 ? -1 : 1, v.y < 0 ? -1 : 1, v.z < 0 ? -1 : 1 };

            Vector3 max = new Vector3(
                Mathf.Abs(((v.x < 0 ? 0 : dSize.x) - FMod(start.x, dSize.x)) / v.x),
                Mathf.Abs(((v.y < 0 ? 0 : dSize.y) - FMod(start.y, dSize.y)) / v.y),
                Mathf.Abs(((v.z < 0 ? 0 : dSize.z) - FMod(start.z, dSize.z)) / v.z));

            void addByThickness(int dir, ref Vector3 inter) {
                if (dir == 0) {
                    float interX = FMod(inter.x, dSize.x);
                    if (interX < lineThickness) {
                        H.Add(new int[] { startD[0] - 1, startD[1], startD[2] });
                    }
                    if (interX > dSize.x - lineThickness) {
                        H.Add(new int[] { startD[0] + 1, startD[1], startD[2] });
                    }
                }
                else if (dir == 1) {
                    float interY = FMod(inter.y, dSize.y);
                    if (interY < lineThickness) {
                        H.Add(new int[] { startD[0], startD[1] - 1, startD[2] });
                    }
                    if (interY > dSize.y - lineThickness) {
                        H.Add(new int[] { startD[0], startD[1] + 1, startD[2] });
                    }
                }
                else if (dir == 2) {
                    float interZ = FMod(inter.z, dSize.z);
                    if (interZ < lineThickness) {
                        H.Add(new int[] { startD[0], startD[1], startD[2] - 1 });
                    }
                    if (interZ > dSize.z - lineThickness) {
                        H.Add(new int[] { startD[0], startD[1], startD[2] + 1 });
                    }
                }
            }

            while (!comparer.Equals(startD, endD)) {
                if (Math.Abs(startD[0] - endD[0]) + Math.Abs(startD[1] - endD[1]) + Math.Abs(startD[2] - endD[2]) == 1) //Adjacent à la fin : utile pour contrer les imprécisions flottantes
                    endD.CopyTo(startD, 0);
                else if (max.x < max.y) {
                    if (max.x < max.z) {
                        Vector3 inter = start + v * max.x;
                        addByThickness(1, ref inter);
                        addByThickness(2, ref inter);
                        startD[0] += stepD[0];
                        max.x += delta.x;
                    }
                    else {
                        Vector3 inter = start + v * max.z;
                        addByThickness(0, ref inter);
                        addByThickness(1, ref inter);
                        startD[2] += stepD[2];
                        max.z += delta.z;
                    }
                }
                else {
                    if (max.y < max.z) {
                        Vector3 inter = start + v * max.y;
                        addByThickness(0, ref inter);
                        addByThickness(2, ref inter);
                        startD[1] += stepD[1];
                        max.y += delta.y;
                    }
                    else {
                        Vector3 inter = start + v * max.z;
                        addByThickness(0, ref inter);
                        addByThickness(1, ref inter);
                        startD[2] += stepD[2];
                        max.z += delta.z;
                    }
                }

                H.Add((int[])startD.Clone());
            }

            return H;
        }

        public static bool IsWithin(int[] c, int[] cmin, int[] cmax) {
            int n = c.Length;
            for (int i = 0; i < n; i++) {
                if (c[i] < cmin[i] || c[i] >= cmax[i])
                    return false;
            }
            return true;
        }
    }

    class CoordsEqualityComparer : IEqualityComparer<int[]> {
        public bool Equals(int[] c1, int[] c2) {
            if (c1.Length != c2.Length) {
                return false;
            }
            for (int i = 0; i < c1.Length; i++) {
                if (c1[i] != c2[i])
                    return false;
            }
            return true;
        }

        public int GetHashCode(int[] c) {
            int result = 19;
            foreach (int i in c) {
                unchecked {
                    result = result * 486187739 + i;
                }
            }
            return result;
        }
    }

}