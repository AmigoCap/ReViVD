﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace Revivd {

    public static class Tools {
        private static float startTime = 0;
        private static float time = 0;
        private static float subTime = 0;
        private static string clockString = "";
        private static Vector2 _GPSOrigin = Vector2.zero;
        private static float _LatGPS { get { return _GPSOrigin.x; } }
        private static float _LonGPS { get { return _GPSOrigin.y; } }
        private static float metersPerLat;
        private static float metersPerLon;

        public static void SetGPSOrigin(Vector2 GPSOrigin) {
            _GPSOrigin = GPSOrigin;
        }

        private static void FindMetersPerLat(float lat) {
            float m1 = 111132.92f;
            float m2 = -559.82f;
            float m3 = 1.175f;
            float m4 = -0.0023f;
            float p1 = 111412.84f;
            float p2 = -93.5f;
            float p3 = 0.118f;
            lat = lat * Mathf.Deg2Rad;
            metersPerLat = m1 + (m2 * Mathf.Cos(2 * lat)) + (m3 * Mathf.Cos(4 * lat)) + (m4 * Mathf.Cos(6 * lat));
            metersPerLon = (p1 * Mathf.Cos(lat)) + (p2 * Mathf.Cos(3 * lat)) + (p3 * Mathf.Cos(5 * lat));
        }
    

        public static Vector3 GPSToXYZ(Vector2 GPSCoordinates) {
            FindMetersPerLat(_LatGPS);
            float z = metersPerLat * (GPSCoordinates.x - _LatGPS);
            float x = metersPerLon * (GPSCoordinates.y - _LonGPS);
            return new Vector3(x, 0, z);
        }

        public static Vector2 XYZToGPS(Vector3 position) {
            FindMetersPerLat(_LatGPS);
            Vector2 GPSCoordinates = new Vector2(0, 0);
            GPSCoordinates.x = (_LatGPS + (position.z) / metersPerLat);
            GPSCoordinates.y = (_LonGPS + (position.x) / metersPerLon);
            return GPSCoordinates;
        }

        public static void StartClock() {
            if (!Visualization.Instance.debugMode)
                return;

            clockString = "Started Clock\n";
            time = Time.realtimeSinceStartup;
            startTime = time;
            subTime = time;
        }

        public static void AddClockStop(string message) {
            if (!Visualization.Instance.debugMode)
                return;

            float delta = Time.realtimeSinceStartup - time;
            clockString += delta.ToString("F4") + " - " + message + '\n';

            time = Time.realtimeSinceStartup;
            subTime = time;
        }

        public static void AddSubClockStop(string message) {
            if (!Visualization.Instance.debugMode)
                return;

            float delta = Time.realtimeSinceStartup - subTime;
            clockString += "    " + delta.ToString("F4") + " - " + message + '\n';

            subTime = Time.realtimeSinceStartup;
        }

        public static void EndClock(string message = "End") {
            if (!Visualization.Instance.debugMode)
                return;

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

        public static float ParseField_f(UnityEngine.UI.InputField field, float ifEmpty = 0f) {
            if (field.text == "")
                return ifEmpty;
            if (float.TryParse(field.text.Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float result))
                return result;
            return field.text[0] == '-' ? float.MinValue : float.MaxValue;
        }

        public static int ParseField_i(UnityEngine.UI.InputField field, int ifEmpty = 0) {
            if (field.text == "")
                return ifEmpty;
            if (int.TryParse(field.text, out int result))
                return result;
            return field.text[0] == '-' ? int.MinValue : int.MaxValue;
        }

        public static string GetFullPath(string filename) {
            return System.IO.Path.Combine(ControlPanel.Instance.workingDirectory, filename);
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

        public static int Sign(float f) {
            return f < 0 ? -1 : 1;
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

        public static float InterpretExponent(string word) {
            int pos = word.IndexOf("E");
            if (pos == -1) {
                pos = word.IndexOf("e");
            }
            if (pos != -1) {
                return float.Parse(word.Substring(0, pos - 1).Replace('.', ',')) * Mathf.Pow(10, float.Parse(word.Substring(pos + 1)));
            }
            else
                return float.Parse(word.Replace('.', ','));
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