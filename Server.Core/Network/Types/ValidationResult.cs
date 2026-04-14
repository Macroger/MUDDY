using Shared.Protocol.System;

namespace Server.Core.Network.Model
{
    public class ValidationResult
    {
        public bool IsValid { get; init; }
        public SystemResponse? RejectionResponse { get; init; }

        private ValidationResult(bool isValid, SystemResponse? rejectionResponse)
        {
            IsValid = isValid;
            RejectionResponse = rejectionResponse;
        }

        #region Factory methods to create specific types of ValidationResult
        public static ValidationResult Valid()
        {
            return new ValidationResult(true, null);
        }
        public static ValidationResult Invalid(SystemResponse rejectionResponse)
        {
            return new ValidationResult(false, rejectionResponse);
        }
        #endregion
    }
}
