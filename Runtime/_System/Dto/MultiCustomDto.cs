#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlappyTemplate
{
	public class MultiCustomDto
	{
		public string TurnId { get; set; } = string.Empty;
		public string EventName { get; set; } = string.Empty;
		public object CustomData { get; set; } = new object();
		public MultiBetDto[]? Bets { get; set; } = null;
	}
}
