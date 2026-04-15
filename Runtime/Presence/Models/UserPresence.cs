using System;

namespace SocialManager.Presence.Models
{
    [Serializable]
    public class UserPresence
    {
        public string state; // "online" | "offline"
        public long lastChanged; // ServerValue.TIMESTAMP

        public bool IsOnline => state == "online";
    }
}
