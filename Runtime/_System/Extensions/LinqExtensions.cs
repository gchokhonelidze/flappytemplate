using System.Collections.Generic;

public static class LinqExtensions
{
	public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> source)
	{
		int index = 0;

		foreach (var item in source)
		{
			yield return (item, index++);
		}
	}
}
