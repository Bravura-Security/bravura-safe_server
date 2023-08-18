using Bit.Core.Enums;
using Bit.Core.Models.StaticStore;

namespace Bit.Core.Utilities;

public static class PasswordManagerPlanStore
{
    public static IEnumerable<Plan> CreatePlan()
    {
        return new List<Plan>
        {
            new Plan
            {
                Type = PlanType.Free,
                Product = ProductType.Free,
                BitwardenProduct = BitwardenProductType.PasswordManager,
                Name = "Free",
                NameLocalizationKey = "planNameFree",
                DescriptionLocalizationKey = "planDescFree",
                BaseSeats = 2,
                MaxCollections = 2,
                MaxUsers = 2,

                UpgradeSortOrder = -1, // Always the lowest plan, cannot be upgraded to
                DisplaySortOrder = -1,

                AllowSeatAutoscale = false,
            },
            new Plan
            {
                Type = PlanType.TeamsAnnually2019,
                Product = ProductType.Teams,
                BitwardenProduct = BitwardenProductType.PasswordManager,
                Name = "Teams (Annually) 2019",
                IsAnnual = true,
                NameLocalizationKey = "planNameTeams",
                DescriptionLocalizationKey = "planDescTeams",
                CanBeUsedByBusiness = true,
                BaseSeats = 5,
                BaseStorageGb = 1,

                HasAdditionalSeatsOption = true,
                HasAdditionalStorageOption = true,
                TrialPeriodDays = 7,

                HasTotp = true,

                UpgradeSortOrder = 2,
                DisplaySortOrder = 2,
                LegacyYear = 2020,

                StripePlanId = "teams-org-annually",
                StripeSeatPlanId = "teams-org-seat-annually",
                StripeStoragePlanId = "storage-gb-annually",
                BasePrice = 60,
                SeatPrice = 24,
                AdditionalStoragePricePerGb = 4,

                AllowSeatAutoscale = true,
            },
            new Plan
            {
                Type = PlanType.EnterpriseAnnually2019,
                Name = "Enterprise (Annually) 2019",
                IsAnnual = true,
                Product = ProductType.Enterprise,
                BitwardenProduct = BitwardenProductType.PasswordManager,
                NameLocalizationKey = "planNameEnterprise",
                DescriptionLocalizationKey = "planDescEnterprise",
                CanBeUsedByBusiness = true,
                BaseSeats = 0,
                BaseStorageGb = 1,

                HasAdditionalSeatsOption = true,
                HasAdditionalStorageOption = true,
                TrialPeriodDays = 7,

                HasPolicies = true,
                HasSelfHost = true,
                HasGroups = true,
                HasDirectory = true,
                HasEvents = true,
                HasTotp = true,
                Has2fa = true,
                HasApi = true,
                UsersGetPremium = true,
                HasCustomPermissions = true,

                UpgradeSortOrder = 3,
                DisplaySortOrder = 3,
                LegacyYear = 2020,

                StripePlanId = null,
                StripeSeatPlanId = "enterprise-org-seat-annually",
                StripeStoragePlanId = "storage-gb-annually",
                BasePrice = 0,
                SeatPrice = 36,
                AdditionalStoragePricePerGb = 4,

                AllowSeatAutoscale = true,
            },
            new Plan
            {
                Type = PlanType.EnterpriseMonthly2019,
                Product = ProductType.Enterprise,
                BitwardenProduct = BitwardenProductType.PasswordManager,
                Name = "Enterprise (Monthly) 2019",
                NameLocalizationKey = "planNameEnterprise",
                DescriptionLocalizationKey = "planDescEnterprise",
                CanBeUsedByBusiness = true,
                BaseSeats = 0,
                BaseStorageGb = 1,

                HasAdditionalSeatsOption = true,
                HasAdditionalStorageOption = true,
                TrialPeriodDays = 7,

                HasPolicies = true,
                HasGroups = true,
                HasDirectory = true,
                HasEvents = true,
                HasTotp = true,
                Has2fa = true,
                HasApi = true,
                HasSelfHost = true,
                UsersGetPremium = true,
                HasCustomPermissions = true,

                UpgradeSortOrder = 3,
                DisplaySortOrder = 3,
                LegacyYear = 2020,

                StripePlanId = null,
                StripeSeatPlanId = "enterprise-org-seat-monthly",
                StripeStoragePlanId = "storage-gb-monthly",
                BasePrice = 0,
                SeatPrice = 4M,
                AdditionalStoragePricePerGb = 0.5M,

                AllowSeatAutoscale = true,
            },
            new Plan
            {
                Type = PlanType.BravuraTeams,
                Product = ProductType.Teams,
                BitwardenProduct = BitwardenProductType.PasswordManager,
                Name = "Teams",
                NameLocalizationKey = "bravuraPlanNameTeams",
                DescriptionLocalizationKey = "bravuraPlanDescTeams",
                BaseSeats = 32767,
                BaseStorageGb = 100,

                MaxCollections = 32767,

                HasAdditionalSeatsOption = false,
                HasAdditionalStorageOption = false,

                HasPolicies = true,
                HasGroups = true,
                HasDirectory = true,
                HasEvents = true,
                HasTotp = true,
                Has2fa = false,
                HasApi = true,
                HasSelfHost = true,
                HasSso = false,
                HasKeyConnector = false,
                HasScim = false,
                HasResetPassword = false,
                UsersGetPremium = true,
                HasCustomPermissions = true,

                UpgradeSortOrder = 7,
                DisplaySortOrder = 7,

                AllowSeatAutoscale = true,
            },
            new Plan
            {
                Type = PlanType.BravuraEnterprise,
                Product = ProductType.Enterprise,
                BitwardenProduct = BitwardenProductType.PasswordManager,
                Name = "Enterprise",
                NameLocalizationKey = "bravuraPlanNameEnterprise",
                DescriptionLocalizationKey = "bravuraPlanDescEnterprise",
                BaseSeats = 1000000,
                BaseStorageGb = 100,

                MaxCollections = 32767,

                HasPolicies = true,
                HasGroups = true,
                HasDirectory = true,
                HasEvents = true,
                HasTotp = true,
                Has2fa = true,
                HasApi = true,
                HasSelfHost = true,
                HasSso = true,
                HasKeyConnector = false,
                HasScim = false,
                HasResetPassword = true,
                UsersGetPremium = true,
                HasCustomPermissions = true,

                UpgradeSortOrder = 8,
                DisplaySortOrder = 8,

                AllowSeatAutoscale = true,
            },
            new Plan
            {
                Type = PlanType.Custom,

                AllowSeatAutoscale = true,
            },
        };
    }
}
