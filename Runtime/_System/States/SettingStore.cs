using System;
using System.Globalization;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace FlappyTemplate
{
    // The player's own settings - `sound`, `music`, `keyboard`, the volumes - as the features that keep one
    // read and write them.
    //
    // The server holds them as a flat table of strings per session and sends the lot as ON_SETTING: on init,
    // when they are changed from here, and when they are changed from somewhere else the same player is
    // signed in - the web front in another tab, a second device. Incoming merges that into
    // `MainState.Settings`, so this class is only ever reading a dictionary that is already in memory.
    //
    // Writing is two things rather than one: the value goes into the local state *and* out over the socket.
    // The local write is what makes a switch answer the press rather than the round trip - the ON_SETTING that
    // comes back a moment later merges over it and says the same thing.
    //
    // Values arrive as whatever the sender wrote them as. The server's own defaults are strings - "1", "0" -
    // the web front writes numbers, and a hand-written one may well be a bool. All three mean the same thing
    // here, which is what Flag and Number are for: a setting that arrives in the shape nobody expected reads
    // as absent rather than throwing in the middle of a layout pass.
    internal static class SettingStore
    {
        /// <summary>Whether there is a game to read settings from at all. False in a scene being laid out
        /// rather than played in, where there is no socket to have sent any - which is a different thing from
        /// a setting the player has simply not changed, and the reason callers are given both.</summary>
        public static bool Available => Table != null;

        /// <summary>A setting read as a switch. False means it is not there, or is in a shape that means
        /// nothing - not that it is off.</summary>
        public static bool TryFlag(string key, out bool value)
        {
            value = false;

            var table = Table;
            if (table == null || string.IsNullOrEmpty(key) || !table.TryGetValue(key, out var token))
                return false;

            return Truthy(token, out value);
        }

        /// <summary>A setting read as a number, in whatever units the caller stores it in.</summary>
        public static bool TryNumber(string key, out float value)
        {
            value = 0f;

            var table = Table;
            if (table == null || string.IsNullOrEmpty(key) || !table.TryGetValue(key, out var token))
                return false;

            return Numeric(token, out value);
        }

        /// <summary>Writes a setting locally and sends it. What a switch being flipped does.</summary>
        public static void Set(string key, object value)
        {
            Write(key, value);
            Send(key, value);
        }

        /// <summary>Writes a setting into the local state and nowhere else. For a control that is still being
        /// dragged - see <see cref="Send"/>.</summary>
        public static void Write(string key, object value)
        {
            var manager = StateManager.Inst;
            if (manager == null || manager.MainState == null || string.IsNullOrEmpty(key))
                return;

            manager.MainState.Settings[key] = Token(value);
            manager.MainState._Settings[key] = Text(value);
        }

        /// <summary>Sends a setting to the server without touching the local state. Split from
        /// <see cref="Write"/> because a slider writes on every frame it moves and should send once, when the
        /// finger comes off.</summary>
        public static void Send(string key, object value)
        {
            if (Emitter.Inst == null || string.IsNullOrEmpty(key))
                return;

            Emitter.Inst.OnSettingSet(new SettingDto { Name = key, Value = Wire(value) });
        }

        // A switch goes out as 1 or 0 rather than as true or false. The server keeps settings as strings and
        // the web front reads them back as numbers, so a Unity build that sent "true" would be writing a value
        // its own web front could not make sense of - and the two share one player's settings.
        private static object Wire(object value)
        {
            if (value is bool flag)
                return flag ? 1 : 0;

            return value;
        }

        private static Dictionary<string, JToken> Table
        {
            get
            {
                var manager = StateManager.Inst;
                return manager != null && manager.MainState != null ? manager.MainState.Settings : null;
            }
        }

        // The JSON shape the socket would have delivered, so a value written here reads back exactly as one
        // that came over the wire - anything reading the table cannot tell the two apart, and should not.
        private static JToken Token(object value)
        {
            switch (value)
            {
                case bool flag:
                    return new JValue(flag ? 1 : 0);
                case null:
                    return JValue.CreateNull();
                default:
                    return JToken.FromObject(value);
            }
        }

        // The mirror of that for `_Settings`, which is the inspector-readable copy: strings, so a serialized
        // dictionary can be looked at while the game runs.
        private static string Text(object value)
        {
            switch (value)
            {
                case null:
                    return string.Empty;
                case bool flag:
                    return flag ? "1" : "0";
                case float number:
                    return number.ToString(CultureInfo.InvariantCulture);
                case double number:
                    return number.ToString(CultureInfo.InvariantCulture);
                case IFormattable formattable:
                    return formattable.ToString(null, CultureInfo.InvariantCulture);
                default:
                    return value.ToString();
            }
        }

        private static bool Truthy(JToken token, out bool value)
        {
            value = false;

            if (token == null || token.Type == JTokenType.Null)
                return false;

            switch (token.Type)
            {
                case JTokenType.Boolean:
                    value = token.Value<bool>();
                    return true;
                case JTokenType.Integer:
                case JTokenType.Float:
                    value = token.Value<double>() != 0d;
                    return true;
                case JTokenType.String:
                    var text = token.Value<string>();
                    if (string.IsNullOrEmpty(text))
                        return false;

                    value = text != "0" && !text.Equals("false", StringComparison.OrdinalIgnoreCase);
                    return true;
                default:
                    return false;
            }
        }

        // Invariant culture on the string case, and that is not a detail: a build running on a Russian or
        // German locale parses "0.5" as five with a comma-decimal culture, and a volume of five is a volume
        // of one after it has been clamped. Which reads, from the outside, like the slider doing nothing.
        private static bool Numeric(JToken token, out float value)
        {
            value = 0f;

            if (token == null || token.Type == JTokenType.Null)
                return false;

            switch (token.Type)
            {
                case JTokenType.Integer:
                case JTokenType.Float:
                    value = token.Value<float>();
                    return true;
                case JTokenType.Boolean:
                    value = token.Value<bool>() ? 1f : 0f;
                    return true;
                case JTokenType.String:
                    return float.TryParse(
                        token.Value<string>(),
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out value);
                default:
                    return false;
            }
        }
    }
}
