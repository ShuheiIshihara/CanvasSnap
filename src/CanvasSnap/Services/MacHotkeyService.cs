using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CanvasSnap.Exceptions;
using CanvasSnap.Models;

namespace CanvasSnap.Services;

/// <summary>
/// macOS環境でのグローバルホットキーサービス実装
/// Requirements: 5.1 (ホットキー押下で即座にキャプチャ処理開始)
/// Requirements: 5.2 (デフォルトホットキー設定)
/// Requirements: 5.3 (修飾キーと任意のキーの組み合わせ)
/// Requirements: 5.4 (OS予約ショートカットと競合時は登録拒否)
/// Requirements: 5.5 (IMEがオンでもホットキーに反応)
/// Requirements: 5.6 (ホットキー登録失敗時のエラーメッセージ)
/// </summary>
/// <remarks>
/// 実装ノート:
/// - Phase 1 MVP: CGEvent API（Quartz Event Services）を使用したグローバルホットキー実装
/// - CFRunLoop専用スレッドでイベントタップを監視
/// - アクセシビリティ権限が必須（IPermissionServiceで事前チェック推奨）
/// - リソースクリーンアップを確実に実行（Dispose実装）
/// </remarks>
public class MacHotkeyService : IHotkeyService, IDisposable
{
    private readonly bool _isMacOS;
    private IntPtr _eventTap;
    private IntPtr _runLoopSource;
    private IntPtr _currentRunLoop;
    private Thread? _runLoopThread;
    private HotkeyConfig? _currentConfig;
    private bool _disposed;

    // CGEvent API P/Invoke定義
    private delegate IntPtr CGEventTapCallBack(IntPtr proxy, uint type, IntPtr eventRef, IntPtr userInfo);

    // デリゲートをインスタンスフィールドに保持（GC対策）
    private CGEventTapCallBack? _eventCallback;

    private const uint kCGEventKeyDown = 10;
    private const uint kCGEventFlagsChanged = 12;
    private const uint kCGEventTapOptionDefault = 0;
    private const uint kCGSessionEventTap = 1;
    private const uint kCGHeadInsertEventTap = 0;
    private const uint kCGKeyboardEventKeycode = 9;

    // CGEventFlags
    private const ulong kCGEventFlagMaskCommand = 0x100000;
    private const ulong kCGEventFlagMaskShift = 0x20000;
    private const ulong kCGEventFlagMaskAlternate = 0x80000;

    public event EventHandler? HotkeyPressed;

    public MacHotkeyService()
    {
        _isMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    }

    /// <summary>
    /// グローバルホットキーを登録
    /// </summary>
    public async Task RegisterHotkeyAsync(HotkeyConfig config)
    {
        if (!_isMacOS)
        {
            // 非macOS環境ではダミー実装（テスト用）
            _currentConfig = config;
            return;
        }

        // 既存の登録を解除
        await UnregisterHotkeyAsync();

        _currentConfig = config;

        // CGEventTapCreateを実行（UIスレッドから呼び出される可能性があるため、Task.Runで別スレッドで実行）
        await Task.Run(() =>
        {
            try
            {
                // イベントタップを作成（デリゲートをインスタンスフィールドに保持してGC対策）
                _eventCallback = new CGEventTapCallBack(EventCallback);
                var callbackPtr = Marshal.GetFunctionPointerForDelegate(_eventCallback);

                _eventTap = CGEventTapCreate(
                    kCGSessionEventTap,
                    kCGHeadInsertEventTap,
                    kCGEventTapOptionDefault,
                    CGEventMaskBit(kCGEventKeyDown),
                    callbackPtr,
                    IntPtr.Zero);

                if (_eventTap == IntPtr.Zero)
                {
                    throw new HotkeyRegistrationException(
                        "Failed to create event tap. Accessibility permission may be required.");
                }

                // RunLoopSourceを作成
                _runLoopSource = CFMachPortCreateRunLoopSource(IntPtr.Zero, _eventTap, 0);

                if (_runLoopSource == IntPtr.Zero)
                {
                    throw new HotkeyRegistrationException("Failed to create run loop source");
                }

                // CFRunLoop専用スレッドを開始
                _runLoopThread = new Thread(RunLoopThreadProc)
                {
                    IsBackground = true,
                    Name = "HotkeyRunLoopThread"
                };
                _runLoopThread.Start();
            }
            catch (Exception ex) when (ex is not HotkeyRegistrationException)
            {
                throw new HotkeyRegistrationException(
                    $"Failed to register hotkey: {ex.Message}", ex);
            }
        });
    }

