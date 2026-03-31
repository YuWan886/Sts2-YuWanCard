namespace YuWanCard.Utils;

public class RecursiveCallGuard<T>(Func<T, Task> action, Func<T, bool>? shouldProcess = null)
{
    private bool _isProcessing;
    private T _pendingValue = default!;
    private readonly Func<T, Task> _action = action ?? throw new ArgumentNullException(nameof(action));
    private readonly Func<T, bool> _shouldProcess = shouldProcess ?? (_ => true);
    private readonly bool _isReferenceType = !typeof(T).IsValueType;

    public bool TryExecute(T value)
    {
        if (_isProcessing)
        {
            return true;
        }

        if (!_shouldProcess(value))
        {
            return false;
        }

        _pendingValue = value;
        return false;
    }

    public async Task ExecutePendingAsync()
    {
        if (_isProcessing)
        {
            return;
        }

        if (_isReferenceType && _pendingValue == null)
        {
            return;
        }

        _isProcessing = true;
        try
        {
            var value = _pendingValue;
            _pendingValue = default!;
            await _action(value);
        }
        finally
        {
            _isProcessing = false;
        }
    }

    public void Reset()
    {
        _isProcessing = false;
        _pendingValue = default!;
    }
}
