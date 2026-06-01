using System.Collections.Generic;

namespace MultiFactor.SelfService.Windows.Portal.Core.Authentication.AdditionalClaims
{
    public interface IApplicationValuesContext
    {
        /// <summary>
        /// Returns all available values for the specified key.
        /// </summary>
        /// <param name="key">Key identifier.</param>
        /// <returns>List of values.</returns>
        IReadOnlyList<string> this[string key] { get; }
    }
}
