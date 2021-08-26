using System;
using System.Linq;

namespace MultiFactor.SelfService.Windows.Portal.Models
{
    public class ExchangeActiveSyncDevice
    {
        public string MsExchDeviceId { get; set; }
        public string FriendlyName { get; set; }
        public string Model { get; set; }
        public string Type { get; set; }
        public DateTime WhenCreated { get; set; }
        public ExchangeActiveSyncDeviceAccessState AccessState { get; set; }
        public string AccessStateName
        {
            get
            {
                switch (AccessState)
                {
                    case ExchangeActiveSyncDeviceAccessState.Allowed:
                        return Resources.ExchangeActiveSyncDevices.AllowedState;
                    case ExchangeActiveSyncDeviceAccessState.Blocked:
                        return Resources.ExchangeActiveSyncDevices.BlockedState;
                    case ExchangeActiveSyncDeviceAccessState.Quarantined:
                        return Resources.ExchangeActiveSyncDevices.QuarantinedState;
                    default:
                        throw new NotImplementedException(AccessState.ToString());
                }
            }
        }
        public string AccessStateReason { get; set; }

        /// <summary>
        /// FriendlyName / Model / Type
        /// </summary>
        public string DisplayName
        {
            get
            {
                return new[]
                {
                    FriendlyName, Model, Type
                }
                .Where(el => !string.IsNullOrEmpty(el))
                .Distinct()
                .Aggregate((a, b) => a + " / " + b);
            }
        }
    }

    //https://social.technet.microsoft.com/Forums/en-US/016ed5b5-d26d-48e7-8c1f-613de81e6d91/how-can-i-use-the-shell-to-get-a-list-of-all-quarantined-devices-in-2010-sp1?forum=exchangesvrmobilitylegacy
    public enum ExchangeActiveSyncDeviceAccessState
    {
        Allowed = 1,
        Blocked = 2,
        Quarantined = 3
    }
}