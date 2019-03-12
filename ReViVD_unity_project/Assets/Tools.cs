using System;
using System.Collections.Generic;

public static class Tools {
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
        int n = c1.Length;
        if (n != c2.Length) {
            return false;
        }
        for (int i = 0; i < n; i++) {
            if (c1[i] != c2[i])
                return false;
        }
        return true;
    }

    public int GetHashCode(int[] c) {
        int hCode = 0;
        foreach (int i in c)
            hCode ^= i;
        return hCode.GetHashCode();
    }
}