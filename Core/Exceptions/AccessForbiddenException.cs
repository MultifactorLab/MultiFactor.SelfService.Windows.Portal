using System;

namespace MultiFactor.SelfService.Windows.Portal.Core.Exceptions
{ public class AccessForbiddenException : Exception
    {
        public AccessForbiddenException() { }
        public AccessForbiddenException(string message) : base(message) { }
        public AccessForbiddenException(string message, Exception inner) : base(message, inner) { }
    }
    
    
}

