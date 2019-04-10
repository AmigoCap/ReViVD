using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectorManager : MonoBehaviour {
    private static SelectorManager _instance;

    public static SelectorManager Instance { get { return _instance; } }

    public HashSet<Selector> selectors = new HashSet<Selector>();

    public GameObject LeftHand { get; set; }
    public GameObject RightHand { get; set; }
    public Visualization Viz { get; set; }

    public bool shouldSelect = false;
    public bool highlightChecked = false;
    private bool old_highightChecked = false;
    public bool highlightSelected = true;
    private bool old_highlightSelected = true;

    private void Awake() {
        if (_instance != null && _instance != this) {
            Destroy(this.gameObject);
        }
        else {
            _instance = this;
        }
    }

    private bool isInitialized;
    public void initialize() {
        if (!isInitialized) {
            LeftHand = GameObject.Find("leftHand");
            RightHand = GameObject.Find("rightHand");
            Viz = GameObject.Find("airTraffic").GetComponent<Visualization>();
            isInitialized = true;
        }
    }

    private bool Grip_Right_old = false;
    private bool Grip_Left_old = false;

    private void Update() {
        shouldSelect = false;
        bool shouldClearSelected = false;
        if (Input.GetButton("Trig_Right")) {
            shouldSelect = true;
        }
        else if (Input.GetButtonDown("Trig_Left")) {
            shouldClearSelected = true;
        }

        bool Grip_Right_new = Input.GetAxis("Grip_Right") > 0.5; //Workaround : les input "boutons" pour les boutons de grip ne marchent pas.
        bool Grip_Left_new = Input.GetAxis("Grip_Left") > 0.5;

        if (Grip_Right_new && !Grip_Right_old) {
            HashSet<Path> selectedPaths = new HashSet<Path>();
            foreach (Atom a in Viz.selectedRibbons) {
                selectedPaths.Add(a.path);
            }

            foreach (Path p in Viz.PathsAsBase) {
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
            Viz.needsFullRenderingUpdate = true;
        }
        else if (Grip_Left_new && !Grip_Left_old) {
            foreach (Path p in Viz.PathsAsBase) {
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

            Viz.needsFullRenderingUpdate = true;
        }

        Grip_Right_old = Grip_Right_new;
        Grip_Left_old = Grip_Left_new;

        foreach (Selector s in selectors) {
            s.UpdateGeometry();
            s.districtsToCheck.Clear();
            if (shouldSelect) {
                s.FindDistrictsToCheck();
            }
        }

        if (highlightChecked || old_highightChecked) {
            foreach (Selector s in selectors) {
                foreach (Atom a in s.ribbonsToCheck) {
                    a.ShouldHighlight = false;
                }
            }
        }


        foreach (Selector s in selectors) {
            s.ribbonsToCheck.Clear();
            if (shouldSelect) {
                foreach (int[] d in s.districtsToCheck) {
                    foreach (Atom a in Viz.districts[d[0], d[1], d[2]].atoms_segment)
                        s.ribbonsToCheck.Add(a);
                }
            }
        }


        if (highlightChecked) {
            Color32 yellow = new Color32(255, 240, 20, 255);
            foreach (Selector s in selectors) {
                foreach (Atom a in s.ribbonsToCheck) {
                    a.ShouldHighlight = true;
                    a.highlightColor = yellow;
                }
            }
        }

        if (highlightSelected || old_highlightSelected) {
            foreach (Atom a in Viz.selectedRibbons) {
                a.ShouldHighlight = false;
            }
        }

        if (shouldClearSelected) {
            Viz.selectedRibbons.Clear();
        }

        foreach (Selector s in selectors) {
            if (shouldSelect) {
                s.AddToSelectedRibbons();
            }
        }

        if (highlightSelected) {
            Color32 green = new Color32(0, 255, 0, 255);
            foreach (Atom a in Viz.selectedRibbons) {
                a.ShouldHighlight = true;
                a.highlightColor = green;
            }
        }

        if (highlightSelected != old_highlightSelected || highlightChecked != old_highightChecked || shouldClearSelected) {
            Viz.needsFullRenderingUpdate = true;
        }

        old_highightChecked = highlightChecked;
        old_highlightSelected = highlightSelected;
    }
}

public abstract class Selector : MonoBehaviour {
    public HashSet<int[]> districtsToCheck = new HashSet<int[]>(new CoordsEqualityComparer());
    public HashSet<Atom> ribbonsToCheck = new HashSet<Atom>();

    protected abstract void CreateObjects();

    public abstract void UpdateGeometry();

    public abstract void FindDistrictsToCheck();

    public abstract void AddToSelectedRibbons();

    protected virtual void OnEnable() {
        SelectorManager.Instance.initialize();
        SelectorManager.Instance.selectors.Add(this);
        CreateObjects();
    }

    protected virtual void OnDisable() {
        for (int i = 0; i < transform.childCount; i++)
            Destroy(transform.GetChild(i).gameObject);
        SelectorManager.Instance.selectors.Remove(this);
    }
}