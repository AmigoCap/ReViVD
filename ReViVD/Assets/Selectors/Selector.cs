using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Revivd {

    [DisallowMultipleComponent]
    public class Selector : MonoBehaviour {
        public bool highlightChecked = false;
        private bool old_highightChecked = false;
        public bool highlightSelected = true;
        private bool old_highlightSelected = true;

        public bool inverse = false;
        public bool erase = false;

        private void DisplayOnlySelected(SteamVR_TrackedController sender) {
            HashSet<Path> selectedPaths = new HashSet<Path>();
            foreach (Atom a in Visualization.Instance.selectedRibbons) {
                selectedPaths.Add(a.path);
            }

            foreach (Path p in Visualization.Instance.PathsAsBase) {
                if (!selectedPaths.Contains(p)) {
                    foreach (Atom a in p.AtomsAsBase) {
                        a.ShouldDisplay = false;
                    }
                }
            }
            Visualization.Instance.needsFullVerticesUpdate = true;
        }

        private void DisplayAll(SteamVR_TrackedController sender) {
            foreach (Path p in Visualization.Instance.PathsAsBase) {
                foreach (Atom a in p.AtomsAsBase) {
                    a.ShouldDisplay = true;
                }
            }

            Visualization.Instance.needsFullVerticesUpdate = true;
        }

        private void ClearSelected(SteamVR_TrackedController sender) {
            if (Visualization.Instance.selectedRibbons.Count != 0) {
                if (highlightSelected) {
                    foreach (Atom a in Visualization.Instance.selectedRibbons) {
                        a.ShouldHighlight = false;
                    }
                }
                Visualization.Instance.selectedRibbons.Clear();
                Visualization.Instance.needsFullVerticesUpdate = true;
            }
        }

        private void OnEnable() {
            SteamVR_ControllerManager.RightController.Gripped += DisplayOnlySelected;
            SteamVR_ControllerManager.LeftController.Gripped += DisplayAll;
            SteamVR_ControllerManager.LeftController.TriggerClicked += ClearSelected;
        }

        private void OnDisable() {
            if (SteamVR_ControllerManager.RightController != null)
                SteamVR_ControllerManager.RightController.Gripped -= DisplayOnlySelected;
            if (SteamVR_ControllerManager.LeftController != null) {
                SteamVR_ControllerManager.LeftController.Gripped -= DisplayAll;
                SteamVR_ControllerManager.LeftController.TriggerClicked -= ClearSelected;
            }
        }

        private bool ShouldSelect {
            get {
                return SteamVR_ControllerManager.RightController.triggerPressed;
            }
        }

        private void Update() {
            SelectorPart[] parts = GetComponents<SelectorPart>();

            foreach (SelectorPart s in parts) {
                s.UpdatePrimitive();
                if (ShouldSelect) {
                    s.districtsToCheck.Clear();
                    s.FindDistrictsToCheck();
                }
            }

            if (highlightChecked || old_highightChecked) {
                foreach (SelectorPart s in parts) {
                    foreach (Atom a in s.ribbonsToCheck) {
                        a.ShouldHighlight = false;
                    }
                }
            }


            
            if (ShouldSelect) {
                foreach (SelectorPart s in parts) {
                    s.ribbonsToCheck.Clear();
                    foreach (int[] d in s.districtsToCheck) {
                        foreach (Atom a in Visualization.Instance.districts[d[0], d[1], d[2]].atoms_segment)
                            s.ribbonsToCheck.Add(a);
                    }
                }
            }


            if (highlightChecked) {
                Color32 yellow = new Color32(255, 240, 20, 255);
                foreach (SelectorPart s in parts) {
                    foreach (Atom a in s.ribbonsToCheck) {
                        a.ShouldHighlight = true;
                        a.highlightColor = yellow;
                    }
                }
            }

            if (highlightSelected || old_highlightSelected) {
                foreach (Atom a in Visualization.Instance.selectedRibbons) {
                    a.ShouldHighlight = false;
                }
            }

            HashSet<Atom> handledRibbons = new HashSet<Atom>();

            if (ShouldSelect) {
                foreach (SelectorPart s in parts) {
                    s.touchedRibbons.Clear();
                    s.FindTouchedRibbons();
                    if (s.Positive) {
                        foreach (Atom a in s.touchedRibbons) {
                            handledRibbons.Add(a);
                        }
                    }
                    else {
                        foreach (Atom a in s.touchedRibbons) {
                            handledRibbons.Remove(a);
                        }
                    }
                }

                
                if (inverse) { //Very inefficient code for now, needs an in-depth restructuration of the Viz/Path/Atom architecture
                    List<Atom> allRibbons = new List<Atom>();
                    foreach (Path p in Visualization.Instance.PathsAsBase) {
                        allRibbons.AddRange(p.AtomsAsBase);
                    }
                    HashSet<Atom> inversed = new HashSet<Atom>(allRibbons);
                    inversed.ExceptWith(handledRibbons);

                    if (erase)
                        Visualization.Instance.selectedRibbons.ExceptWith(inversed);
                    else
                        Visualization.Instance.selectedRibbons.UnionWith(inversed);
                }
                else {
                    if (erase)
                        Visualization.Instance.selectedRibbons.ExceptWith(handledRibbons);
                    else
                        Visualization.Instance.selectedRibbons.UnionWith(handledRibbons);
                }
            }

            if (highlightSelected) {
                Color32 green = new Color32(0, 255, 0, 255);
                foreach (Atom a in Visualization.Instance.selectedRibbons) {
                    a.ShouldHighlight = true;
                    a.highlightColor = green;
                }
            }

            if (highlightSelected != old_highlightSelected || highlightChecked != old_highightChecked) {
                Visualization.Instance.needsFullVerticesUpdate = true;
            }

            old_highightChecked = highlightChecked;
            old_highlightSelected = highlightSelected;
        }
    }
}