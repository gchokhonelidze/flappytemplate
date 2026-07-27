#nullable enable

using UnityEngine;

namespace FlappyTemplate
{
	public class StateManager : MonoBehaviour
	{
		public static StateManager Inst { get; private set; } = null!;

		[SerializeField]
		public MainState MainState = new();

		[SerializeField]
		public MultiState MultiState = new();

		// [SerializeField]
		// public MainEvents _Events = new();

		[HideInInspector]
		public MainEvents Events = new();

		void Awake()
		{
			Inst = this;
		}
	}
}
