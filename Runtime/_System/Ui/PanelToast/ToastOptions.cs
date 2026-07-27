#nullable enable
using System;
using UnityEngine;

public enum EToastType
{
	Default,
	Error,
	Warning,
	Success
}

public class ToastOptions
{
	public string? Title;
	public EToastType Type = EToastType.Default;
	public string Message = string.Empty;
	public TimeSpan HideAfter = TimeSpan.FromSeconds(3);
	public TimeSpan AnimationDuration = TimeSpan.FromSeconds(0.5);
	public bool ShowCloseButton = true;
	public bool AutoHide = true;
}
