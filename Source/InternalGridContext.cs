using System;

namespace DataNavigator.Blazor;

// The grid cascades this so that descendant columns can talk back to it. It's an internal type
// so that it doesn't show up by mistake in unrelated components.
sealed class InternalGridContext<TGridItem>(DataNavigator<TGridItem> grid)
{
	int _index;
	int _rowId;
	int _cellId;

	public ICollection<TGridItem>             Items                 { get; set; } = [];
	public int                                TotalItemCount        { get; set; }
	public int                                TotalViewItemCount    { get; set; }
	public DataNavigator<TGridItem>           Grid                  { get; } = grid;
	public EventCallbackSubscribable<object?> ColumnsFirstCollected { get; } = new();

	public int GetNextRowId()
	{
		Interlocked.Increment(ref _rowId);
		return _rowId;
	}

	public int GetNextCellId()
	{
		Interlocked.Increment(ref _cellId);
		return _cellId;
	}

	internal void ResetRowIndexes(int start)
	{
		_index = start;
	}
}
