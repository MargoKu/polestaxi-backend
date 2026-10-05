namespace PolesTaxi.Shared.Types;

public enum Role
{
    Unspecified = 0,
    User = 1,
    Driver = 2,
    Analyst = 3,
    Admin = 4
}

public enum TaxiType
{
    Unspecified = 0,
    Economy = 1,
    Comfort = 2,
    Business = 3
}

public enum DriverStatus
{
    Unspecified = 0,
    Offline = 1,
    Available = 2,
    OnTrip = 3
}

public enum OrderStatus
{
    Unspecified = 0,
    Created = 1,
    DriverAssigned = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5
}

public static class EnumMaps
{
    public static string ToWire(this Role value) => value switch
    {
        Role.User => Consts.Roles.User,
        Role.Driver => Consts.Roles.Driver,
        Role.Analyst => Consts.Roles.Analyst,
        Role.Admin => Consts.Roles.Admin,
        _ => "Unspecified"
    };

    public static Role RoleFromWire(string? value) => value switch
    {
        Consts.Roles.User => Role.User,
        Consts.Roles.Driver => Role.Driver,
        Consts.Roles.Analyst => Role.Analyst,
        Consts.Roles.Admin => Role.Admin,
        _ => Role.Unspecified
    };

    public static string ToWire(this TaxiType value) => value switch
    {
        TaxiType.Economy => Consts.TaxiTypes.Economy,
        TaxiType.Comfort => Consts.TaxiTypes.Comfort,
        TaxiType.Business => Consts.TaxiTypes.Business,
        _ => "Unspecified"
    };

    public static TaxiType TaxiTypeFromWire(string? value) => value switch
    {
        Consts.TaxiTypes.Economy => TaxiType.Economy,
        Consts.TaxiTypes.Comfort => TaxiType.Comfort,
        Consts.TaxiTypes.Business => TaxiType.Business,
        _ => TaxiType.Unspecified
    };

    public static bool CanTransition(DriverStatus from, DriverStatus to) => (from, to) switch
    {
        (DriverStatus.Offline, DriverStatus.Available) => true,
        (DriverStatus.Available, DriverStatus.Offline) => true,
        (DriverStatus.Available, DriverStatus.OnTrip) => true,
        (DriverStatus.OnTrip, DriverStatus.Available) => true,
        (DriverStatus.OnTrip, DriverStatus.Offline) => true,
        _ when from == to => true,
        _ => false
    };
}
