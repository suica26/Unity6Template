namespace Test;

/// <summary>
/// C#11 チェック用のテストクラス
/// このクラスは C# 11 の新機能を使用していることを確認するためのもの
/// file修飾子、required修飾子などはnetstandard2.1では使用不可
/// </summary>
public class CSharp11Test
{
    // record struct
    private record struct Point(int X, int Y);

    private Point _point;

    public void Test()
    {
        // 拡張プロパティパターンマッチング
        if (_point is Point { X: 1, Y: 2 })
        {
            // _point が Point であり、X が 1、Y が 2 の場合の処理
        }

        // 生文字列リテラル
        var rawString = """
            This is a raw string literal.
            It can span multiple lines.
            """;

        // リストパターン
        var numbers = new[] { 1, 2, 3 };
        if (numbers is [1, 2, _])
        {
            // 1, 2, _ のパターンにマッチした場合の処理
        }

        // スパンパターン
        if (numbers is [.. var middle, 3])
        {
            // middle は [1, 2] になる
        }
    }
}