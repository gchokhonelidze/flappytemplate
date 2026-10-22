using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlappyTemplate
{
    // One key per kind, through the same event a game would raise. Drop it anywhere in a scene with a
    // FlappyCanvas in it and press play: 1 is a success, 2 a warning, 3 an error, 4 the plain kind, and T a
    // titled one with a close cross that stays until it is closed.
    public class ToastExample : MonoBehaviour
    {
        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || UiManager.Inst == null)
                return;

            if (keyboard.digit1Key.wasPressedThisFrame)
                Show(EToastType.Success, "Bet placed");
            else if (keyboard.digit2Key.wasPressedThisFrame)
                Show(EToastType.Warning, "Balance is running low");
            else if (keyboard.digit3Key.wasPressedThisFrame)
                Show(EToastType.Error, "The bet was not accepted");
            else if (keyboard.digit4Key.wasPressedThisFrame)
                Show(EToastType.Default, "Placeholder");
            else if (keyboard.tKey.wasPressedThisFrame)
            {
                UiManager.Inst.OnShowToast.Invoke(new ToastOptions
                {
                    Title = "Connection lost",
                    Message = "Trying again. Anything you bet while offline will not be placed, so wait for the line to come back before playing on.",
                    Type = EToastType.Error,
                    ShowCloseButton = true,
                    AutoHide = false,
                });
            }
        }

        private static void Show(EToastType kind, string message)
        {
            UiManager.Inst.OnShowToast.Invoke(new ToastOptions
            {
                Message = message,
                Type = kind,
                HideAfter = TimeSpan.FromSeconds(3),
            });
        }
    }
}
