using UnityEngine;

public static class TransformExtensions
{
	public static void DestroyAllChildren(this Transform parent)
	{
		for (int i = parent.childCount - 1; i >= 0; i--)
		{
			Object.Destroy(parent.GetChild(i).gameObject);
		}
	}

	public static void DestroyChildrenWith<T>(this Transform parent)
		where T : Component
	{
		for (int i = parent.childCount - 1; i >= 0; i--)
		{
			var child = parent.GetChild(i);

			if (child.TryGetComponent<T>(out _))
			{
				Object.Destroy(child.gameObject);
			}
		}
	}
}
