using System;

namespace MultiFactor.SelfService.Windows.Portal.Core.Exceptions
{
    [Serializable]
    internal class PasswordChangingSessionExpired : Exception
    {
        public string Identity { get; }
        public PasswordChangingSessionExpired(string identity) 
        {
            Identity = identity;
        }
        public PasswordChangingSessionExpired(string identity, string message) : base(message) 
        { 
            Identity = identity;
        }
        public PasswordChangingSessionExpired(string identity, string message, Exception inner) : base(message, inner) 
        {
            Identity = identity;
        }
        protected PasswordChangingSessionExpired(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}