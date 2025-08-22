using System;

namespace MultiFactor.SelfService.Windows.Portal.Core.Exceptions
{
    [Serializable]
    public class AccessForbiddenException : Exception
    {
        public AccessForbiddenException() { }
        public AccessForbiddenException(string message) : base(message) { }
        public AccessForbiddenException(string message, Exception inner) : base(message, inner) { }
        protected AccessForbiddenException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    
    
}

