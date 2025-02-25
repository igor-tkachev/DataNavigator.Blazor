using System;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace DataNavigator.Blazor
{
	public class DataNavigator2<TDataItem> : IComponent, IPaginationStateSubscriber
	{
		public DataNavigator2()
		{
			Console.WriteLine($"--- DataNavigator.ctor ---");
		}

		[Inject] ILogger<DataNavigator2<TDataItem>> Logger           { get; set; }
		[Inject] PersistentComponentState           ApplicationState { get; set; } = default!;
		[Inject] IJSRuntime                         JSRuntime        { get; set; } = default!;
		[Inject] NavigationManager                  Navigation       { get; set; } = default!;

		[Parameter] public string?                       Id                 { get; set; }
		[Parameter] public PaginationState?              Pagination         { get; set; }
		[Parameter] public bool                          PersistState       { get; set; } = true;
		[Parameter] public bool                          CustomPersistState { get; set; } = true;
		[Parameter] public RenderFragment<TDataItem>?    ChildContent       { get; set; }
		[Parameter] public IQueryable<TDataItem>?        Items              { get; set; }
		[Parameter] public DataItemsProvider<TDataItem>? ItemsProvider      { get; set; }

		public bool IsLoading { get; private set; }

		PersistingComponentStateSubscription? _persistingSubscription;
		RenderHandle                          _renderHandle;
		ICollection<TDataItem>?               _items;
//		bool                                  _shouldRefresh = true;
//		bool                                  _shouldRender;
		int                                   _lastCurrentPageIndex;
		int                                   _lastTotalItemCount;
		bool                                  _isInitialized;

		#region IComponent Members

		void IComponent.Attach(RenderHandle renderHandle)
		{
			LogInfo();

			_renderHandle = renderHandle;
		}

		Task IComponent.SetParametersAsync(ParameterView parameters)
		{
			LogInfo(Navigation.Uri);

			parameters.SetParameterProperties(this);

#if DEBUG
			var ps = "";

			foreach (var parameter in parameters)
				ps += $"\n\t{parameter.Name}: {parameter.Value}";

			LogInfo(ps);
#endif

			if (!_isInitialized)
			{
				_isInitialized = true;

				return Initialize();
			}
			else
			{
				LogInfo("after initialized");
				return LoadData();
			}
		}

		#endregion

		#region IPaginationStateSubscriber

		Task IPaginationStateSubscriber.CurrentPageItemsChangedAsync(PaginationState caller, int pageIndex)
		{
			LogInfo();

//			if (_lastCurrentPageIndex != pageIndex)
//			{
//				return RefreshDataCoreAsync();
//			}

			return Task.CompletedTask;
		}

		Task IPaginationStateSubscriber.TotalItemCountChangedAsync(PaginationState caller, int totalItemCount)
		{
			LogInfo();

//			if (_lastTotalItemCount != totalItemCount)
//			{
//				return RefreshDataCoreAsync();
//			}

			return Task.CompletedTask;
		}

		Task IPaginationStateSubscriber.StateHasChangedAsync(PaginationState caller)
		{
			LogInfo();

			//return InvokeAsync(StateHasChanged);
			return Task.CompletedTask;
		}

		#endregion

		#region Init

		Task Initialize()
		{
			LogInfo();

			Pagination?.AddSubscriber(this);

			if (PersistState)
			{
				_persistingSubscription = ApplicationState.RegisterOnPersisting(PersistStateAsJson, RenderMode.InteractiveAuto);

				if (ApplicationState.TryTakeFromJson($"totalItemCount_{Id}", out int totalItemCount))
				{
					LogInfo("");

					_lastTotalItemCount = totalItemCount;
					if (Pagination is not null)
						Pagination.TotalItemCount = totalItemCount;

					if (ApplicationState.TryTakeFromJson($"pageIndex_{Id}", out int pageIndex))
					{
						_lastCurrentPageIndex = pageIndex;
						if (Pagination is not null)
							Pagination.CurrentPageIndex = pageIndex;
					}

					if (ApplicationState.TryTakeFromJson($"items_{Id}", out ICollection<TDataItem>? items))
						_items = items;

					//_shouldRefresh = false;
					//_shouldRender  = true;

					LogState("after persist");

					NotifyRender();

					return Task.CompletedTask;
				}
			}

			if (_items is null && CustomPersistState && _renderHandle.RendererInfo.Name == "WebAssembly")
			{
				return GetCustomState();
			}

			return LoadData();
		}

		#endregion

		#region Rendering

		void NotifyRender()
		{
			_renderHandle.Render(BuildRenderTree);
		}

		int _renderCount;

		void BuildRenderTree(RenderTreeBuilder builder)
		{
			var renderCount = Interlocked.Increment(ref _renderCount);

			try
			{
				if (renderCount == 1)
				{
//					if (!_shouldRender)
//						return;

					LogState($"{_items?.Count} items rendering...");

//					_shouldRender = false;

					if (_items is not null)
					{
						if (ChildContent is not null)
							foreach (var item in _items)
								builder.AddContent(0, ChildContent(item));
					}

					if (CustomPersistState)
					{
						if (_items != null)
						{
							builder.AddMarkupContent(1,
								$"""

								 <script>
								 	document.AppState{Id?.Replace('-', 'x')} = '{CompressStateJson()}';
								 </script>
								 """);
						}
						else
						{
							builder.AddMarkupContent(2,
								$"""

								<script>
									document.AppState{Id?.Replace('-', 'x')} = null;
								</script>
								""");
						}
					}
				}
			}
			finally
			{
				Interlocked.Decrement(ref _renderCount);
			}
		}

		#endregion

		#region State

		Task PersistStateAsJson()
		{
			LogInfo();
			LogState();

			if (_items is { Count: > 0 })
			{
				try
				{
					ApplicationState.PersistAsJson($"totalItemCount_{Id}", _lastTotalItemCount);
					ApplicationState.PersistAsJson($"pageIndex_{Id}",      _lastCurrentPageIndex);
					ApplicationState.PersistAsJson($"items_{Id}",          _items);
				}
				catch
				{
					// ignored
				}
			}

			return Task.CompletedTask;
		}

		JsonSerializerOptions _jsonOptions = new ()
		{
			WriteIndented        = false,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};

		public class DataNavigatorState
		{
			public List<TDataItem>? Items          { get; set; }
			public int              PageIndex      { get; set; }
			public int              TotalItemCount { get; set; }
			public bool             IsBrowser      { get; set; }
			public string?          RendererInfo   { get; set; }
			public bool             IsInteractive  { get; set; }
			public string           Id             { get; set; } = Guid.NewGuid().ToString();
		}

		string CompressStateJson()
		{
			LogInfo();

			var now = DateTime.Now;

			var data = new DataNavigatorState
			{
				Items          = _items?.ToList() ?? [],
				PageIndex      = _lastCurrentPageIndex,
				TotalItemCount = _lastTotalItemCount,
				IsBrowser      = OperatingSystem.IsBrowser(),
				RendererInfo   = _renderHandle.RendererInfo.Name,
				IsInteractive  = _renderHandle.RendererInfo.IsInteractive,
			};

			var jsonData = JsonSerializer.Serialize(data, _jsonOptions);

			using var output = new MemoryStream();
			using var gzip   = new GZipStream  (output, CompressionMode.Compress);
			using var writer = new StreamWriter(gzip, Encoding.UTF8);

			writer.Write(jsonData);
			writer.Flush();

			var compressStateJson = Convert.ToBase64String(output.ToArray());

			LogInfo(
				$"""
				serialized :
					compressState  = {compressStateJson.Length} in {DateTime.Now - now}
					Items          = {data.Items},
					PageIndex      = {data.PageIndex},
					TotalItemCount = {data.TotalItemCount},
					IsBrowser      = {data.IsBrowser},
					RendererInfo   = {data.RendererInfo},
					IsInteractive  = {data.IsInteractive},
					Id             = {data.Id}
				""");

			return compressStateJson;
		}

		async Task GetCustomState()
		{
			LogInfo();

			var now = DateTime.Now;

			var compressedBase64 = await JSRuntime.InvokeAsync<string>("eval", $"document.AppState{Id?.Replace('-', 'x')}");

			if (!string.IsNullOrEmpty(compressedBase64))
			{
				var data = Decompress(compressedBase64);

				if (data is not null)
				{
					_items                = data.Items;
					_lastCurrentPageIndex = data.PageIndex;
					_lastTotalItemCount   = data.TotalItemCount;

//					_shouldRefresh = false;
//					_shouldRender  = true;

					if (Pagination is not null)
					{
						Pagination.CurrentPageIndex = _lastCurrentPageIndex;
						Pagination.TotalItemCount   = _lastTotalItemCount;

						await Pagination.StateHasChangedAsync();
					}

					LogInfo(
						$"""
						 deserialized :
						 	compressState  = {compressedBase64.Length} in {DateTime.Now - now}
						 	Items          = {data.Items},
						 	PageIndex      = {data.PageIndex},
						 	TotalItemCount = {data.TotalItemCount},
						 	IsBrowser      = {data.IsBrowser},
						 	RendererInfo   = {data.RendererInfo},
						 	IsInteractive  = {data.IsInteractive},
						 	Id             = {data.Id}
						 """);

					NotifyRender();

					return;
				}
			}

			await LoadData();

			DataNavigatorState? Decompress(string compressedData)
			{
				if (string.IsNullOrEmpty(compressedData))
					return default;

				var compressedBytes = Convert.FromBase64String(compressedData);

				using var input  = new MemoryStream(compressedBytes);
				using var gzip   = new GZipStream  (input, CompressionMode.Decompress);
				using var reader = new StreamReader(gzip, Encoding.UTF8);

				var jsonData = reader.ReadToEnd();

				if (string.IsNullOrEmpty(jsonData))
					return default;

				return JsonSerializer.Deserialize<DataNavigatorState>(jsonData, _jsonOptions);
			}
		}

		#endregion

		#region LoadData

		IAsyncQueryExecutor?     _asyncQueryExecutor;
		CancellationTokenSource? _pendingDataLoadCancellationTokenSource;

		public Task RefreshDataAsync()
		{
			LogInfo();
			return LoadData();
		}

		async Task LoadData()
		{
			LogInfo();

//			_shouldRefresh = false;

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

					LogInfo("before request ItemsProvider");
					result = await ItemsProvider(request);
					LogInfo($"after request ItemsProvider {result.Value.TotalItemCount}");
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
//				_shouldRender         = true;

				if (Pagination is not null)
					await Pagination.SetTotalItemCountAsync(_lastTotalItemCount);

				LogState("after refresh state");

				_pendingDataLoadCancellationTokenSource = null;
			}

			NotifyRender();
		}

		#endregion

		#region Logging

		[Conditional("DEBUG")]
		void LogInfo(string? message = null, [CallerMemberName] string? callerName = null, [CallerLineNumber] int lineNumber = 0)
		{
			if (message is null)
				Logger.LogInformation($"--- DataNavigator2.{callerName} ({lineNumber}) ---");
			else
				Logger.LogInformation($"--- DataNavigator2.{callerName} ({lineNumber}) {message} ---");
		}

		[Conditional("DEBUG")]
		void LogState(string title = "", [CallerMemberName] string? callerName = null, [CallerLineNumber] int lineNumber = 0)
		{
			Logger.LogInformation(
				$"""
				--- DataNavigator2.{callerName} ({lineNumber}) {title} ---
					IsBrowser                : {OperatingSystem.IsBrowser()}
					RendererInfo             : {_renderHandle.RendererInfo.Name}
					IsInteractive            : {_renderHandle.RendererInfo.IsInteractive}
					_shouldRefresh           : {0/*_shouldRefresh*/}
					_shouldRender            : {0/*_shouldRender*/}
					Pagination?.ItemsPerPage : {Pagination?.ItemsPerPage}
					_items?.Count            : {_items?.Count}
					_lastCurrentPageIndex    : {_lastCurrentPageIndex,5} | {Pagination?.CurrentPageIndex, -5} : Pagination?.CurrentPageIndex
					_lastTotalItemCount      : {_lastTotalItemCount,5} | {Pagination?.TotalItemCount,-5} : Pagination?.TotalItemCount
				""");
		}

		#endregion
	}
}
