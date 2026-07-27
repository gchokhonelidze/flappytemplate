using System;
using UnityEngine;

public class ToastExample : MonoBehaviour
{
	void Update()
	{
		if (Input.GetKeyDown(KeyCode.T))
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
