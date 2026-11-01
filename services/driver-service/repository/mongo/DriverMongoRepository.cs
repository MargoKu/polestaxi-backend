using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using PolesTaxi.DriverService.Entity.Repository;
using PolesTaxi.DriverService.Entity.Service;
using PolesTaxi.Shared.Errors;
using PolesTaxi.Shared.Types;

namespace PolesTaxi.DriverService.Repository.Mongo;

public sealed class DriverMongoRepository : IDriverRepository
{
    private readonly IMongoCollection<DriverDocument> _drivers;

    public DriverMongoRepository(IMongoCollection<DriverDocument> drivers)
    {
        _drivers = drivers;
    }

    public async Task<Driver> Insert(Driver driver, CancellationToken cancellationToken)
    {
        var doc = ToDocument(driver);
        try
        {
            await _drivers.InsertOneAsync(doc, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Code == 11000)
        {
            throw AppException.Conflict("driver already exists for user");
        }

        return ToDomain(doc);
    }

    public async Task<Driver> GetByUserId(string userId, CancellationToken cancellationToken)
    {
        var doc = await _drivers.Find(x => x.UserId == userId).FirstOrDefaultAsync(cancellationToken);
        if (doc is null)
        {
            throw AppException.NotFound("driver not found");
        }

        return ToDomain(doc);
    }

    public async Task<Driver> Update(string userId, DriverPatch patch, CancellationToken cancellationToken)
    {
        var update = Builders<DriverDocument>.Update
            .Set(x => x.TaxiType, patch.TaxiType.ToWire())
            .Set(x => x.LicensePlate, patch.LicensePlate)
            .Set(x => x.Location, ToGeo(patch.Location))
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        var filter = Builders<DriverDocument>.Filter.Eq(x => x.UserId, userId);
        FindOneAndUpdateOptions<DriverDocument, DriverDocument> options = new()
        {
            ReturnDocument = ReturnDocument.After
        };
        var doc = await _drivers.FindOneAndUpdateAsync(filter, update, options, cancellationToken);

        if (doc is null)
        {
            throw AppException.NotFound("driver not found");
        }

        return ToDomain(doc);
    }

    public async Task<Driver> SetStatus(string userId, DriverStatus status, CancellationToken cancellationToken)
    {
        var filter = Builders<DriverDocument>.Filter.Eq(x => x.UserId, userId);
        FindOneAndUpdateOptions<DriverDocument, DriverDocument> options = new()
        {
            ReturnDocument = ReturnDocument.After
        };
        var doc = await _drivers.FindOneAndUpdateAsync(
            filter,
            Builders<DriverDocument>.Update
                .Set(x => x.Status, StatusToWire(status))
                .Set(x => x.UpdatedAt, DateTime.UtcNow),
            options,
            cancellationToken);

        if (doc is null)
        {
            throw AppException.NotFound("driver not found");
        }

        return ToDomain(doc);
    }

    public async Task<IReadOnlyList<Driver>> FindAvailable(TaxiType taxiType, GeoPoint? near, int limit, CancellationToken cancellationToken)
    {
        var take = limit <= 0 ? 20 : Math.Min(limit, 100);
        var builder = Builders<DriverDocument>.Filter;
        var filter = builder.Eq(x => x.Status, StatusToWire(DriverStatus.Available))
                     & builder.Eq(x => x.TaxiType, taxiType.ToWire());

        if (near is not null)
        {
            filter &= builder.Near(x => x.Location, ToGeo(near)!);
        }

        var docs = await _drivers.Find(filter).Limit(take).ToListAsync(cancellationToken);
        return docs.Select(ToDomain).ToList();
    }

    private static DriverDocument ToDocument(Driver driver) => new()
    {
        Id = string.IsNullOrEmpty(driver.Id) ? ObjectId.GenerateNewId().ToString() : driver.Id,
        UserId = driver.UserId,
        TaxiType = driver.TaxiType.ToWire(),
        LicensePlate = driver.LicensePlate,
        Status = StatusToWire(driver.Status),
        Location = ToGeo(driver.Location),
        CreatedAt = driver.CreatedAt,
        UpdatedAt = driver.UpdatedAt
    };

    private static Driver ToDomain(DriverDocument doc) => new()
    {
        Id = doc.Id,
        UserId = doc.UserId,
        TaxiType = EnumMaps.TaxiTypeFromWire(doc.TaxiType),
        LicensePlate = doc.LicensePlate,
        Status = StatusFromWire(doc.Status),
        Location = doc.Location is null
            ? null
            : new GeoPoint
            {
                Latitude = doc.Location.Coordinates.Latitude,
                Longitude = doc.Location.Coordinates.Longitude
            },
        CreatedAt = doc.CreatedAt,
        UpdatedAt = doc.UpdatedAt
    };

    private static GeoJsonPoint<GeoJson2DGeographicCoordinates>? ToGeo(GeoPoint? point)
    {
        if (point is null)
        {
            return null;
        }

        return new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
            new GeoJson2DGeographicCoordinates(point.Longitude, point.Latitude));
    }

    private static string StatusToWire(DriverStatus status) => status switch
    {
        DriverStatus.Offline => "offline",
        DriverStatus.Available => "available",
        DriverStatus.OnTrip => "on-trip",
        _ => "unspecified"
    };

    private static DriverStatus StatusFromWire(string value) => value switch
    {
        "offline" => DriverStatus.Offline,
        "available" => DriverStatus.Available,
        "on-trip" => DriverStatus.OnTrip,
        _ => DriverStatus.Unspecified
    };
}
