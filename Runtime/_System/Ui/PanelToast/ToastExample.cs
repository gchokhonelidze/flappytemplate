using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlappyTemplate
{
    public class ToastExample : MonoBehaviour
    {
        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.tKey.wasPressedThisFrame)
            {
                UiManager.Inst.OnShowToast.Invoke(
                    new ToastOptions
                    {
                        Message = "Hello, World!",
                        HideAfter = TimeSpan.FromSeconds(5),
                        AnimationDuration = TimeSpan.FromSeconds(0.5),
                        ShowCloseButton = true,
                        AutoHide = false,
                        Title = "This is a toast message",
                        Type = EToastType.Success
                    }
                );
            }
        }
    }
}
