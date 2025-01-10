using Bit.Core.Billing.Enums;
using Bit.Core.Models.StaticStore;

namespace Bit.Core.Billing.Models.StaticStore.Plans;

public record BravuraEnterprise : Plan
{
    public BravuraEnterprise()
    {
        Type = PlanType.BravuraEnterprise;
        ProductTier = ProductTierType.Enterprise;
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
        HasResetPassword = true;
        UsersGetPremium = true;
        HasCustomPermissions = true;
        HasSkip2faForSso = true;

        UpgradeSortOrder = 3;
        DisplaySortOrder = 3;

        PasswordManager = new EnterprisePasswordManagerFeatures();
    }

    private record EnterprisePasswordManagerFeatures : PasswordManagerPlanFeatures
    {
        public EnterprisePasswordManagerFeatures()
        {
            BaseSeats = 1000000;
            BaseStorageGb = 100;

            HasAdditionalStorageOption = true;
            HasAdditionalSeatsOption = true;

            AllowSeatAutoscale = true;
        }
    }
}
