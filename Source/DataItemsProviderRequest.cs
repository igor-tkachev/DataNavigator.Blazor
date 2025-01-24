using System;

namespace DataNavigator.Blazor;

/// <summary>
/// Parameters for data to be supplied by a <see cref="DataNavigator{TDataItem}"/>'s <see cref="DataNavigator{TDataItem}.ItemsProvider"/>.
/// </summary>
/// <typeparam name="TDataItem">The type of data represented by each row in the data repeater.</typeparam>
public class DataItemsProviderRequest<TDataItem>
{
	/// <summary>
	/// The zero-based index of the first item to be supplied.
	/// </summary>
	public int StartIndex { get; init; }

	/// <summary>
	/// If set, the maximum number of items to be supplied. If not set, the maximum number is unlimited.
	/// </summary>
	public int? Count { get; init; }

//		/// <summary>
//		/// Specifies which column represents the sort order.
//		///
//		/// Rather than inferring the sort rules manually, you should normally call either <see cref="ApplySorting(IQueryable{TDataItem})"/>
//		/// or <see cref="GetSortByProperties"/>, since they also account for <see cref="SortByColumn" /> and <see cref="SortByAscending" /> automatically.
//		/// </summary>
//		public ColumnBase<TDataItem>? SortByColumn { get; init; }

//		/// <summary>
//		/// Specifies the current sort direction.
//		///
//		/// Rather than inferring the sort rules manually, you should normally call either <see cref="ApplySorting(IQueryable{TDataItem})"/>
//		/// or <see cref="GetSortByProperties"/>, since they also account for <see cref="SortByColumn" /> and <see cref="SortByAscending" /> automatically.
//		/// </summary>
//		public bool SortByAscending { get; init; }

	/// <summary>
	/// A token that indicates if the request should be cancelled.
	/// </summary>
	public CancellationToken CancellationToken { get; init; }

	internal DataItemsProviderRequest(int startIndex, int? count, CancellationToken cancellationToken)
	{
		StartIndex        = startIndex;
		Count             = count;
		CancellationToken = cancellationToken;
	}

	/// <summary>
	/// Applies the request's sorting rules to the supplied <see cref="IQueryable{TGridItem}"/>.
	/// </summary>
	/// <param name="source">An <see cref="IQueryable{TGridItem}"/>.</param>
	/// <returns>A new <see cref="IQueryable{TGridItem}"/> representing the <paramref name="source"/> with sorting rules applied.</returns>
	public IQueryable<TDataItem> ApplySorting(IQueryable<TDataItem> source)
	{
		return source;
		//return SortByColumn?.SortBy?.Apply(source, SortByAscending) ?? source;
	}

//		/// <summary>
//		/// Produces a collection of (property name, direction) pairs representing the sorting rules.
//		/// </summary>
//		/// <returns>A collection of (property name, direction) pairs representing the sorting rules</returns>
//		public IReadOnlyCollection<SortedProperty> GetSortByProperties() =>
//			SortByColumn?.SortBy?.ToPropertyList(SortByAscending) ?? Array.Empty<SortedProperty>();
}

