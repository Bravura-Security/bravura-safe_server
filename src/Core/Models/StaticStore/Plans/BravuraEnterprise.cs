using Bit.Core.Enums;

namespace Bit.Core.Models.StaticStore.Plans;

public record BravuraEnterprise : Models.StaticStore.Plan
{
    public BravuraEnterprise()
    {
        Type = PlanType.BravuraEnterprise;
        Product = ProductType.Enterprise;
        Name = "Bravura Enterprise";
        NameLocalizationKey = "planNameEnterprise";
        DescriptionLocalizationKey = "planDescEnterprise";
        CanBeUsedByBusiness = true;

        TrialPeriodDays = 7;

        HasPolicies = true;
        HasSelfHost = true;
        HasGroups = true;
        HasDirectory = true;
        HasEvents = true;
        HasTotp = true;
        Has2fa = true;
        HasApi = true;
        HasSso = true;
        HasKeyConnector = true;
        HasScim = true;
        HasResetPassword = true;
        UsersGetPremium = true;
        HasCustomPermissions = true;

        UpgradeSortOrder = 3;
        DisplaySortOrder = 3;

        PasswordManager = new EnterprisePasswordManagerFeatures();
        SecretsManager = new EnterpriseSecretsManagerFeatures();
    }

    private record EnterpriseSecretsManagerFeatures : SecretsManagerPlanFeatures
    {
        public EnterpriseSecretsManagerFeatures()
        {
            BaseSeats = 0;
            BasePrice = 0;
            BaseServiceAccount = 200;

            HasAdditionalSeatsOption = true;
            HasAdditionalServiceAccountOption = true;

            AllowSeatAutoscale = true;
            AllowServiceAccountsAutoscale = true;

            /*
            if (isAnnual)
            {
                StripeSeatPlanId = "secrets-manager-enterprise-seat-annually";
                StripeServiceAccountPlanId = "secrets-manager-service-account-annually";
                SeatPrice = 144;
                AdditionalPricePerServiceAccount = 6;
            }
            else
            {
                StripeSeatPlanId = "secrets-manager-enterprise-seat-monthly";
                StripeServiceAccountPlanId = "secrets-manager-service-account-monthly";
                SeatPrice = 13;
                AdditionalPricePerServiceAccount = 0.5M;
            }
            */
        }
    }

    private record EnterprisePasswordManagerFeatures : PasswordManagerPlanFeatures
    {
        public EnterprisePasswordManagerFeatures()
        {
            BaseSeats = 0;
            BaseStorageGb = 1;

            HasAdditionalStorageOption = true;
            HasAdditionalSeatsOption = true;

            AllowSeatAutoscale = true;

            /*
            if (isAnnual)
            {
                AdditionalStoragePricePerGb = 4;
                StripeStoragePlanId = "storage-gb-annually";
                StripeSeatPlanId = "bravura-enterprise-org-seat-annually";
                SeatPrice = 72;
            }
            else
            {
                StripeSeatPlanId = "bravura-enterprise-seat-monthly";
                StripeStoragePlanId = "storage-gb-monthly";
                SeatPrice = 7;
                AdditionalStoragePricePerGb = 0.5M;
            }
            */
        }
    }
}
