using UnityEngine;
using UnityEngine.Events;

namespace FlappyTemplate
{
    public class UiManager : MonoBehaviour
    {
        public Toast panelToast;
        public static UiManager Inst { get; private set; } = null!;
        public UnityEvent<ToastOptions> OnShowToast = new();
        public UnityEvent OnHideToast = new();

        void Awake()
        {
            Inst = this;
            OnShowToast.AddListener(panelToast.Show);
            OnHideToast.AddListener(panelToast.Hide);
        }

        void OnDestroy()
        {
            OnShowToast.RemoveAllListeners();
            OnHideToast.RemoveAllListeners();
        }
    }
}
