#nullable enable
using System;
using Newtonsoft.Json;

namespace FlappyTemplate
{
    // ON_BET_ROLLBACK: a bet the player took back with Emitter.OnBetRollback. It is out of the round either way;
    // Refunded says whether the stake is already back on the balance - the ON_BALANCE that carries it arrives just
    // before this - or still on its way, the integrator having failed to answer and the refund being retried.
    [Serializable]
    public class BetRollbackDto
    {
        public string TransactionId = string.Empty;

        // The stake given back, in the bet's own currency.
        public string Amount = "0";

        public bool Refunded;

        // The bet as MainState.BetInfos held it until now, for a game that wants to take back what it drew for it.
        // Null when this client never had it - placed from another tab, or before a reconnect that did not resend it.
        [JsonIgnore]
        public BetInfoDto? BetInfo;
    }
}
