using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Revivd {

    [DisallowMultipleComponent]
    public class SelectorManager : MonoBehaviour
    {
        private static SelectorManager _instance;
        public static SelectorManager Instance { get { return _instance; } }

        public enum ColorGroup { Red = 0, Green, Blue, Yellow, Cyan, Magenta };
        public static readonly Color[] colors = { Color.red, Color.green, Color.blue, Color.yellow, Color.cyan, Color.magenta };
        public static readonly Color[] colors_dark = new Color[colors.Length];
        public Selector[] handSelectors = new Selector[colors.Length];
        public List<Selector>[] persistentSelectors = new List<Selector>[colors.Length];
        public HashSet<Atom>[] selectedRibbons = new HashSet<Atom>[colors.Length];

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
        }
    }

}