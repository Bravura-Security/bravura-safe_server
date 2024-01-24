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

        HasGroups = true;
        HasDirectory = true;
        HasEvents = true;
        HasTotp = true;
        Has2fa = true;
        HasApi = true;
        UsersGetPremium = true;

        UpgradeSortOrder = 2;
        DisplaySortOrder = 2;

        PasswordManager = new TeamsPasswordManagerFeatures();
        SecretsManager = new TeamsSecretsManagerFeatures();
    }

    private record TeamsSecretsManagerFeatures : SecretsManagerPlanFeatures
    {
        public TeamsSecretsManagerFeatures()
        {
            BaseSeats = 0;
            BasePrice = 0;
            BaseServiceAccount = 50;

            HasAdditionalSeatsOption = true;
            HasAdditionalServiceAccountOption = true;

            AllowSeatAutoscale = true;
            AllowServiceAccountsAutoscale = true;

            /*
            if (isAnnual)
            {
                StripeSeatPlanId = "secrets-manager-teams-seat-annually";
                StripeServiceAccountPlanId = "secrets-manager-service-account-annually";
                SeatPrice = 72;
                AdditionalPricePerServiceAccount = 6;
            }
            else
            {
                StripeSeatPlanId = "secrets-manager-teams-seat-monthly";
                StripeServiceAccountPlanId = "secrets-manager-service-account-monthly";
                SeatPrice = 7;
                AdditionalPricePerServiceAccount = 0.5M;
            }
            */
        }
    }

    private record TeamsPasswordManagerFeatures : PasswordManagerPlanFeatures
    {
        public TeamsPasswordManagerFeatures()
        {
            BaseSeats = 0;
            BaseStorageGb = 1;
            BasePrice = 0;

            HasAdditionalStorageOption = true;
            HasAdditionalSeatsOption = true;

            AllowSeatAutoscale = true;

            /*
            if (isAnnual)
            {
                StripeStoragePlanId = "storage-gb-annually";
                StripeSeatPlanId = "bravura-teams-org-seat-annually";
                SeatPrice = 48;
                AdditionalStoragePricePerGb = 4;
            }
            else
            {
                StripeSeatPlanId = "bravura-teams-org-seat-monthly";
                StripeStoragePlanId = "storage-gb-monthly";
                SeatPrice = 5;
                AdditionalStoragePricePerGb = 0.5M;
            }
            */
        }
    }
}
