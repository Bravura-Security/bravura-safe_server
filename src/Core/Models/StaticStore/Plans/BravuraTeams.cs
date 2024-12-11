using Bit.Core.Enums;

namespace Bit.Core.Models.StaticStore.Plans;

public record BravuraTeams : Models.StaticStore.Plan
{
    public BravuraTeams()
    {
        Type = PlanType.BravuraTeams;
        Product = ProductType.Teams;
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
