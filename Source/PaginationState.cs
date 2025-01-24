using System;

using Microsoft.AspNetCore.Components;

namespace DataNavigator.Blazor;

/// <summary>
/// Holds state to represent pagination in a <see cref="DataNavigator{TGridItem}"/>.
/// </summary>
public class PaginationState
{
	public PaginationState()
	{
	}

	/// <summary>
	/// Gets or sets the number of items on each page.
	/// </summary>
	public int ItemsPerPage { get; set; } = 10;

	/// <summary>
	/// Gets the current zero-based page index. To set it, call <see cref="SetCurrentPageIndexAsync(int)" />.
	/// </summary>
	public int CurrentPageIndex { get; internal set; }

	/// <summary>
	/// Gets the total number of items across all pages, if known. The value will be null until an
	/// associated <see cref="DataNavigator{TGridItem}"/> assigns a value after loading data.
	/// </summary>
	public int? TotalItemCount { get; internal set; }

	/// <summary>
	/// Gets the zero-based index of the last page, if known. The value will be null until <see cref="TotalItemCount"/> is known.
	/// </summary>
	public int? LastPageIndex => (TotalItemCount - 1) / ItemsPerPage;

	readonly HashSet<IPaginationStateSubscriber> _subscribers = new();

	public void AddSubscriber(IPaginationStateSubscriber subscriber)
	{
		_subscribers.Add(subscriber);
	}

	public void RemoveSubscriber(IPaginationStateSubscriber subscriber)
	{
		_subscribers.Remove(subscriber);
	}

	public Task GoFirstAsync()    => SetCurrentPageIndexAsync(0);
	public Task GoPreviousAsync() => SetCurrentPageIndexAsync(CurrentPageIndex - 1);
	public Task GoNextAsync()     => SetCurrentPageIndexAsync(CurrentPageIndex + 1);
	public Task GoLastAsync()     => SetCurrentPageIndexAsync(LastPageIndex.GetValueOrDefault(0));
	public bool CanGoBack         => CurrentPageIndex > 0;
	public bool CanGoForwards     => CurrentPageIndex < LastPageIndex;

	/// <summary>
	/// Sets the current page index, and notifies any associated <see cref="DataNavigator{TGridItem}"/>
	/// to fetch and render updated data.
	/// </summary>
	/// <param name="pageIndex">The new, zero-based page index.</param>
	/// <returns>A <see cref="Task"/> representing the completion of the operation.</returns>
	public async Task SetCurrentPageIndexAsync(int pageIndex)
	{
		CurrentPageIndex = pageIndex;
		foreach (var subscriber in _subscribers)
			await subscriber.CurrentPageItemsChangedAsync(this, pageIndex);
	}

	public async Task SetTotalItemCountAsync(int totalItemCount)
	{
		TotalItemCount = totalItemCount;

		foreach (var subscriber in _subscribers)
			await subscriber.TotalItemCountChangedAsync(this, totalItemCount);

		if (CurrentPageIndex > 0 && CurrentPageIndex > LastPageIndex)
		{
			// If the number of items has reduced such that the current page index is no longer valid,
			// move automatically to the final valid page index and trigger a further data load.
			//
			await SetCurrentPageIndexAsync(LastPageIndex.Value);
		}
	}

	public async Task StateHasChangedAsync()
	{
		foreach (var subscriber in _subscribers)
			await subscriber.StateHasChangedAsync(this);
	}
}
