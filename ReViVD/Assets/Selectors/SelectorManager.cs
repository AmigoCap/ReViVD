using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Revivd {

    public class SelectorManager : MonoBehaviour {
        private static SelectorManager _instance;

        public static SelectorManager Instance { get { return _instance; } }

        public HashSet<Selector> selectors = new HashSet<Selector>();

        public Visualization Viz { get; set; }

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
                Viz = GameObject.Find("airTraffic").GetComponent<Visualization>();
                isInitialized = true;
            }
        }

        private void DisplayOnlySelected(SteamVR_TrackedController sender) {
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

        private void DisplayAll(SteamVR_TrackedController sender) {
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

        private void ClearSelected(SteamVR_TrackedController sender) {
            if (Viz.selectedRibbons.Count != 0) {
                if (highlightSelected) {
                    foreach (Atom a in Viz.selectedRibbons) {
                        a.ShouldHighlight = false;
                    }
                }
                Viz.selectedRibbons.Clear();
                Viz.needsFullRenderingUpdate = true;
            }
        }

        private void OnEnable() {
            SteamVR_ControllerManager.RightController.Gripped += DisplayOnlySelected;
            SteamVR_ControllerManager.LeftController.Gripped += DisplayAll;
            SteamVR_ControllerManager.LeftController.TriggerClicked += ClearSelected;
        }

        private void OnDisable() {
            SteamVR_ControllerManager.RightController.Gripped -= DisplayOnlySelected;
            SteamVR_ControllerManager.LeftController.Gripped -= DisplayAll;
            SteamVR_ControllerManager.LeftController.TriggerClicked -= ClearSelected;
        }

        private bool ShouldSelect {
            get {
                return SteamVR_ControllerManager.RightController.triggerPressed;
            }
        }

        private void Update() {
            foreach (Selector s in selectors) {
                s.UpdateGeometry();
                s.districtsToCheck.Clear();
                if (ShouldSelect) {
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
                if (ShouldSelect) {
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

            foreach (Selector s in selectors) {
                if (ShouldSelect) {
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

            if (highlightSelected != old_highlightSelected || highlightChecked != old_highightChecked) {
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

}