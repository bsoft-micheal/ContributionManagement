namespace TeamContributionManagementSystem.Domain.Entities;

/// <summary>
/// Represents the application navigation menu hierarchy.
/// Column alignment with role_rights:
///   module     = role_rights.module     (Dashboard, Members, Events, Finance, Support Ticket, Tools, Reports)
///   sub_module = role_rights.sub_module (Analytics, Directory, Registry, Ledger, Admin, etc.)
///   activity   = role_rights.page       (Dashboard, Members, Event, Calendar, Contribution, etc.)
/// </summary>
public class NavigationMenu
{
    public int FeatureID { get; set; }
    public int MainModuleID { get; set; }

    /// <summary>Top-level module name. NULL for child rows. Matches role_rights.Module on parent rows.</summary>
    public string? Module { get; set; }

    /// <summary>Parent FeatureID. 0 = top-level row.</summary>
    public int ParentID { get; set; }

    /// <summary>Logical group name. Matches role_rights.SubModule. NULL for parent header rows.</summary>
    public string? SubModule { get; set; }

    /// <summary>Display / page label shown in menu. Matches role_rights.Page. NULL for parent header rows.</summary>
    public string? Activity { get; set; }

    public string RoutingUrl { get; set; } = "#";
    public int ModuleNO { get; set; }
    public int DisplayOrder { get; set; }
    public bool HasSubModule { get; set; }
    public bool ShowingUserRight { get; set; }
    public string? ItemDescription { get; set; }
}
