using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Revivd {

    [DisallowMultipleComponent]
    public class Selector : MonoBehaviour {
        public bool highlightChecked = false;
        public bool highlightSelected = true;

        public bool inverse = false;
        public bool erase = false;

        private bool old_persistent;
        [SerializeField]
        private bool _persistent = false;
        public bool Persistent {
            get => _persistent;
            set {
                if (value == _persistent && _persistent == old_persistent) //On rafraîchit la valeur si elle a été changée dans l'éditeur
                    return;
                if (_persistent) {
                    SelectorManager.Instance.persistentSelectors[(int)_color].Remove(this);
                }
                else if (SelectorManager.Instance.handSelectors[(int)_color] == this)
                    SelectorManager.Instance.handSelectors[(int)_color] = null;
                _persistent = value;
                if (_persistent) {
                    SelectorManager.Instance.persistentSelectors[(int)_color].Add(this);
                    highlightChecked = false;
                    foreach (SelectorPart p in GetComponents<SelectorPart>())
                        if (p.enabled)
                            p.Detach();
                }
                else {
                    SelectorManager.Instance.handSelectors[(int)_color] = this;
                    foreach (SelectorPart p in GetComponents<SelectorPart>())
                        if (p.enabled)
                            p.Attach();
                }
                old_persistent = _persistent;
            }
        }

        private SelectorManager.ColorGroup old_color;
        [SerializeField]
        private SelectorManager.ColorGroup _color = 0;
        public SelectorManager.ColorGroup Color {
            get => _color;
            set {
                if (value == _color && _color == old_color)
                    return;
                if (_persistent)
                    SelectorManager.Instance.persistentSelectors[(int)_color].Remove(this);
                else
                    SelectorManager.Instance.handSelectors[(int)_color] = null;
                _color = value;
                if (_persistent)
                    SelectorManager.Instance.persistentSelectors[(int)_color].Add(this);
                else
                    SelectorManager.Instance.handSelectors[(int)_color] = this;

                foreach (SelectorPart p in GetComponents<SelectorPart>())
                    p.UpdateColor();
                old_color = _color;
            }
        }

        private void DisplayOnlySelected(SteamVR_TrackedController sender) {
            HashSet<Path> selectedPaths = new HashSet<Path>();
            foreach (Atom a in SelectorManager.Instance.selectedRibbons[(int)Color]) {
                selectedPaths.Add(a.path);
            }

            foreach (Path p in Visualization.Instance.PathsAsBase) {
                if (!selectedPaths.Contains(p)) {
                    foreach (Atom a in p.AtomsAsBase) {
                        a.ShouldDisplay = false;
                    }
                }
            }
        }

        private void DisplayAll(SteamVR_TrackedController sender) {
            foreach (Path p in Visualization.Instance.PathsAsBase) {
                foreach (Atom a in p.AtomsAsBase) {
                    a.ShouldDisplay = true;
                }
            }
        }

        private void ClearSelected(SteamVR_TrackedController sender) {
            HashSet<Atom> selectedRibbons = SelectorManager.Instance.selectedRibbons[(int)Color];

            if (selectedRibbons.Count != 0) {
                if (highlightSelected) {
                    foreach (Atom a in selectedRibbons) {
                        a.ShouldHighlightBecauseSelected(Color, false);
                    }
                }
                selectedRibbons.Clear();
            }
        }

        private void OnEnable() {
            SteamVR_ControllerManager.RightController.Gripped += Select;
            SteamVR_ControllerManager.RightController.PadClicked += DisplayOnlySelected;
            SteamVR_ControllerManager.RightController.MenuButtonClicked += MakePersistentCopy;

            SteamVR_ControllerManager.LeftController.PadClicked += DisplayAll;
            SteamVR_ControllerManager.LeftController.TriggerClicked += ClearSelected;
        }

        private void OnDisable() {
            if (SteamVR_ControllerManager.RightController != null) {
                SteamVR_ControllerManager.RightController.Gripped -= Select;
                SteamVR_ControllerManager.RightController.PadClicked -= DisplayOnlySelected;
                SteamVR_ControllerManager.RightController.MenuButtonClicked -= MakePersistentCopy;
            }
            if (SteamVR_ControllerManager.LeftController != null) {
                SteamVR_ControllerManager.LeftController.PadClicked -= DisplayAll;
                SteamVR_ControllerManager.LeftController.TriggerClicked -= ClearSelected;
            }
        }

        private bool ShouldSelect {
            get {
                return SteamVR_ControllerManager.RightController.triggerPressed;
            }
        }

        private List<SelectorPart> parts = new List<SelectorPart>();
        private HashSet<Atom> handledRibbons = new HashSet<Atom>();
        private bool needsCheckedHighlightCleanup = false;

        private void Select(SteamVR_TrackedController sender) {
            if (sender != null && !Persistent) //Handheld selectors are only operated in the update loop (not as events)
                return;

            Visualization viz = Visualization.Instance;
            HashSet<Atom> selectedRibbons = SelectorManager.Instance.selectedRibbons[(int)Color];

            foreach (SelectorPart p in parts) {
                p.districtsToCheck.Clear();
                p.FindDistrictsToCheck();

                foreach (Atom a in p.ribbonsToCheck)
                    a.ShouldHighlightBecauseChecked(Color, false);
                p.ribbonsToCheck.Clear();

                foreach (int[] c in p.districtsToCheck) {
                    if (viz.districts.TryGetValue(c, out Visualization.District d)) {
                        foreach (Atom a in d.atoms_segment) {
                            if (a.ShouldDisplay) {
                                p.ribbonsToCheck.Add(a);
                                if (highlightChecked) {
                                    a.ShouldHighlightBecauseChecked(Color, true);
                                }
                            }
                        }
                    }
                }

                p.touchedRibbons.Clear();
                p.FindTouchedRibbons();
            }

            handledRibbons.Clear();

            foreach (SelectorPart p in parts) {
                if (p.Positive) {
                    foreach (Atom a in p.touchedRibbons)
                        handledRibbons.Add(a);
                }
                else {
                    foreach (Atom a in p.touchedRibbons)
                        handledRibbons.Remove(a);
                }
            }

            if (inverse) { //Very inefficient code for now, may need an in-depth restructuration of the Viz/Path/Atom architecture
                List<Atom> allRibbons = new List<Atom>();
                foreach (Path p in viz.PathsAsBase) {
                    allRibbons.AddRange(p.AtomsAsBase);
                }
                HashSet<Atom> inversed = new HashSet<Atom>(allRibbons);
                inversed.ExceptWith(handledRibbons);

                if (erase)
                    selectedRibbons.ExceptWith(inversed);
                else
                    selectedRibbons.UnionWith(inversed);
            }
            else {
                if (erase)
                    selectedRibbons.ExceptWith(handledRibbons);
                else
                    selectedRibbons.UnionWith(handledRibbons);
            }

            if (highlightSelected) {
                foreach (Atom a in selectedRibbons) {
                    a.ShouldHighlightBecauseSelected(Color, true);
                }
            }

            if (highlightChecked)
                needsCheckedHighlightCleanup = true;
        }

        private void MakePersistentCopy(SteamVR_TrackedController sender) {
            if (Persistent)
                return;
            GameObject go = Instantiate(this.gameObject);
            go.transform.parent = SelectorManager.Instance.transform;
            go.GetComponent<Selector>().Persistent = true;
        }

        private void Awake() {
            old_color = Color;
            old_persistent = Persistent;
        }

        private void Update() {
            Color = _color; //Une update se fera si nécessaire (couleur changée dans l'éditeur)
            Persistent = _persistent; //idem

            GetComponents(parts);
            parts.RemoveAll(p => p.isActiveAndEnabled == false);

            foreach (SelectorPart p in parts) {
                p.UpdatePrimitive();
                if (!ShouldSelect && needsCheckedHighlightCleanup) {
                    foreach (Atom a in p.ribbonsToCheck)
                        a.ShouldHighlightBecauseChecked(Color, false);
                }
            }

            if (ShouldSelect && !Persistent)
                Select(null);
        }
    }
}