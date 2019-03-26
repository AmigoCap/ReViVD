using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Selector : MonoBehaviour {

    protected GameObject leftHand;
    protected GameObject rightHand;
    protected Visualization viz;

    public bool shouldSelect = false;
    private bool highlightToCheck = false;
    private bool highlightSelected = true;
    private bool updateHighlightToCheck = false;
    private bool updateHighlightSelected = false;

    public HashSet<int[]> districtsToCheck = new HashSet<int[]>(new CoordsEqualityComparer());
    public HashSet<Atom> ribbonsToCheck = new HashSet<Atom>();
    public HashSet<Atom> selectedRibbons = new HashSet<Atom>();

    public bool HighlightToCheck {
        get {
            return highlightToCheck;
        }

        set {
            highlightToCheck = value;
            updateHighlightToCheck = true;
            viz.needsFullRenderingUpdate = true;
        }
    }

    public bool HighlightSelected {
        get {
            return highlightSelected;
        }

        set {
            highlightSelected = value;
            updateHighlightSelected = true;
            viz.needsFullRenderingUpdate = true;
        }
    }

    protected abstract void CreateSelectorObjects();

    protected abstract void UpdateSelectorGeometry();

    protected abstract void FindDistrictsToCheck();

    protected abstract void AddToSelectedRibbons();

    protected void BaseStart() {
        leftHand = GameObject.Find("leftHand");
        rightHand = GameObject.Find("rightHand");
        viz = GameObject.Find("airTraffic").GetComponent<Visualization>();

        CreateSelectorObjects();
    }

    private bool Grip_Right_old = false;
    private bool Grip_Left_old = false;

	protected void BaseUpdate() {
        shouldSelect = false;
        bool shouldClearSelected = false;
        if (Input.GetButton("Trig_Right")) {
            shouldSelect = true;
        }
        else if (Input.GetButtonDown("Trig_Left")) {
            shouldClearSelected = true;
            if (highlightSelected) {
                updateHighlightSelected = true;
                viz.needsFullRenderingUpdate = true;
            }
        }

        bool Grip_Right_new = Input.GetAxis("Grip_Right") > 0.5; //Workaround : les input "boutons" pour les boutons de grip ne marchent pas.
        bool Grip_Left_new = Input.GetAxis("Grip_Left") > 0.5;

        if (Grip_Right_new && !Grip_Right_old) {
            HashSet<Path> selectedPaths = new HashSet<Path>();
            foreach (Atom a in selectedRibbons) {
                selectedPaths.Add(a.path);
            }

            foreach (Path p in viz.PathsAsBase) {
                if (!selectedPaths.Contains(p)) {
                    bool shouldUpdateTriangles = false;
                    foreach (Atom a in p.AtomsAsBase) {
                        if (a.shouldDisplay) {
                            a.shouldDisplay = false;
                            shouldUpdateTriangles = true;
                        }
                    }
                    if (shouldUpdateTriangles)
                        p.GenerateTriangles();
                }
            }
            viz.needsFullRenderingUpdate = true;
        }
        else if (Grip_Left_new && !Grip_Left_old) {
            foreach (Path p in viz.PathsAsBase) {
                bool shouldUpdateTriangles = false;
                foreach (Atom a in p.AtomsAsBase) {
                    if (!a.shouldDisplay) {
                        a.shouldDisplay = true;
                        shouldUpdateTriangles = true;
                    }
                }
                if (shouldUpdateTriangles)
                    p.GenerateTriangles();
            }

            viz.needsFullRenderingUpdate = true;
        }

        Grip_Right_old = Grip_Right_new;
        Grip_Left_old = Grip_Left_new;

        UpdateSelectorGeometry();

        districtsToCheck.Clear();
        if (shouldSelect) {
            FindDistrictsToCheck();
        }

        if (HighlightToCheck || updateHighlightToCheck) {
            foreach (Atom a in ribbonsToCheck) {
                a.shouldHighlight = false;
            }
            updateHighlightToCheck = false;
        }

        ribbonsToCheck.Clear();
        if (shouldSelect) {
            foreach (int[] d in districtsToCheck) {
                foreach (Atom a in viz.districts[d[0], d[1], d[2]].atoms_segment)
                    ribbonsToCheck.Add(a);
            }
        }
        
        if (HighlightToCheck) {
            Color32 yellow = new Color32(255, 240, 20, 255);
            foreach (Atom a in ribbonsToCheck) {
                a.shouldHighlight = true;
                a.highlightColor = yellow;
            }
        }

        if (HighlightSelected || updateHighlightSelected) {
            foreach (Atom a in selectedRibbons) {
                a.shouldHighlight = false;
            }
            updateHighlightToCheck = false;
        }

        if (shouldClearSelected) {
            selectedRibbons.Clear();
        }
        if (shouldSelect) {
            AddToSelectedRibbons();
        }
        
        if (HighlightSelected) {
            Color32 green = new Color32(0, 255, 0, 255);
            foreach (Atom a in selectedRibbons) {
                a.shouldHighlight = true;
                a.highlightColor = green;
            }
        }
	}
}
