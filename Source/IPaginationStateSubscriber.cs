using System;

namespace DataNavigator.Blazor
{
	public interface IPaginationStateSubscriber
	{
		Task CurrentPageItemsChangedAsync(PaginationState caller, int currentPageIndex)
		{
			return StateHasChangedAsync(caller);
		}

		Task TotalItemCountChangedAsync  (PaginationState caller, int totalItemCount)
		{
			return StateHasChangedAsync(caller);
		}

		Task StateHasChangedAsync(PaginationState caller);
	}
}
