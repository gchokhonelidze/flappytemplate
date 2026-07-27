#nullable enable
using Newtonsoft.Json.Linq;

namespace FlappyTemplate
{
	public class PayloadDto
	{
		public string EventName { get; set; } = null!;
		public JToken Data { get; set; } = null!;
	}

	public class PayloadGroupElementDto
	{
		public string E { get; set; } = null!;
		public JToken O { get; set; } = null!;
	}

	public class PayloadGroupDto
	{
		public PayloadGroupElementDto[] Values { get; set; } = null!;
	}
}
