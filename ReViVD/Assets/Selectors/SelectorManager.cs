using System;
using System.Collections.Generic;
using UnityEngine;

namespace Revivd {

    [DisallowMultipleComponent]
    public class SelectorManager : MonoBehaviour
    {
        private static SelectorManager _instance;
        public static SelectorManager Instance { get { return _instance; } }

        private bool oldHighlightChecked;
        [SerializeField]
        private bool _highlightChecked = false;
        public bool HighlightChecked {
            get => _highlightChecked;
            set {
                if (_highlightChecked == value && _highlightChecked == oldHighlightChecked)
                    return;

                _highlightChecked = value;
                oldHighlightChecked = value;

                for (int i = 0; i < colors.Length; i++) {
                    if (handSelectors[i] != null)
                        foreach (SelectorPart p in handSelectors[i].GetComponents<SelectorPart>())
                            foreach (Atom a in p.ribbonsToCheck)
                                a.ShouldHighlightBecauseChecked(i, _highlightChecked);
                }
            }
        }

        private bool oldHighlightSelected;
        [SerializeField]
        private bool _highlightSelected = true;
        public bool HighlightSelected {
            get => _highlightSelected;
            set {
                if (_highlightSelected == value && _highlightSelected == oldHighlightSelected)
                    return;

                _highlightSelected = value;
                oldHighlightSelected = value;

                for (int i = 0; i < colors.Length; i++) {
                    foreach (Atom a in selectedRibbons[i])
                        a.ShouldHighlightBecauseSelected(i, _highlightSelected);
                }
            }
        }

        public enum ColorGroup { Red = 0, Green, Blue, Yellow, Cyan, Magenta };
        public static readonly Color[] colors = { Color.red, Color.green, Color.blue, Color.yellow, Color.cyan, Color.magenta };
        public static readonly Color[] colors_dark = new Color[colors.Length];
        public Selector[] handSelectors = new Selector[colors.Length];
        public List<Selector>[] persistentSelectors = new List<Selector>[colors.Length];
        public HashSet<Atom>[] selectedRibbons = new HashSet<Atom>[colors.Length];

        private ColorGroup oldCurrentColor;
        [SerializeField]
        private ColorGroup _currentColor = ColorGroup.Red;
        public ColorGroup CurrentColor {
            get => _currentColor;
            set {
                if (_currentColor == value && _currentColor == oldCurrentColor)
                    return;

                if (handSelectors[(int)oldCurrentColor] != null)
                    handSelectors[(int)oldCurrentColor].Shown = false;

                _currentColor = value;
                oldCurrentColor = _currentColor;

                if (!lockOperatingColors)
                    operatingColors = new ColorGroup[] { _currentColor };

                if (handSelectors[(int)_currentColor] != null)
                    handSelectors[(int)_currentColor].Shown = true;
            }
        }

        public int Pow(int num, int exp) {
            return exp == 0 ? 1 : num * Pow(num, exp - 1);
        }

        public ColorGroup[] operatingColors;
        public bool lockOperatingColors = false;

        private bool ShouldHandSelect {
            get => SteamVR_ControllerManager.RightController.triggerPressed;
        }

        private void SelectWithPersistents(SteamVR_TrackedController sender) {
            foreach (Selector s in persistentSelectors[(int)CurrentColor])
                s.Select();
        }

        private void MakePersistentCopyOfHand(SteamVR_TrackedController sender) {
            Selector s = handSelectors[(int)CurrentColor];
            if (s != null && s.isActiveAndEnabled) {
                GameObject go = Instantiate(s.gameObject, this.transform);
                go.name = "Persistent " + s.name;
                go.GetComponent<Selector>().Persistent = true;
            }                
        }

        private void DisplayOnlySelected(SteamVR_TrackedController sender) {
            foreach (Path p in Visualization.Instance.PathsAsBase) {
                foreach (Atom a in p.AtomsAsBase)
                    a.ShouldDisplay = false;
            }

            foreach (ColorGroup c in operatingColors) {
                foreach (Atom a in selectedRibbons[(int)c])
                    a.ShouldDisplay = true;
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
            if (HighlightSelected) {
                foreach (Atom a in selectedRibbons[(int)CurrentColor])
                    a.ShouldHighlightBecauseSelected((int)CurrentColor, false);
            }
            selectedRibbons[(int)CurrentColor].Clear();
        }

        private void OnEnable() {
            SteamVR_ControllerManager.RightController.PadClicked += DisplayOnlySelected;
            SteamVR_ControllerManager.RightController.Gripped += SelectWithPersistents;
            SteamVR_ControllerManager.RightController.MenuButtonClicked += MakePersistentCopyOfHand;

            SteamVR_ControllerManager.LeftController.PadClicked += DisplayAll;
            SteamVR_ControllerManager.LeftController.TriggerClicked += ClearSelected;
        }

        private void OnDisable() {
            if (SteamVR_ControllerManager.RightController != null) {
                SteamVR_ControllerManager.RightController.PadClicked -= DisplayOnlySelected;
                SteamVR_ControllerManager.RightController.Gripped -= SelectWithPersistents;
                SteamVR_ControllerManager.RightController.MenuButtonClicked -= MakePersistentCopyOfHand;
            }
            if (SteamVR_ControllerManager.LeftController != null) {
                SteamVR_ControllerManager.LeftController.PadClicked -= DisplayAll;
                SteamVR_ControllerManager.LeftController.TriggerClicked -= ClearSelected;
            }
        }

        private void Awake() {
            if (_instance != null) {
                Debug.LogWarning("Multiple instances of selector manager singleton");
            }
            _instance = this;

            for (int i = 0; i < colors.Length; i++) {
                colors_dark[i] = colors[i] / 3;
                persistentSelectors[i] = new List<Selector>();
                selectedRibbons[i] = new HashSet<Atom>();
            }

            oldCurrentColor = _currentColor;
            oldHighlightChecked = _highlightChecked;
            oldHighlightSelected = _highlightSelected;

            operatingColors = new ColorGroup[] { _currentColor };
        }

        private void Update() {
            CurrentColor = _currentColor; //Triggers the property if _currentColor was changed in the editor
            HighlightChecked = _highlightChecked;
            HighlightSelected = _highlightSelected;

            if (!ShouldHandSelect)
                foreach (Selector s in handSelectors)
                    if (s != null && s.needsCheckedHighlightCleanup)
                        foreach (SelectorPart p in s.GetComponents<SelectorPart>())
                            foreach (Atom a in p.ribbonsToCheck)
                                a.ShouldHighlightBecauseChecked((int)s.Color, false);


            Selector hs = handSelectors[(int)CurrentColor];
            if (hs != null && hs.isActiveAndEnabled) {
                foreach (SelectorPart p in hs.GetComponents<SelectorPart>())
                    if (p.enabled)
                        p.UpdatePrimitive();
                if (ShouldHandSelect)
                    hs.Select();
            }

            foreach (List<Selector> L in persistentSelectors)
                foreach (Selector ps in L)
                    if (ps.isActiveAndEnabled)
                        foreach (SelectorPart p in ps.GetComponents<SelectorPart>())
                            if (p.enabled)
                                p.UpdatePrimitive();
        }
    }

}