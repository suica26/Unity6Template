using System.Collections.Generic;
using ZLinq;

/// <summary>
/// ZLinqの拡張メソッドを定義するクラス
/// </summary>
public static class ZLinqExtensions
{
    /// <summary>
    /// ValueEnumerableをIEnumerableに変換する拡張メソッド
    /// </summary>
    public static IEnumerable<T> AsEnumerable<TEnumerator, T>(this ValueEnumerable<TEnumerator, T> valueEnumerable)
        where TEnumerator : struct, IValueEnumerator<T>
    {
        using var e = valueEnumerable.Enumerator;

        while (e.TryGetNext(out var current))
        {
            yield return current;
        }
    }
}