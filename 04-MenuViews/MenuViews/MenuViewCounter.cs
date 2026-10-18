using System.Threading;

namespace MenuViews;

public sealed class MenuViewCounter
{
    private int _views;

    public int Views => _views;

    public void RegisterView()
    {
        Interlocked.Increment(ref _views);
    }
}