using System;
using System.Collections.Generic;
using ZLinq;

/// <summary>
/// ZLinqの拡張メソッドを定義するクラス
/// </summary>
public static class ZLinqExtensions
{
    /// <summary>
    /// ValueEnumerableをIEnumerableに変換する
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

    /// <summary>
    /// ValueEnumerableの各要素に対して指定されたActionを実行する
    /// </summary>
    public static void ForEach<TEnumerator, TSource>(this ValueEnumerable<TEnumerator, TSource> source, Action<TSource> action)
        where TEnumerator : struct, IValueEnumerator<TSource>
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        using var e = source.Enumerator;

        if (e.TryGetSpan(out var span))
        {
            foreach (var item in span)
            {
                action(item);
            }
        }
        else
        {
            while (e.TryGetNext(out var item))
            {
                action(item);
            }
        }
    }
}