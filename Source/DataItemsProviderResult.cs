using System;

namespace DataNavigator.Blazor
{
	public readonly struct DataItemsProviderResult<TDataItem>
	{
		/// <summary>
		/// The items being supplied.
		/// </summary>
		public required ICollection<TDataItem> Items { get; init; }

		/// <summary>
		/// The total number of items that may be displayed in the grid. This normally means the total number of items in the
		/// underlying data source after applying any filtering that is in effect.
		///
		/// If the grid is paginated, this should include all pages. If the grid is virtualized, this should include the entire scroll range.
		/// </summary>
		public int TotalItemCount { get; init; }
	}

	/// <summary>
	/// Provides convenience methods for constructing <see cref="DataItemsProviderResult{TDataItem}"/> instances.
	/// </summary>
	public static class DataItemsProviderResult
	{
		// This is just to provide generic type inference, so you don't have to specify TDataItem yet again.

		/// <summary>
		/// Supplies an instance of <see cref="DataItemsProviderResult{TDataItem}"/>.
		/// </summary>
		/// <typeparam name="TDataItem">The type of data represented by each row in the grid.</typeparam>
		/// <param name="items">The items being supplied.</param>
		/// <param name="totalItemCount">The total numer of items that exist. See <see cref="DataItemsProviderResult{TDataItem}.TotalItemCount"/> for details.</param>
		/// <returns>An instance of <see cref="DataItemsProviderResult{TDataItem}"/>.</returns>
		public static DataItemsProviderResult<TDataItem> From<TDataItem>(ICollection<TDataItem> items, int totalItemCount)
		{
			return new DataItemsProviderResult<TDataItem> { Items = items, TotalItemCount = totalItemCount };
		}
	}
}
