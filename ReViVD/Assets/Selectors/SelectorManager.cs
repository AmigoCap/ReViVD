using System;
using System.Collections.Generic;
using UnityEngine;

namespace Revivd {

    [DisallowMultipleComponent]
    public class SelectorManager : MonoBehaviour
    {
        private static SelectorManager _instance;
        public static SelectorManager Instance { get { return _instance; } }

        public bool highlightChecked = false;
        public bool highlightSelected = true;

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
                    handSelectors[(int)oldCurrentColor].enabled = false;

                _currentColor = value;
                colorAlgebraResult = Pow(2, (int)_currentColor);

                if (handSelectors[(int)_currentColor] != null)
                    handSelectors[(int)_currentColor].enabled = true;
                oldCurrentColor = _currentColor;
            }
        }

        public int Pow(int num, int exp) {
            return exp == 0 ? 1 : num * Pow(num, exp - 1);
        }

        public int colorAlgebraResult = 0;

        private void DisplayOnlySelected(SteamVR_TrackedController sender) {
            foreach (Path p in Visualization.Instance.PathsAsBase) {
                foreach (Atom a in p.AtomsAsBase)
                    a.ShouldDisplay = false;
            }

            for (int i = 0; i < colors.Length; i++) {
                if ((colorAlgebraResult & Pow(2, i)) != 0) {
                    foreach (Atom a in selectedRibbons[i])
                        a.ShouldDisplay = true;
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
            for (int i = 0; i < colors.Length; i++) {
                if ((colorAlgebraResult & Pow(2, i)) != 0) {
                    if (highlightSelected) {
                        foreach (Atom a in selectedRibbons[i])
                            a.ShouldHighlightBecauseSelected(i, false);
                    }
                    selectedRibbons[i].Clear();
                }
            }
        }

        private void OnEnable() {
            SteamVR_ControllerManager.RightController.PadClicked += DisplayOnlySelected;

            SteamVR_ControllerManager.LeftController.PadClicked += DisplayAll;
            SteamVR_ControllerManager.LeftController.TriggerClicked += ClearSelected;
        }

        private void OnDisable() {
            if (SteamVR_ControllerManager.RightController != null) {
                SteamVR_ControllerManager.RightController.PadClicked -= DisplayOnlySelected;
            }
            if (SteamVR_ControllerManager.LeftController != null) {
                SteamVR_ControllerManager.LeftController.PadClicked -= DisplayAll;
                SteamVR_ControllerManager.LeftController.TriggerClicked -= ClearSelected;
            }

            foreach (SelectorPart p in GetComponents<SelectorPart>())
                p.enabled = false;
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
            colorAlgebraResult = Pow(2, (int)_currentColor);
        }

        private void Update() {
            CurrentColor = _currentColor;
        }
    }

}