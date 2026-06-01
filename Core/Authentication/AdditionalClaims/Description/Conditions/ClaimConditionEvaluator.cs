using System;
using Serilog;
using System.Linq;

namespace MultiFactor.SelfService.Windows.Portal.Core.Authentication.AdditionalClaims.Description.Conditions
{
    public class ClaimConditionEvaluator
    {
        private readonly IApplicationValuesContext _claimValuesContext;
        private readonly ILogger _logger;

        public ClaimConditionEvaluator(IApplicationValuesContext claimValuesContext, ILogger logger)
        {
            _claimValuesContext = claimValuesContext ?? throw new ArgumentNullException(nameof(claimValuesContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Evaluates condition and returns TRUE or FALSE.
        /// </summary>
        /// <param name="condition">Claim condition</param>
        /// <returns>TRUE / FALSE</returns>
        public bool Evaluate(ClaimCondition condition)
        {
            if (condition.Operation != ClaimsConditionOperation.Eq)
            {
                _logger.Debug("Not supported claim condition operation: {op}", condition.Operation);
                return false;
            }
            var leftValue = condition.LeftOperand.GetValues(_claimValuesContext);
            var rightValue = condition.RightOperand.GetValues(_claimValuesContext);

            if (!leftValue.Any() || !rightValue.Any())
                return false;

            if (leftValue.Count == 1 && rightValue.Count == 1)
            {
                return leftValue[0] == rightValue[0];
            }

            if (leftValue.Count > 1 && rightValue.Count > 1)
            {
                return leftValue.OrderBy(x => x).SequenceEqual(
                    rightValue.OrderBy(x => x));
            }

            return rightValue.Count > 1 && leftValue.Count == 1 ?
                rightValue.Any(x => leftValue.Contains(x)) :
                leftValue.Any(x => rightValue.Contains(x));
        }
    }
}
