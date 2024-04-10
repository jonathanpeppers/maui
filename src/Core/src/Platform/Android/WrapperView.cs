#nullable disable
using System;
using Android.Content;
using Android.Graphics;
using Android.Views;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;
using APath = Android.Graphics.Path;
using AView = Android.Views.View;

namespace Microsoft.Maui.Platform
{
	public partial class WrapperView : PlatformWrapperView
	{
		const int MaximumRadius = 100;

		APath _currentPath;
		SizeF _lastPathSize;
		bool _invalidateClip;
		AView _borderView;

		public bool InputTransparent { get; set; }

		public WrapperView(Context context)
			: base(context)
		{
		}

		protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
		{
			_borderView?.BringToFront();

			if (ChildCount == 0 || GetChildAt(0) is not AView child)
				return;

			var widthMeasureSpec = MeasureSpecMode.Exactly.MakeMeasureSpec(right - left);
			var heightMeasureSpec = MeasureSpecMode.Exactly.MakeMeasureSpec(bottom - top);

			child.Measure(widthMeasureSpec, heightMeasureSpec);
			child.Layout(0, 0, child.MeasuredWidth, child.MeasuredHeight);
			_borderView?.Layout(0, 0, child.MeasuredWidth, child.MeasuredHeight);
		}

		public override bool DispatchTouchEvent(MotionEvent e)
		{
			if (InputTransparent)
			{
				return false;
			}

			return base.DispatchTouchEvent(e);
		}

		partial void ClipChanged()
		{
			_invalidateClip = true;
			SetHasClip(Clip is not null);
		}

		partial void ShadowChanged()
		{
			var paint = Shadow?.Paint;
			if (paint is not null)
			{
				var shadowOpacity = (float)Shadow.Opacity;
				if (paint is LinearGradientPaint linearGradientPaint)
				{
					var linearGradientShaderFactory = PaintExtensions.GetGradientShaderFactory(linearGradientPaint, shadowOpacity);
					var shader = linearGradientShaderFactory.Resize(1, 1); //TODO: this should be bitmapWidth, bitmapHeight
					EnableShadow(shader, (float)Shadow.Radius, (float)Shadow.Offset.X, (float)Shadow.Offset.Y);
				}
				else if (paint is RadialGradientPaint radialGradientPaint)
				{
					var radialGradientShaderFactory = PaintExtensions.GetGradientShaderFactory(radialGradientPaint, shadowOpacity);
					var shader = radialGradientShaderFactory.Resize(1, 1); //TODO: this should be bitmapWidth, bitmapHeight
					EnableShadow(shader, (float)Shadow.Radius, (float)Shadow.Offset.X, (float)Shadow.Offset.Y);
				}
				else if (paint is SolidPaint solidPaint)
				{
					var solidColor = solidPaint.ToColor();
					var shadowColor = solidColor.WithAlpha(shadowOpacity).ToPlatform();
					EnableShadow(shadowColor, solidColor.ToPlatform(), (float)Shadow.Radius, (float)Shadow.Offset.X, (float)Shadow.Offset.Y);
				}
			}
			else
			{
				DisableShadow();
			}
		}

		partial void BorderChanged()
		{
			if (Border == null)
			{
				if (_borderView != null)
					RemoveView(_borderView);
				_borderView = null;
				return;
			}

			if (_borderView == null)
			{
				AddView(_borderView = new AView(Context));
			}

			_borderView.UpdateBorderStroke(Border);
		}

		protected override APath GetClipPath(int width, int height)
		{
			var density = Context.GetDisplayDensity();
			var newSize = new SizeF(width, height);
			var bounds = new Graphics.RectF(Graphics.Point.Zero, newSize / density);

			if (_invalidateClip || _lastPathSize != newSize || _currentPath == null)
			{
				_invalidateClip = false;

				var path = Clip.PathForBounds(bounds);
				_currentPath = path?.AsAndroidPath(scaleX: density, scaleY: density);
				_lastPathSize = newSize;
			}

			return _currentPath;
		}

		public override ViewStates Visibility
		{
			get => base.Visibility;
			set
			{
				base.Visibility = value;

				if (value != ViewStates.Visible)
				{
					return;
				}

				for (int n = 0; n < this.ChildCount; n++)
				{
					var child = GetChildAt(n);
					child.Visibility = ViewStates.Visible;
				}
			}
		}

		internal static void SetupContainer(AView platformView, Context context, AView containerView, Action<AView> setWrapperView)
		{
			if (context == null || platformView == null || containerView != null)
				return;

			var oldParent = (ViewGroup)platformView.Parent;

			var oldIndex = oldParent?.IndexOfChild(platformView);
			oldParent?.RemoveView(platformView);

			containerView ??= new WrapperView(context);
			setWrapperView.Invoke(containerView);

			((ViewGroup)containerView).AddView(platformView);

			if (oldIndex is int idx && idx >= 0)
				oldParent?.AddView(containerView, idx);
			else
				oldParent?.AddView(containerView);
		}

		internal static void RemoveContainer(AView platformView, Context context, AView containerView, Action clearWrapperView)
		{
			if (context == null || platformView == null || containerView == null || platformView.Parent != containerView)
			{
				CleanupContainerView(containerView, clearWrapperView);
				return;
			}

			var oldParent = (ViewGroup)containerView.Parent;

			var oldIndex = oldParent?.IndexOfChild(containerView);
			oldParent?.RemoveView(containerView);

			CleanupContainerView(containerView, clearWrapperView);

			if (oldIndex is int idx && idx >= 0)
				oldParent?.AddView(platformView, idx);
			else
				oldParent?.AddView(platformView);

			void CleanupContainerView(AView containerView, Action clearWrapperView)
			{
				if (containerView is ViewGroup vg)
					vg.RemoveAllViews();

				clearWrapperView.Invoke();
			}
		}
	}
}