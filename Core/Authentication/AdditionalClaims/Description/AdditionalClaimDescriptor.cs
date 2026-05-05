using System;
using System.Diagnostics;
using MultiFactor.SelfService.Windows.Portal.Core.Authentication.AdditionalClaims.Description.Conditions;

namespace MultiFactor.SelfService.Windows.Portal.Core.Authentication.AdditionalClaims.Description
{
    /// <summary>
    /// Object that describes additional claim properties.
    /// </summary>
    [DebuggerDisplay("Name = {Name}")]
    public class AdditionalClaimDescriptor
    {
        /// <summary>
        /// Claim name (type).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// From where claim value will be consumed.
        /// </summary>
        public IClaimValueSource Source { get; }

        /// <summary>
        /// Object described condition that can be evaluated.
        /// </summary>
        public ClaimCondition Condition { get; }

        public AdditionalClaimDescriptor(string name, IClaimValueSource source, ClaimCondition condition)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Condition = condition;
        }
    }
}
