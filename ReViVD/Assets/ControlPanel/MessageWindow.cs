using UnityEngine.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Revivd {
    public class MessageWindow : MonoBehaviour {
#pragma warning disable 0649
        [SerializeField] Button close;
#pragma warning restore 0649
        public Text title;
        public Text message;

        private void OnEnable() {
            close.onClick.AddListener(delegate { Destroy(this.gameObject); });
            oldMousePos = Input.mousePosition;
        }

        private void OnDisable() {
            close.onClick.RemoveAllListeners();
        }

        bool dragging = false;
        Vector2 oldMousePos;
        public void StartDragging() {
            dragging = true;
            oldMousePos = Input.mousePosition;
        }

        public void StopDragging() {
            dragging = false;
        }

        private void Update() {
            if (dragging) {
                Vector2 newMousePos = Input.mousePosition;
                GetComponent<RectTransform>().anchoredPosition += newMousePos - oldMousePos;
                oldMousePos = newMousePos;
            }
        }
    }
}