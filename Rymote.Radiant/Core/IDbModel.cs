namespace Rymote.Radiant.Core;

public interface IDbModel<TKey>
{
    TKey Id { get; set; }
}