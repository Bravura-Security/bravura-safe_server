using Bit.Core.Billing.Enums;
using Bit.Core.Models.StaticStore;

namespace Bit.Core.Billing.Models.StaticStore.Plans;

public record BravuraTeams : Plan
{
    public BravuraTeams()
    {
        Type = PlanType.BravuraTeams;
        ProductTier = ProductTierType.Teams;
        Name = "Bravura Teams";
        NameLocalizationKey = "planNameTeams";
        DescriptionLocalizationKey = "planDescTeams";
        CanBeUsedByBusiness = true;

        TrialPeriodDays = 7;

        HasPolicies = true;
        HasSelfHost = true;
        HasGroups = true;
        HasDirectory = true;
        HasEvents = true;
        HasTotp = true;
        Has2fa = false;
        HasApi = true;
        UsersGetPremium = true;
        HasCustomPermissions = true;
        HasSkip2faForSso = false;

        UpgradeSortOrder = 2;
        DisplaySortOrder = 2;

        PasswordManager = new TeamsPasswordManagerFeatures();
    }

    private record TeamsPasswordManagerFeatures : PasswordManagerPlanFeatures
    {
        public TeamsPasswordManagerFeatures()
        {
            BaseSeats = 32767;
            BaseStorageGb = 100;

            HasAdditionalStorageOption = true;
            HasAdditionalSeatsOption = true;

            AllowSeatAutoscale = true;
        }
    }
}
