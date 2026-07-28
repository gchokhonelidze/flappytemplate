#nullable enable
using System;
using Newtonsoft.Json.Linq;

namespace FlappyTemplate
{
    [Serializable]
    public class PayloadDto
    {
        public string EventName = null!;
        public JToken Data = null!;
    }

    [Serializable]
    public class PayloadGroupElementDto
    {
        public string E = null!;
        public JToken O = null!;
    }

    [Serializable]
    public class PayloadGroupDto
    {
        public PayloadGroupElementDto[] Values = null!;
    }
}
