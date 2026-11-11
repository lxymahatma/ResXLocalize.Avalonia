using System.Collections;

namespace ResXLocalize.Avalonia.Collections;

internal readonly struct EquatableArray<T>(ImmutableArray<T> array) : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    private readonly ImmutableArray<T> _array = array;

    public bool Equals(EquatableArray<T> other) => _array.AsSpan().SequenceEqual(other._array.AsSpan());

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var item in _array)
        {
            hash.Add(item);
        }

        return hash.ToHashCode();
    }

    public ImmutableArray<T>.Enumerator GetEnumerator() => _array.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)_array).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_array).GetEnumerator();
}
