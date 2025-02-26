using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace DataNavigator.Blazor;

public partial class DataNavigatorOld<TDataItem>
{
	public DataNavigatorOld()
	{
		LogState(".ctor");
	}

	/// <summary>
	/// Gets or sets the unique identifier.
	/// The value will be used as the HTML <see href="https://developer.mozilla.org/en-US/docs/Web/HTML/Global_attributes/id">global id attribute</see>.
	/// </summary>
	[Parameter]
	public string? Id { get; set; }

	/// <summary>
	/// Gets or sets a queryable source of data for the grid.
	///
	/// This could be in-memory data converted to queryable using the
	/// <see cref="System.Linq.Queryable.AsQueryable(System.Collections.IEnumerable)"/> extension method,
	/// or an EntityFramework DataSet or an <see cref="IQueryable"/> derived from it.
	///
	/// You should supply either <see cref="Items"/> or <see cref="ItemsProvider"/>, but not both.
	/// </summary>
	[Parameter]
	public IQueryable<TDataItem>? Items { get; set; }

	/// <summary>
	/// Gets or sets a callback that supplies data for the rid.
	///
	/// You should supply either <see cref="Items"/> or <see cref="ItemsProvider"/>, but not both.
	/// </summary>
	[Parameter]
	public DataItemsProvider<TDataItem>? ItemsProvider { get; set; }

	/// <summary>
	/// Gets or sets the child components of this instance.
	/// </summary>
	[Parameter]
	public RenderFragment<TDataItem> ChildContent { get; set; } = null!;

	/// <summary>
	/// Gets or sets the child components of this instance.
	/// </summary>
	[Parameter]
	public RenderFragment<TDataItem> EmptyContent { get; set; } = null!;

	/// <summary>
	/// Optionally links this <see cref="DataNavigator2{TGridItem}"/> instance with a <see cref="PaginationState"/> model,
	/// causing the grid to fetch and render only the current page of data.
	///
	/// This is normally used in conjunction with some UI logic
	/// that displays and updates the supplied <see cref="PaginationState"/> instance.
	/// </summary>
	[Parameter]
	public PaginationState? Pagination { get; set; }

	/// <summary>
	/// Instructs the repeater to re-fetch and render the current data from the supplied data source
	/// (either <see cref="Items"/> or <see cref="ItemsProvider"/>).
	/// </summary>
	/// <returns>A <see cref="Task"/> that represents the completion of the operation.</returns>
	public async Task RefreshDataAsync()
	{
		_shouldRefresh = true;
		await RefreshDataCoreAsync();
	}

	/// <summary>
	/// IQueryable only exposes synchronous query APIs. IAsyncQueryExecutor is an adapter that lets us invoke any
	/// async query APIs that might be available. We have built-in support for using EF Core's async query APIs.
	/// </summary>
	IAsyncQueryExecutor? _asyncQueryExecutor;

	CancellationTokenSource? _pendingDataLoadCancellationTokenSource;

	async Task RefreshDataCoreAsync()
	{
		LogInfo();
		LogState("start requesting data");

		_shouldRefresh = false;

		_pendingDataLoadCancellationTokenSource?.Cancel();

		var thisLoadCts = _pendingDataLoadCancellationTokenSource = new CancellationTokenSource();
		var startIndex  = Pagination is null ? 0 : Pagination.CurrentPageIndex * Pagination.ItemsPerPage;
		var request     = new DataItemsProviderRequest<TDataItem>(startIndex, Pagination?.ItemsPerPage, thisLoadCts.Token);

		DataItemsProviderResult<TDataItem>? result = null;

		try
		{
			if (ItemsProvider is not null)
			{
				IsLoading = true;

				LogState($"before request ItemsProvider");
				result = await ItemsProvider(request);
				LogState($"after request ItemsProvider {result.Value.TotalItemCount}");
			}
			else if (Items is not null)
			{
				var totalItemCount = _asyncQueryExecutor is null ? Items.Count() : await _asyncQueryExecutor.CountAsync(Items);
				var query          = request.ApplySorting(Items).Skip(request.StartIndex);

				if (request.Count.HasValue)
					query = query.Take(request.Count.Value);

				var resultArray = _asyncQueryExecutor is null ? query.ToArray() : await _asyncQueryExecutor.ToArrayAsync(query);

				result = DataItemsProviderResult.From(resultArray, totalItemCount);
			}
			else
			{
				result = DataItemsProviderResult.From(Array.Empty<TDataItem>(), 0);
			}
		}
		catch (TaskCanceledException)
		{
		}
		catch (OperationCanceledException)
		{
		}
		finally
		{
			IsLoading = false;
		}

		LogInfo($"DataNavigator RefreshDataCoreAsync: IsCancellationRequested {thisLoadCts.IsCancellationRequested}");

		if (!thisLoadCts.IsCancellationRequested && result is not null)
		{
			_items                = result.Value.Items;
			_lastCurrentPageIndex = Pagination?.CurrentPageIndex ?? 0;
			_lastTotalItemCount   = result.Value.TotalItemCount;
			_shouldRender         = true;

			if (Pagination is not null)
				await Pagination.SetTotalItemCountAsync(_lastTotalItemCount);

			LogState("after refresh state");

			_pendingDataLoadCancellationTokenSource = null;

			await InvokeAsync(StateHasChanged);
		}
	}

	[Conditional("DEBUG")]
	void LogInfo(string? message = null, [CallerMemberName] string? callerName = null, [CallerLineNumber] int lineNumber = 0)
	{
		if (message is null)
			Console.WriteLine($"--- DataNavigator.{callerName} ({lineNumber}) ---");
		else
			Logger.LogInformation($"--- DataNavigator.{callerName} ({lineNumber}) {message} ---");
	}

	[Conditional("DEBUG")]
	void LogState(string title = "", [CallerMemberName] string? callerName = null, [CallerLineNumber] int lineNumber = 0)
	{
		if (Logger is null)
		{
			Console.WriteLine($"--- DataNavigator.{callerName} ({lineNumber}) {title} ---");
			return;
		}

		Logger.LogInformation(
			$"""
			--- DataNavigator.{callerName} ({lineNumber}) {title} ---
			IsBrowser                : {OperatingSystem.IsBrowser()}
			RendererInfo             : {RendererInfo.Name}
			IsInteractive            : {RendererInfo.IsInteractive}
			_shouldRefresh           : {_shouldRefresh}
			_shouldRender            : {_shouldRender}
			Pagination?.ItemsPerPage : {Pagination?.ItemsPerPage}
			_items?.Count            : {_items?.Count}
			_lastCurrentPageIndex    : {_lastCurrentPageIndex,5} | {Pagination?.CurrentPageIndex, -5} : Pagination?.CurrentPageIndex
			_lastTotalItemCount      : {_lastTotalItemCount,5} | {Pagination?.TotalItemCount,-5} : Pagination?.TotalItemCount
			""");
	}
}
