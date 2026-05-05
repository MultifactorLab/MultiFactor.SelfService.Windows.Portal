using System.Collections.Generic;

namespace MultiFactor.SelfService.Windows.Portal.Core.Authentication.AdditionalClaims
{
    public interface IClaimValueSource
    {
        /// <summary>
        /// Returns all available values for the claim.
        /// </summary>
        /// <param name="context">Context that consumes all available values.</param>
        /// <returns>List of values.</returns>
        IReadOnlyList<string> GetValues(IApplicationValuesContext context);
    }
}
