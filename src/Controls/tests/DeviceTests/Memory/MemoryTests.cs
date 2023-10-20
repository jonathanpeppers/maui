﻿using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;

namespace Microsoft.Maui.DeviceTests.Memory;

[Category(TestCategory.Memory)]
public class MemoryTests : ControlsHandlerTestBase
{
	void SetupBuilder()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Border, BorderHandler>();
				handlers.AddHandler<CarouselView, CarouselViewHandler>();
				handlers.AddHandler<CheckBox, CheckBoxHandler>();
				handlers.AddHandler<DatePicker, DatePickerHandler>();
				handlers.AddHandler<Entry, EntryHandler>();
				handlers.AddHandler<Editor, EditorHandler>();
				handlers.AddHandler<GraphicsView, GraphicsViewHandler>();
				handlers.AddHandler<Label, LabelHandler>();
				handlers.AddHandler<ListView, ListViewRenderer>();
				handlers.AddHandler<Picker, PickerHandler>();
				handlers.AddHandler<IContentView, ContentViewHandler>();
				handlers.AddHandler<Image, ImageHandler>();
				handlers.AddHandler<RefreshView, RefreshViewHandler>();
				handlers.AddHandler<IScrollView, ScrollViewHandler>();
				handlers.AddHandler<SwipeView, SwipeViewHandler>();
				handlers.AddHandler<TimePicker, TimePickerHandler>();
			});
		});
	}

	[Theory("Handler Does Not Leak")]
	[InlineData(typeof(Border))]
	[InlineData(typeof(CarouselView))]
	[InlineData(typeof(ContentView))]
	[InlineData(typeof(CheckBox))]
	[InlineData(typeof(DatePicker))]
	[InlineData(typeof(Entry))]
	[InlineData(typeof(Editor))]
	[InlineData(typeof(GraphicsView))]
	[InlineData(typeof(Image))]
	[InlineData(typeof(Label))]
	[InlineData(typeof(Picker))]
	[InlineData(typeof(RefreshView))]
	[InlineData(typeof(ScrollView))]
	[InlineData(typeof(SwipeView))]
	[InlineData(typeof(TimePicker))]
	public async Task HandlerDoesNotLeak(Type type)
	{
		SetupBuilder();

#if ANDROID
		// NOTE: skip certain controls on older Android devices
		if (type == typeof (DatePicker) && !OperatingSystem.IsAndroidVersionAtLeast(30))
				return;
#endif

		WeakReference viewReference = null;
		WeakReference platformViewReference = null;
		WeakReference handlerReference = null;

		var observable = new ObservableCollection<int> { 1, 2, 3 };

		await InvokeOnMainThreadAsync(() =>
		{
			var layout = new Grid();
			var view = (View)Activator.CreateInstance(type);
			layout.Add(view);
			if (view is ContentView content)
			{
				content.Content = new Label();
			}
			else if (view is ItemsView items)
			{
				items.ItemTemplate = new DataTemplate(() => new Label());
				items.ItemsSource = observable;
			}
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

#if IOS
	[Fact]
	public async Task ResignFirstResponderTouchGestureRecognizer()
	{
		WeakReference viewReference = null;
		WeakReference recognizerReference = null;

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new UIKit.UIView();
			var recognizer = new Platform.ResignFirstResponderTouchGestureRecognizer(view);
			view.AddGestureRecognizer(recognizer);
			viewReference = new(view);
			recognizerReference = new(recognizer);
		});

		await AssertionExtensions.WaitForGC(viewReference, recognizerReference);
		Assert.False(viewReference.IsAlive, "UIView should not be alive!");
		Assert.False(recognizerReference.IsAlive, "ResignFirstResponderTouchGestureRecognizer should not be alive!");
	}
#endif
}

