using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Selector : MonoBehaviour {

    protected GameObject leftHand;
    protected GameObject rightHand;
    protected Visualization viz;

    public HashSet<int[]> districtsToCheck = new HashSet<int[]>(new CoordsEqualityComparer());
    public HashSet<Atom> ribbonsToCheck = new HashSet<Atom>();
    public HashSet<Atom> selectedRibbons = new HashSet<Atom>();

    protected abstract void CreateSelectorObjects();

    protected abstract void UpdateSelectorGeometry();

    protected abstract void FindDistrictsToCheck();

    protected abstract void FindSelectedRibbons();

    protected void BaseStart () {
        leftHand = GameObject.Find("leftHand");
        rightHand = GameObject.Find("rightHand");
        viz = GameObject.Find("airTraffic").GetComponent<Visualization>();

        CreateSelectorObjects();
    }
	
	protected void BaseUpdate () {
        UpdateSelectorGeometry();

        FindDistrictsToCheck();

        foreach (Atom a in ribbonsToCheck) {
            a.shouldHighlight = false;
        }

        ribbonsToCheck.Clear();
        foreach (int[] d in districtsToCheck) {
            foreach (Atom a in viz.districts[d[0], d[1], d[2]].atoms_segment)
                ribbonsToCheck.Add(a);
        }
        
        Color32 yellow = new Color32(255, 240, 20, 255);
        foreach (Atom a in ribbonsToCheck) {
            a.shouldHighlight = true;
            a.highlightColor = yellow;
        }

        FindSelectedRibbons();
	}
}
