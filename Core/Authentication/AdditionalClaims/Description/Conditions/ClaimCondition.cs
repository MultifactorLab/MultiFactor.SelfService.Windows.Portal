using System;

namespace MultiFactor.SelfService.Windows.Portal.Core.Authentication.AdditionalClaims.Description.Conditions
{
    public class ClaimCondition
    {
        public IClaimValueSource LeftOperand { get; }
        public IClaimValueSource RightOperand { get; }
        public ClaimsConditionOperation Operation { get; }

        public ClaimCondition(IClaimValueSource left, IClaimValueSource right, ClaimsConditionOperation op)
        {
            LeftOperand = left ?? throw new ArgumentNullException(nameof(left));
            RightOperand = right ?? throw new ArgumentNullException(nameof(right));
            Operation = op;
        }
    }
}
