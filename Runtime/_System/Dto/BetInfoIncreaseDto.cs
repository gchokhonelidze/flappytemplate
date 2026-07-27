#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlappyTemplate
{
	public class BetInfoIncreaseDto
	{
		public string Id { get; set; } = string.Empty; //related transaction's id
		public string BetId { get; set; } = string.Empty;
		public string Amount { get; set; } = "0";
	}
}