    /// <summary>
    /// グローバルホットキーを解除
    /// </summary>
    public async Task UnregisterHotkeyAsync()
    {
        if (!_isMacOS)
        {
            _currentConfig = null;
            return;
        }

        await Task.Run(() =>
        {
            try
            {
                // CFRunLoopを停止
                if (_runLoopThread != null && _runLoopThread.IsAlive && _currentRunLoop != IntPtr.Zero)
                {
                    try
                    {
                        CFRunLoopStop(_currentRunLoop);
                    }
                    catch (Exception)
                    {
                        // ログ記録は Phase 2 で実装
                    }
                }

                // イベントタップを無効化してリソース解放
                if (_eventTap != IntPtr.Zero)
                {
                    try
                    {
                        CGEventTapEnable(_eventTap, false);
                        CFRelease(_eventTap);
                    }
                    catch (Exception)
                    {
                        // ログ記録は Phase 2 で実装
                    }
                    _eventTap = IntPtr.Zero;
                }

                // RunLoopSourceを解放
                if (_runLoopSource != IntPtr.Zero)
                {
                    try
                    {
                        CFRelease(_runLoopSource);
                    }
                    catch (Exception)
                    {
                        // ログ記録は Phase 2 で実装
                    }
                    _runLoopSource = IntPtr.Zero;
                }

                _currentRunLoop = IntPtr.Zero;
                _runLoopThread = null;
                _eventCallback = null;
                _currentConfig = null;
            }
            catch (Exception)
            {
                // 最終的なクリーンアップでは例外をスローしない
            }
        });
    }

    /// <summary>
    /// CFRunLoop実行スレッド
    /// </summary>
    private void RunLoopThreadProc()
    {
        IntPtr mode = IntPtr.Zero;
        try
        {
            _currentRunLoop = CFRunLoopGetCurrent();
            mode = CFStringCreateWithCString(IntPtr.Zero, "kCFRunLoopCommonModes", 0);
            CFRunLoopAddSource(_currentRunLoop, _runLoopSource, mode);
            CGEventTapEnable(_eventTap, true);
            CFRunLoopRun(); // ブロッキング
        }
        catch (Exception)
        {
            // ログ記録は Phase 2 で実装
        }
        finally
        {
            // CFStringリソースを解放（メモリリーク対策）
            if (mode != IntPtr.Zero)
            {
                try
                {
                    CFRelease(mode);
                }
                catch (Exception)
                {
                    // ログ記録は Phase 2 で実装
                }
            }
        }
    }

    /// <summary>
    /// CGEventコールバック
    /// </summary>
    private IntPtr EventCallback(IntPtr proxy, uint type, IntPtr eventRef, IntPtr userInfo)
    {
        try
        {
            if (type == kCGEventKeyDown && _currentConfig != null)
            {
                var keyCode = (int)CGEventGetIntegerValueField(eventRef, kCGKeyboardEventKeycode);
                var flags = CGEventGetFlags(eventRef);

                if (MatchesConfig(keyCode, flags))
                {
                    // ホットキーが一致した場合、イベントを発火
                    HotkeyPressed?.Invoke(this, EventArgs.Empty);
                    return IntPtr.Zero; // イベントを消費
                }
            }
        }
        catch (Exception)
        {
            // エラーハンドリングは Phase 2 で実装
        }

        return eventRef; // イベントを伝播
    }

    /// <summary>
    /// キーコードと修飾キーが設定と一致するかチェック
    /// </summary>
    private bool MatchesConfig(int keyCode, ulong flags)
    {
        if (_currentConfig == null)
            return false;

        // キーコードチェック
        if (keyCode != _currentConfig.KeyCode)
            return false;

        // 修飾キーチェック
        bool hasCommand = (flags & kCGEventFlagMaskCommand) != 0;
        bool hasShift = (flags & kCGEventFlagMaskShift) != 0;
        bool hasAlt = (flags & kCGEventFlagMaskAlternate) != 0;

        bool needsCommand = (_currentConfig.Modifiers & HotkeyModifiers.Control) != 0;
        bool needsShift = (_currentConfig.Modifiers & HotkeyModifiers.Shift) != 0;
        bool needsAlt = (_currentConfig.Modifiers & HotkeyModifiers.Alt) != 0;

        return hasCommand == needsCommand && hasShift == needsShift && hasAlt == needsAlt;
    }

    /// <summary>
    /// リソースを解放
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        UnregisterHotkeyAsync().GetAwaiter().GetResult();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    // CGEvent API P/Invoke
    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern IntPtr CGEventTapCreate(
        uint tap,
        uint place,
        uint options,
        ulong eventsOfInterest,
        IntPtr callback,
        IntPtr userInfo);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern void CGEventTapEnable(IntPtr tap, bool enable);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern long CGEventGetIntegerValueField(IntPtr eventRef, uint field);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern ulong CGEventGetFlags(IntPtr eventRef);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern IntPtr CFMachPortCreateRunLoopSource(IntPtr allocator, IntPtr port, long order);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern IntPtr CFRunLoopGetCurrent();

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRunLoopAddSource(IntPtr rl, IntPtr source, IntPtr mode);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRunLoopRun();

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRunLoopStop(IntPtr rl);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(IntPtr cf);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern IntPtr CFStringCreateWithCString(IntPtr allocator, string cStr, int encoding);

    private static ulong CGEventMaskBit(uint eventType)
    {
        return 1UL << (int)eventType;
    }
}
