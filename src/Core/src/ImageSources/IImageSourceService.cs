﻿#nullable enable
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Maui
{
	public interface IImageSourceService
	{
#if ANDROID
		Task<IImageSourceServiceResult<ResourceOrDrawable>?> GetDrawableAsync(
			IImageSource imageSource,
			Android.Content.Context context,
			CancellationToken cancellationToken = default);
#elif IOS
		Task<IImageSourceServiceResult<UIKit.UIImage>?> GetImageAsync(
			IImageSource imageSource,
			float scale = 1,
			CancellationToken cancellationToken = default);
#elif WINDOWS
		Task<IImageSourceServiceResult<UI.Xaml.Media.ImageSource>?> GetImageSourceAsync(
			IImageSource imageSource,
			float scale = 1,
			CancellationToken cancellationToken = default);
#endif
	}

	public interface IImageSourceService<in T> : IImageSourceService
		where T : IImageSource
	{
	}
}