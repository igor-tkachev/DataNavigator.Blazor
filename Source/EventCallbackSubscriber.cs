using System;

using Microsoft.AspNetCore.Components;

namespace DataNavigator.Blazor;

/// <summary>
/// Represents a subscriber that may be subscribe to an <see cref="EventCallbackSubscribable{T}"/>.
/// The subscription can move between <see cref="EventCallbackSubscribable{T}"/> instances over time,
/// and automatically unsubscribes from earlier <see cref="EventCallbackSubscribable{T}"/> instances
/// whenever it moves to a new one.
/// </summary>
sealed class EventCallbackSubscriber<T>(EventCallback<T> handler) : IDisposable
{
	EventCallbackSubscribable<T>? _existingSubscription;

	/// <summary>
	/// Creates a subscription on the <paramref name="subscribable"/>, or moves any existing subscription to it
	/// by first unsubscribing from the previous <see cref="EventCallbackSubscribable{T}"/>.
	///
	/// If the supplied <paramref name="subscribable"/> is null, no new subscription will be created, but any
	/// existing one will still be unsubscribed.
	/// </summary>
	/// <param name="subscribable"></param>
	public void SubscribeOrMove(EventCallbackSubscribable<T>? subscribable)
	{
		if (subscribable != _existingSubscription)
		{
			_existingSubscription?.Unsubscribe(this);
			subscribable?.Subscribe(this, handler);
			_existingSubscription = subscribable;
		}
	}

	public void Dispose()
	{
		_existingSubscription?.Unsubscribe(this);
	}
}
