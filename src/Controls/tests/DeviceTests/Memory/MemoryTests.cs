﻿using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;

namespace Microsoft.Maui.DeviceTests.Memory;

public class MemoryTests : ControlsHandlerTestBase
{
	void SetupBuilder()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Border, BorderHandler>();
				handlers.AddHandler<CheckBox, CheckBoxHandler>();
				handlers.AddHandler<Entry, EntryHandler>();
				handlers.AddHandler<Editor, EditorHandler>();
				handlers.AddHandler<Label, LabelHandler>();
				handlers.AddHandler<IContentView, ContentViewHandler>();
				handlers.AddHandler<Image, ImageHandler>();
				handlers.AddHandler<Page, PageHandler>();
				handlers.AddHandler<Shell, PageHandler>();
				handlers.AddHandler<RefreshView, RefreshViewHandler>();
				handlers.AddHandler<IScrollView, ScrollViewHandler>();
				handlers.AddHandler<SwipeView, SwipeViewHandler>();
				handlers.AddHandler<TimePicker, TimePickerHandler>();
			});
		});
	}

	[Theory("Handler Does Not Leak")]
	[InlineData(typeof(Border))]
	[InlineData(typeof(ContentView))]
	[InlineData(typeof(CheckBox))]
	[InlineData(typeof(Entry))]
	[InlineData(typeof(Editor))]
	[InlineData(typeof(Image))]
	[InlineData(typeof(Label))]
	[InlineData(typeof(RefreshView))]
	[InlineData(typeof(ScrollView))]
	[InlineData(typeof(SwipeView))]
	[InlineData(typeof(TimePicker))]
	public async Task HandlerDoesNotLeak(Type type)
	{
		SetupBuilder();

		WeakReference viewReference = null;
		WeakReference platformViewReference = null;
		WeakReference handlerReference = null;

		await InvokeOnMainThreadAsync(() =>
		{
			var layout = new Grid();
			var view = (View)Activator.CreateInstance(type);
			layout.Add(view);
			var handler = CreateHandler<LayoutHandler>(layout);
			viewReference = new WeakReference(view);
			handlerReference = new WeakReference(view.Handler);
			platformViewReference = new WeakReference(view.Handler.PlatformView);
		});

		await AssertionExtensions.WaitForGC(viewReference, handlerReference, platformViewReference);
		Assert.False(viewReference.IsAlive, $"{type} should not be alive!");
		Assert.False(handlerReference.IsAlive, "Handler should not be alive!");
		Assert.False(platformViewReference.IsAlive, "PlatformView should not be alive!");
	}

	[Theory("Page Does Not Leak")]
	[InlineData(typeof(Page))]
	[InlineData(typeof(ContentPage))]
	[InlineData(typeof(NavigationPage))]
	public async Task PageDoesNotLeak(Type type)
	{
		SetupBuilder();

		WeakReference pageReference = null;
		WeakReference platformViewReference = null;
		WeakReference handlerReference = null;

		await InvokeOnMainThreadAsync(() =>
		{
			var page = (Page)Activator.CreateInstance(type);
			var shell = new Shell
			{
				CurrentItem = new ShellContent
				{
					Content = page,
				}
			};
			var handler = CreateHandler<PageHandler>(shell);
			pageReference = new WeakReference(page);
			handlerReference = new WeakReference(page.Handler);
			platformViewReference = new WeakReference(page.Handler.PlatformView);
		});

		await AssertionExtensions.WaitForGC(pageReference, handlerReference, platformViewReference);
		Assert.False(pageReference.IsAlive, $"{type} should not be alive!");
		Assert.False(handlerReference.IsAlive, "Handler should not be alive!");
		Assert.False(platformViewReference.IsAlive, "PlatformView should not be alive!");
	}
}

