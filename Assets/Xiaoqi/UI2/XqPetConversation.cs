using System.Collections.Generic;

namespace Xiaoqi.UI2
{
    // Display history only. Expiry never mutates the underlying chat session.
    public sealed class XqPetConversation
    {
        public sealed class Message { public long id; public bool assistant; public string text; }
        public readonly List<Message> Messages = new List<Message>();
        public float LastActivity { get; private set; } = -999;
        public void Put(long id, bool assistant, string text, float now)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var message = Messages.Find(m => m.id == id);
            if (message == null)
            {
                message = new Message { id=id, assistant=assistant };
                Messages.Add(message);
                while (Messages.Count > 3) Messages.RemoveAt(0);
            }
            message.text=text; LastActivity=now;
        }
        public void Tick(float now, bool busy)
        {
            if (busy) LastActivity=now;
            else if (now-LastActivity >= 8) Messages.Clear();
        }
        public void Clear() { Messages.Clear(); LastActivity=-999; }
    }
    // Feed can arrive before the HTTP reply carrying the very same sentence.
    // Resolve that race once the direct reply is known, without a duplicate bubble.
    public sealed class XqPetFeedBuffer
    {
        string _text; float _at;
        public void Wait(string text,float now){_text=text;_at=now;}
        public string Take(float now,bool waitingForReply,string reply)
        {
            if(waitingForReply)return null;
            var text=_text;_text=null;
            return now-_at>30||text==reply?null:text;
        }
        public void Clear(){_text=null;}
    }
}
