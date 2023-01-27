using System;

namespace MultiFactor.SelfService.Windows.Portal.Core.Exceptions
{
    [Serializable]
    internal class FeatureNotEnabledException : Exception
    {
        public string FeatureDescription { get; }

        public FeatureNotEnabledException(string featureDescription)
        {
            FeatureDescription = featureDescription;
        }
        public FeatureNotEnabledException(string featureDescription, string message) : base(message)
        {
            FeatureDescription = featureDescription;
        }
        public FeatureNotEnabledException(string featureDescription, string message, Exception inner) : base(message, inner)
        {
            FeatureDescription = featureDescription;
        }
        protected FeatureNotEnabledException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }

    }
}