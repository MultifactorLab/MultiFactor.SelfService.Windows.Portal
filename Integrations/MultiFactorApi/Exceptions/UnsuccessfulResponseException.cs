using System;

namespace MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorApi.Exceptions
{
    /// <summary>
    /// Indicates that the response payload property "Success" is "false";
    /// </summary>
    internal class UnsuccessfulResponseException : Exception
    {
        public UnsuccessfulResponseException() { }
        public UnsuccessfulResponseException(string message) : base(message) { }
        public UnsuccessfulResponseException(string message, Exception inner) : base(message, inner) { }
    }
}
