using System.Collections.Generic;
using UnityEngine;

namespace Revivd {

    [DisallowMultipleComponent]
    public class SelectorManager : MonoBehaviour
    {
        private static SelectorManager _instance;
        public static SelectorManager Instance { get { return _instance; } }

        public float sizeExponent = 1.3f;
        public enum ControlMode { SelectMode, CreatorMode};
        private ControlMode _currentControlMode = ControlMode.SelectMode;
        public ControlMode CurrentControlMode {
            get => _currentControlMode;
            set => _currentControlMode = value;
        }

        [SerializeField]
        private bool s_highlightChecked = false;
        private bool _highlightChecked;
        public bool HighlightChecked {
            get => _highlightChecked;
            set {
                if (_highlightChecked == value)
                    return;

                _highlightChecked = value;
                s_highlightChecked = _highlightChecked;
            }
        }

        [SerializeField]
        private bool s_highlightSelected = true;
        private bool _highlightSelected;
        public bool HighlightSelected {
            get => _highlightSelected;
            set {
                if (_highlightSelected == value)
                    return;

                _highlightSelected = value;
                s_highlightSelected = _highlightSelected;

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

        [SerializeField]
        private ColorGroup s_currentColor = ColorGroup.Red;
        private ColorGroup _currentColor;
        public ColorGroup CurrentColor {
            get => _currentColor;
            set {
                if (_currentColor == value)
                    return;

                if (handSelectors[(int)_currentColor] != null)
                    handSelectors[(int)_currentColor].Shown = false;

                _currentColor = value;
                s_currentColor = _currentColor;

                if (handSelectors[(int)_currentColor] != null)
                    handSelectors[(int)_currentColor].Shown = true;
            }
        }

        public int Pow(int num, int exp) {
            return exp == 0 ? 1 : num * Pow(num, exp - 1);
        }

        public HashSet<ColorGroup> operatingColors;

        public bool InverseMode {
            get => SteamVR_ControllerManager.LeftController.triggerPressed;
        }

        public enum LogicMode { AND, OR };

        public LogicMode operationMode = LogicMode.OR;

        private bool ShouldSelect {
            get => SteamVR_ControllerManager.RightController.triggerPressed;
        }
    

        private void SelectWithPersistents(SteamVR_TrackedController sender) {
            if (CurrentControlMode != ControlMode.SelectMode)
                return;

            if (InverseMode) {
                ClearSelected();
                return;
            }
            foreach (Selector s in persistentSelectors[(int)CurrentColor])
                s.Select();
        }

        private void MakePersistentCopyOfHand(SteamVR_TrackedController sender) {
            if (CurrentControlMode != ControlMode.SelectMode)
                return;

            if (InverseMode) {//inverse mode
                int len = persistentSelectors[(int)CurrentColor].Count;
                if (len == 0)
                    return;
                Selector last = persistentSelectors[(int)CurrentColor][len - 1];
                persistentSelectors[(int)CurrentColor].RemoveAt(len - 1);
                Destroy(last.gameObject);
                return;
            }

            Selector s = handSelectors[(int)CurrentColor];
            if (s != null && s.isActiveAndEnabled) {
                GameObject go = Instantiate(s.gameObject, this.transform);

                go.name = "Persistent " + s.name;
                go.GetComponent<Selector>().Persistent = true;

                SelectorPart[] originalParts = s.GetComponents<SelectorPart>();
                SelectorPart[] newParts = go.GetComponents<SelectorPart>();
                for (int i = 0; i < originalParts.Length; i++) {
                    newParts[i].PrimitiveTransform.localRotation = originalParts[i].PrimitiveTransform.localRotation;
                    newParts[i].PrimitiveTransform.localPosition = originalParts[i].PrimitiveTransform.localPosition;
                    newParts[i].PrimitiveTransform.localScale = originalParts[i].PrimitiveTransform.localScale;
                }
            }                
        }

        private void ModeModification(SteamVR_TrackedController sender) {
            if (CurrentControlMode == ControlMode.SelectMode)
                CurrentControlMode = ControlMode.CreatorMode;
            else
                CurrentControlMode = ControlMode.SelectMode;
        }

        public void DoLogicOperation() {
            if (InverseMode) { // inverse mode
                foreach (Path p in Visualization.Instance.PathsAsBase) {
                    foreach (Atom a in p.AtomsAsBase)
                        a.ShouldDisplay = !a.ShouldDisplay;
                }

                return;
            }

            foreach (Path p in Visualization.Instance.PathsAsBase) {
                foreach (Atom a in p.AtomsAsBase)
                    a.ShouldDisplay = false;
            }

            HashSet<Path> pathsToKeep = new HashSet<Path>();
            if (operationMode == LogicMode.AND) {
                HashSet<Atom> ribbonsToKeep = new HashSet<Atom>();
                bool firstPass = true;
                foreach (ColorGroup c in operatingColors) {
                    if (firstPass) {
                        ribbonsToKeep = new HashSet<Atom>(selectedRibbons[(int)c]);
                        firstPass = false;
                    }
                    else
                        ribbonsToKeep.IntersectWith(selectedRibbons[(int)c]);
                }

                foreach (Atom a in ribbonsToKeep)
                    pathsToKeep.Add(a.path);
            }
            else {
                foreach (ColorGroup c in operatingColors) {
                    foreach (Atom a in selectedRibbons[(int)c])
                        pathsToKeep.Add(a.path);
                }
            }

            foreach (Path p in pathsToKeep) {
                foreach (Atom a in p.AtomsAsBase)
                    a.ShouldDisplay = true;
            }

        }

        private void ClearSelected() {
            if (HighlightSelected) {
                foreach (Atom a in selectedRibbons[(int)CurrentColor])
                    a.ShouldHighlightBecauseSelected((int)CurrentColor, false);
            }
            selectedRibbons[(int)CurrentColor].Clear();
        }

        private void OnEnable() {
            SteamVR_ControllerManager.RightController.Gripped += SelectWithPersistents;
            SteamVR_ControllerManager.RightController.MenuButtonClicked += MakePersistentCopyOfHand;
            SteamVR_ControllerManager.LeftController.MenuButtonClicked += ModeModification;
        }

        private void OnDisable() {
            if (SteamVR_ControllerManager.RightController != null) {
                SteamVR_ControllerManager.RightController.Gripped -= SelectWithPersistents;
                SteamVR_ControllerManager.RightController.MenuButtonClicked -= MakePersistentCopyOfHand;
            }

            if (SteamVR_ControllerManager.LeftController != null) {
                SteamVR_ControllerManager.LeftController.MenuButtonClicked -= ModeModification;
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

            _currentColor = s_currentColor;
            _highlightChecked = s_highlightChecked;
            _highlightSelected = s_highlightSelected;

            operatingColors = new HashSet<ColorGroup>();
        }

        private void Update() {
            CurrentColor = s_currentColor; //Triggers the property if _currentColor was changed in the editor
            HighlightChecked = s_highlightChecked;
            HighlightSelected = s_highlightSelected;

            if (!ShouldSelect) { //clean up highlights
                foreach (Selector s in handSelectors)
                    if (s != null && s.needsCheckedHighlightCleanup) {
                        foreach (SelectorPart p in s.GetComponents<SelectorPart>())
                            foreach (Atom a in p.CheckedRibbons)
                                a.ShouldHighlightBecauseChecked((int)s.Color, false);
                        s.needsCheckedHighlightCleanup = false;
                    }
            }
            

            if (CurrentControlMode == ControlMode.SelectMode) { // Selection
                Selector hs = handSelectors[(int)CurrentColor];
                if (hs != null && hs.isActiveAndEnabled) {
                    if (ShouldSelect)
                        hs.Select(InverseMode);
                }
            }
            else { // creation
                Selector hs = handSelectors[(int)CurrentColor];
                if (hs != null && hs.isActiveAndEnabled) {
                    if (SteamVR_ControllerManager.RightController.triggerPressed) {
                        hs.ScaleUp();
                    }
                    if (SteamVR_ControllerManager.LeftController.triggerPressed) {
                        hs.ScaleDown();
                    }
                    hs.UpdatePosition();
                }
            }
        }
    }

}