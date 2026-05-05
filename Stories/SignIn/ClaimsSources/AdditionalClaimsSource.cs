using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using MultiFactor.SelfService.Windows.Portal.Core.Authentication.AdditionalClaims;
using MultiFactor.SelfService.Windows.Portal.Core.Authentication.AdditionalClaims.Description.Conditions;
using MultiFactor.SelfService.Windows.Portal.Core.Authentication.AuthenticationClaims;
using MultiFactor.SelfService.Windows.Portal.Core.Metadata;

namespace MultiFactor.SelfService.Windows.Portal.Stories.SignIn.ClaimsSources
{
    public class AdditionalClaimsSource : IClaimsSource
    {
        private readonly AdditionalClaimsMetadata _additionalClaimsMetadata;
        private readonly IApplicationValuesContext _claimValuesContext;
        private readonly ClaimConditionEvaluator _conditionEvaluator;
        private readonly ILogger _logger;
        private readonly Configuration _settings;

        public AdditionalClaimsSource(AdditionalClaimsMetadata additionalClaimsMetadata, IApplicationValuesContext claimValuesContext,
            ClaimConditionEvaluator conditionEvaluator, ILogger logger, Configuration settings)
        {
            _additionalClaimsMetadata = additionalClaimsMetadata ?? throw new ArgumentNullException(nameof(additionalClaimsMetadata));
            _claimValuesContext = claimValuesContext ?? throw new ArgumentNullException(nameof(claimValuesContext));
            _conditionEvaluator = conditionEvaluator ?? throw new ArgumentNullException(nameof(conditionEvaluator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public IReadOnlyDictionary<string, string> GetClaims()
        {
            var claims = new Dictionary<string, string>();
            Log($"Try to consume additional claims: {string.Join(", ", _additionalClaimsMetadata.Descriptors.Select(x => x.Name))}");

            foreach (var descriptor in _additionalClaimsMetadata.Descriptors)
            {
                Log($"Getting {(descriptor.Condition != null ? "conditional" : "non conditional")} claim '{descriptor.Name}'...");

                if (descriptor.Condition == null)
                {
                    var value = GetValue(descriptor.Source.GetValues(_claimValuesContext));
                    claims[descriptor.Name] = value;
                    Log($"Claim {{Type: '{descriptor.Name}', Value: '{value}'}} was added");
                    continue;
                }

                var result = _conditionEvaluator.Evaluate(descriptor.Condition);
                Log($"Claim '{descriptor.Name}' condition evaluating result: '{result}'");
                if (result)
                {
                    var value = GetValue(descriptor.Source.GetValues(_claimValuesContext));
                    claims[descriptor.Name] = value;
                    Log($"Claim {{Type: '{descriptor.Name}', Value: '{value}'}} was added");
                }
            }

            return claims;
        }

        private string GetValue(IReadOnlyList<string> values)
        {
            switch (values.Count)
            {
                case 0: return string.Empty;
                case 1: return values[0];
                default: return System.Text.Json.JsonSerializer.Serialize(values);
            }
        }

        private void Log(string message)
        {
            _logger.Debug(message);
        }
    }
}