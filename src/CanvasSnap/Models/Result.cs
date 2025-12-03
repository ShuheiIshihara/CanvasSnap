using System;

namespace CanvasSnap.Models;

/// <summary>
/// 成功値またはエラー値のいずれかを保持する結果型
/// Rustスタイルのエラーハンドリングを.NETで実現
/// 例外をスローする代わりに、エラーを値として扱う
/// Requirements: 2.7 (エラーハンドリング), 3.1 (Result型による型安全なエラー伝播)
/// </summary>
/// <typeparam name="T">成功時の値の型</typeparam>
/// <typeparam name="E">エラー時の値の型</typeparam>
public readonly struct Result<T, E>
{
    private readonly T? _value;
    private readonly E? _error;
    private readonly bool _isSuccess;

    /// <summary>
    /// 成功時のコンストラクタ
    /// </summary>
    private Result(T value)
    {
        _value = value;
        _error = default;
        _isSuccess = true;
    }

    /// <summary>
    /// エラー時のコンストラクタ
    /// </summary>
    private Result(E error)
    {
        _value = default;
        _error = error;
        _isSuccess = false;
    }

    /// <summary>
    /// 成功したかどうか
    /// </summary>
    public bool IsSuccess => _isSuccess;

    /// <summary>
    /// エラーかどうか
    /// </summary>
    public bool IsError => !_isSuccess;

    /// <summary>
    /// 成功値を取得（成功時のみ有効）
    /// エラー時はInvalidOperationExceptionをスロー
    /// </summary>
    public T Value => _isSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on Error result");

    /// <summary>
    /// エラー値を取得（エラー時のみ有効）
    /// 成功時はInvalidOperationExceptionをスロー
    /// </summary>
    public E Error => !_isSuccess
        ? _error!
        : throw new InvalidOperationException("Cannot access Error on Success result");

    /// <summary>
    /// 成功結果を作成
    /// </summary>
    public static Result<T, E> Success(T value) => new(value);

    /// <summary>
    /// エラー結果を作成
    /// </summary>
    public static Result<T, E> Failure(E error) => new(error);

    /// <summary>
    /// パターンマッチング用メソッド
    /// 成功時とエラー時それぞれに対する処理を記述できる
    /// </summary>
    /// <typeparam name="TResult">返却値の型</typeparam>
    /// <param name="onSuccess">成功時の処理</param>
    /// <param name="onError">エラー時の処理</param>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<E, TResult> onError)
    {
        return _isSuccess ? onSuccess(_value!) : onError(_error!);
    }

    /// <summary>
    /// void版のパターンマッチング
    /// </summary>
    public void Match(Action<T> onSuccess, Action<E> onError)
    {
        if (_isSuccess)
            onSuccess(_value!);
        else
            onError(_error!);
    }
}
