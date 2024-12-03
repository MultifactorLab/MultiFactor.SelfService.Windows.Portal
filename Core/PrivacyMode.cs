namespace MultiFactor.SelfService.Windows.Portal.Core
{
    /// <summary>
    /// User information disclosure mode
    /// </summary>
    public enum PrivacyMode
    {
        /// <summary>
        /// Include all
        /// </summary>
        None,
    
        /// <summary>
        /// Disable all but identity
        /// </summary>
        Full,

        /// <summary>
        /// Disable all but identity and specified fields.
        /// </summary>
        Partial
    }
}