using System;

namespace DataNavigator.Blazor
{
	/// <summary>
	/// A callback that provides data for a <see cref="DataNavigator{TDataItem}"/>.
	/// </summary>
	/// <typeparam name="TDataItem">The type of data represented by each row in the grid.</typeparam>
	/// <param name="request">Parameters describing the data being requested.</param>
	/// <returns>A <see cref="T:ValueTask{DataItemsProviderResult{TResult}}" /> that gives the data to be displayed.</returns>
	public delegate ValueTask<DataItemsProviderResult<TDataItem>> DataItemsProvider<TDataItem>(DataItemsProviderRequest<TDataItem> request);
}
