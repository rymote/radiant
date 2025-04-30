using MongoDB.Bson;
using MongoDB.Driver;
using Rymote.Radiant.Core;

namespace Rymote.Radiant.Mongo;

public class RadiantMongoQueryExecutor(IMongoClient client, string databaseName) : RadiantQueryExecutorBase
{
    private readonly IMongoClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly string _databaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
    private readonly Dictionary<Type, Dictionary<string, string>> _mappings = new();

    public override IRadiantQueryExecutor MapProperty<TPoco>(string propertyName, string documentField)
    {
        Type pocoType = typeof(TPoco);
        if (!_mappings.ContainsKey(pocoType))
            _mappings[pocoType] = new Dictionary<string, string>();

        _mappings[pocoType][propertyName] = documentField;
        return this;
    }

    private string GetMappedFieldFor<T>(string propertyName)
    {
        Type pocoType = typeof(T);
        return _mappings.TryGetValue(pocoType, out Dictionary<string, string>? map) &&
               map.TryGetValue(propertyName, out string? mapped)
            ? mapped
            : propertyName;
    }

    protected override IEnumerable<T> ExecuteCore<T>(RadiantQueryData data)
    {
        if (string.IsNullOrWhiteSpace(data.TargetName))
            throw new InvalidOperationException("No target (collection) specified for Mongo.");

        IMongoDatabase? database = _client.GetDatabase(_databaseName);
        IMongoCollection<BsonDocument>? collection = database.GetCollection<BsonDocument>(data.TargetName);

        return data.OperationType switch
        {
            RadiantOperationType.Create => ExecuteInsert<T>(collection, data),
            RadiantOperationType.Get => ExecuteSelect<T>(collection, data),
            RadiantOperationType.Update => ExecuteUpdate<T>(collection, data),
            RadiantOperationType.Remove => ExecuteDelete<T>(collection, data),
            _ => throw new InvalidOperationException($"Unsupported operation: {data.OperationType}")
        };
    }

    private IEnumerable<T> ExecuteInsert<T>(IMongoCollection<BsonDocument> collection, RadiantQueryData data)
    {
        BsonDocument document = new BsonDocument();
        foreach ((string key, object? value) in data.CreateValues)
        {
            string fieldName = GetMappedFieldFor<T>(key);
            document[fieldName] = BsonValue.Create(value);
        }

        collection.InsertOne(document);
        return ConvertDocumentToEnumerable<T>(document);
    }

    private IEnumerable<T> ExecuteSelect<T>(IMongoCollection<BsonDocument> collection, RadiantQueryData data)
    {
        FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Empty;

        if (data.Columns.Count != 0)
        {
            BsonDocument projectionDocument = new BsonDocument();
            
            foreach (string property in data.Columns)
            {
                string mappedField = GetMappedFieldFor<T>(property);
                
                if (mappedField != property)
                    projectionDocument[property] = "$" + mappedField;
                else
                    projectionDocument[property] = 1;
            }

            // Build the aggregation pipeline as a list of BsonDocument stages.
            List<BsonDocument> pipelineStages = new List<BsonDocument>();
            
            if (filter != FilterDefinition<BsonDocument>.Empty) 
                pipelineStages.Add(filter.ToBsonDocument());

            pipelineStages.Add(new BsonDocument("$project", projectionDocument));
            
            if (data.Offset.HasValue)
                pipelineStages.Add(new BsonDocument("$skip", data.Offset.Value));
            if (data.Limit.HasValue)
                pipelineStages.Add(new BsonDocument("$limit", data.Limit.Value));

            List<BsonDocument> aggregatedDocuments = collection.Aggregate<BsonDocument>(pipelineStages).ToList();
            return ConvertDocumentsToEnumerable<T>(aggregatedDocuments);
        }

        List<BsonDocument> documents = collection.Find(filter)
            .Skip(data.Offset)
            .Limit(data.Limit)
            .ToList();
        
        return ConvertDocumentsToEnumerable<T>(documents);
    }

    private IEnumerable<T> ExecuteUpdate<T>(IMongoCollection<BsonDocument> collection, RadiantQueryData data)
    {
        if (data.UpdateValues.Count == 0)
            throw new InvalidOperationException("No UpdateValues provided for update.");

        IEnumerable<UpdateDefinition<BsonDocument>> updates = data.UpdateValues
            .Select(kvp =>
            {
                string fieldName = GetMappedFieldFor<T>(kvp.Key);
                return Builders<BsonDocument>.Update.Set(fieldName, BsonValue.Create(kvp.Value));
            });

        UpdateDefinition<BsonDocument>? updateDefinition = Builders<BsonDocument>.Update.Combine(updates);
        FilterDefinition<BsonDocument>? filter = Builders<BsonDocument>.Filter.Empty;

        FindOneAndUpdateOptions<BsonDocument> options = new FindOneAndUpdateOptions<BsonDocument>
        {
            ReturnDocument = ReturnDocument.After
        };

        BsonDocument? updatedDocument = collection.FindOneAndUpdate(filter, updateDefinition, options);
        return updatedDocument == null ? [] : ConvertDocumentToEnumerable<T>(updatedDocument);
    }

    private IEnumerable<T> ExecuteDelete<T>(IMongoCollection<BsonDocument> collection, RadiantQueryData data)
    {
        FilterDefinition<BsonDocument>? filter = Builders<BsonDocument>.Filter.Empty;
        DeleteResult? result = collection.DeleteMany(filter);
        int deletedCount = (int)result.DeletedCount;

        if (typeof(T) == typeof(int))
            return (IEnumerable<T>)(object)new List<int> { deletedCount };

        return [];
    }

    private static IEnumerable<T> ConvertDocumentToEnumerable<T>(BsonDocument? document)
    {
        if (document == null)
            return [];

        if (typeof(T) == typeof(BsonDocument))
            return (IEnumerable<T>)(object)new List<BsonDocument> { document };

        T obj = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<T>(document);
        return new List<T> { obj };
    }

    private static IEnumerable<T> ConvertDocumentsToEnumerable<T>(List<BsonDocument>? documents)
    {
        if (documents == null || documents.Count == 0)
            return [];

        if (typeof(T) == typeof(BsonDocument))
            return (IEnumerable<T>)(object)documents;

        List<T> list = new List<T>(documents.Count);
        list.AddRange(documents.Select(document => MongoDB.Bson.Serialization.BsonSerializer.Deserialize<T>(document)));

        return list;
    }
}