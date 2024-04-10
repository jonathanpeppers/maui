package com.microsoft.maui;

import android.content.Context;
import android.graphics.Bitmap;
import android.graphics.BlurMaskFilter;
import android.graphics.Canvas;
import android.graphics.Color;
import android.graphics.Paint;
import android.graphics.Path;
import android.graphics.PorterDuff;
import android.graphics.Rect;
import android.graphics.Shader;
import android.view.View;

import androidx.annotation.NonNull;

public abstract class PlatformWrapperView extends PlatformContentViewGroup {
    private static final int MaximumRadius = 100;

    public PlatformWrapperView(Context context) {
        super(context);
        this.viewBounds = new Rect();
        setClipChildren(false);
        setWillNotDraw(true);
    }

    private final Rect viewBounds;
    private boolean hasShadow, invalidateShadow;
    private Canvas shadowCanvas;
    private Paint shadowPaint;
    private Bitmap shadowBitmap;
    private Integer shadowColor, solidColor;
    private Shader shadowShader;
    private float shadowRadius;
    private float shadowOffsetX, shadowOffsetY;

    /**
     * Set by C#, disables calling drawShadow()
     */
    protected final void disableShadow() {
        hasShadow = false;
        clearShadowResources();
        postInvalidate();
    }

    /**
     * Set by C#, enables calling drawShadow()
     * @param shadowColor
     * @param solidColor
     * @param shadowRadius
     * @param shadowOffsetX
     * @param shadowOffsetY
     */
    protected final void enableShadow(int shadowColor, int solidColor, float shadowRadius, float shadowOffsetX, float shadowOffsetY) {
        hasShadow = true;
        invalidateShadow = true;
        this.shadowColor = shadowColor;
        this.solidColor = solidColor;
        this.shadowShader = null;
        this.shadowRadius = shadowRadius;
        this.shadowOffsetX = shadowOffsetX;
        this.shadowOffsetY = shadowOffsetY;
        postInvalidate();
    }

    /**
     * Set by C#, enables calling drawShadow()
     * @param shadowShader
     * @param shadowRadius
     * @param shadowOffsetX
     * @param shadowOffsetY
     */
    protected final void enableShadow(Shader shadowShader, float shadowRadius, float shadowOffsetX, float shadowOffsetY) {
        hasShadow = true;
        invalidateShadow = true;
        this.shadowColor = null;
        this.solidColor = null;
        this.shadowShader = shadowShader;
        this.shadowRadius = shadowRadius;
        this.shadowOffsetX = shadowOffsetX;
        this.shadowOffsetY = shadowOffsetY;
        postInvalidate();
    }

    @Override
    protected void setHasClip(boolean hasClip) {
        invalidateShadow = true;
        super.setHasClip(hasClip);
    }

    @Override
    public void requestLayout() {
        invalidateShadow = true;
        super.requestLayout();
    }

    @Override
    protected void onDetachedFromWindow() {
        super.onDetachedFromWindow();
        invalidateShadow = true;
        clearShadowResources();
    }

    private void clearShadowResources() {
        shadowCanvas = null;
        shadowPaint = null;
        if (shadowBitmap != null) {
            shadowBitmap.recycle();
            shadowBitmap = null;
        }
    }

    @Override
    protected void onMeasure(int widthMeasureSpec, int heightMeasureSpec) {
        if (getChildCount() == 0) {
            super.onMeasure(widthMeasureSpec, heightMeasureSpec);
            return;
        }

        View child = getChildAt(0);
        viewBounds.set(0, 0, MeasureSpec.getSize(widthMeasureSpec), MeasureSpec.getSize(heightMeasureSpec));
        child.measure(widthMeasureSpec, heightMeasureSpec);
        setMeasuredDimension(child.getMeasuredWidth(), child.getMeasuredHeight());
    }

    @Override
    protected void dispatchDraw(Canvas canvas) {
        // Only call into C# if there is a Shadow
        if (hasShadow) {
            int viewWidth = viewBounds.width();
            int viewHeight = viewBounds.height();
            if (getChildCount() > 0)
            {
                View child = getChildAt(0);
                if (viewWidth == 0)
                    viewWidth = child.getMeasuredWidth();
                if (viewHeight == 0)
                    viewHeight = child.getMeasuredHeight();
            }
            drawShadow(canvas, viewWidth, viewHeight);
        }
        super.dispatchDraw(canvas);
    }

    /**
     * Custom logic around shadows
     * @param canvas
     * @param viewWidth
     * @param viewHeight
     */
    protected void drawShadow(@NonNull Canvas canvas, int viewWidth, int viewHeight) {
        if (shadowCanvas == null)
            shadowCanvas = new Canvas();
        if (shadowPaint == null) {
            shadowPaint = new Paint() {{
                setAntiAlias(true);
                setDither(true);
                setFilterBitmap(true);
            }};
        }

        if (invalidateShadow) {
            // If bounds is zero
            if (viewHeight != 0 && viewWidth != 0) {
                int bitmapHeight = viewHeight + MaximumRadius;
                int bitmapWidth = viewWidth + MaximumRadius;

                // Reset bitmap to bounds
                shadowBitmap = Bitmap.createBitmap(bitmapWidth, bitmapHeight, Bitmap.Config.ARGB_8888);

                // Reset Canvas
                shadowCanvas.setBitmap(shadowBitmap);

                invalidateShadow = false;

                // Create the local copy of all content to draw bitmap as a
                // bottom layer of natural canvas.
                viewGroupDispatchDraw(shadowCanvas);

                // Get the alpha bounds of bitmap
                Bitmap extractAlpha = shadowBitmap.extractAlpha();

                // Clear past content content to draw shadow
                shadowCanvas.drawColor(Color.BLACK, PorterDuff.Mode.CLEAR);

                if (shadowColor != null) {
                    shadowPaint.setColor(shadowColor);
                }

                if (shadowShader != null) {
                    shadowPaint.setShader(shadowShader);
                }

                // Apply the shadow radius
                float radius = shadowRadius;

                if (radius <= 0)
                    radius = 0.01f;

                if (radius > MaximumRadius)
                    radius = MaximumRadius;

                shadowPaint.setMaskFilter(new BlurMaskFilter(radius, BlurMaskFilter.Blur.NORMAL));

                if (getHasClip()) {
                    Path clipPath = getClipPath(canvas.getWidth(), canvas.getHeight());
                    clipPath.offset(shadowOffsetX, shadowOffsetY);
                    shadowCanvas.drawPath(clipPath, shadowPaint);
                } else {
                    shadowCanvas.drawBitmap(extractAlpha, shadowOffsetX, shadowOffsetY, shadowPaint);
                }

                extractAlpha.recycle();
            } else {
                shadowBitmap = Bitmap.createBitmap(1, 1, Bitmap.Config.RGB_565);
            }
        }

        // Reset alpha to draw child with full alpha
        if (solidColor != null && shadowPaint != null) {
            shadowPaint.setColor(solidColor);
        }

        // Draw shadow bitmap
        if (shadowCanvas != null && shadowBitmap != null && !shadowBitmap.isRecycled()) {
            canvas.drawBitmap(shadowBitmap, 0, 0, shadowPaint);
        }
    }
}
