using System;
using BenchmarkDotNet.Attributes;

namespace Microsoft.Maui.Handlers.Benchmarks
{
	[MemoryDiagnoser]
	public class WeakEventBenchmarker
	{
		const int Iterations = 100;
		const string EventName = "MyEvent";

		void Handler(object sender, EventArgs e) { }

		[Benchmark]
		public void WeakEventManager_Default()
		{
			var eventManager = new WeakEventManager();

			for (int i = 0; i < Iterations; i++)
			{
				eventManager.AddEventHandler(Handler, EventName);
				eventManager.HandleEvent(this, EventArgs.Empty, EventName);
				eventManager.RemoveEventHandler(Handler, EventName);
			}
		}

		[Benchmark]
		public void WeakEventManager_WithSubscription()
		{
			var eventManager = new WeakEventManager();
			WeakEventManager.Subscription subscription;

			for (int i = 0; i < Iterations; i++)
			{
				subscription = eventManager.AddEventHandlerWithSubscription(Handler, EventName);
				eventManager.HandleEvent(this, EventArgs.Empty, EventName);
				eventManager.RemoveEventHandler(subscription);
			}
		}
	}
}
