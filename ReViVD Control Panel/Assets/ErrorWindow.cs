using UnityEngine.UI;
using UnityEngine;

public class ErrorWindow : MonoBehaviour
{
#pragma warning disable 0649
    [SerializeField] Button close;
#pragma warning restore 0649
    public Text message;

    private void OnEnable() {
        close.onClick.AddListener(delegate { Destroy(this.gameObject); });
    }

    private void OnDisable() {
        close.onClick.RemoveAllListeners();
    }
}
