﻿using System;
using System.Collections.Generic;
using System.Globalization;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace SourceGit.Views
{
    public class CommitRefsPresenter : Control
    {
        public class RenderItem
        {
            public Geometry Icon { get; set; } = null;
            public FormattedText Label { get; set; } = null;
            public IBrush Brush { get; set; } = null;
            public bool IsHead { get; set; } = false;
            public double Width { get; set; } = 0.0;
        }

        public static readonly StyledProperty<FontFamily> FontFamilyProperty =
            TextBlock.FontFamilyProperty.AddOwner<CommitRefsPresenter>();

        public FontFamily FontFamily
        {
            get => GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        public static readonly StyledProperty<double> FontSizeProperty =
           TextBlock.FontSizeProperty.AddOwner<CommitRefsPresenter>();

        public double FontSize
        {
            get => GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public static readonly StyledProperty<IBrush> ForegroundProperty =
            AvaloniaProperty.Register<CommitRefsPresenter, IBrush>(nameof(Foreground), Brushes.White);

        public IBrush Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public static readonly StyledProperty<IBrush> TagBackgroundProperty =
            AvaloniaProperty.Register<CommitRefsPresenter, IBrush>(nameof(TagBackground), Brushes.White);

        public IBrush TagBackground
        {
            get => GetValue(TagBackgroundProperty);
            set => SetValue(TagBackgroundProperty, value);
        }

        static CommitRefsPresenter()
        {
            AffectsMeasure<CommitRefsPresenter>(
                FontFamilyProperty,
                FontSizeProperty,
                ForegroundProperty,
                TagBackgroundProperty);
        }

        public override void Render(DrawingContext context)
        {
            if (_items.Count == 0)
                return;

            var fg = Foreground;
            var x = 1.0;
            foreach (var item in _items)
            {
                var iconRect = new RoundedRect(new Rect(x, 0, 16, 16), new CornerRadius(2, 0, 0, 2));
                var entireRect = new RoundedRect(new Rect(x, 0, item.Width, 16), new CornerRadius(2));

                if (item.IsHead)
                {
                    using (context.PushOpacity(.6))
                        context.DrawRectangle(item.Brush, null, entireRect);

                    context.DrawText(item.Label, new Point(x + 16, 8.0 - item.Label.Height * 0.5));
                }
                else
                {
                    var labelRect = new RoundedRect(new Rect(x + 16, 0, item.Label.Width + 8, 16), new CornerRadius(0, 2, 2, 0));
                    using (context.PushOpacity(.2))
                        context.DrawRectangle(item.Brush, null, labelRect);

                    context.DrawLine(new Pen(item.Brush), new Point(x + 16, 0), new Point(x + 16, 16));
                    context.DrawText(item.Label, new Point(x + 20, 8.0 - item.Label.Height * 0.5));
                }

                context.DrawRectangle(null, new Pen(item.Brush), entireRect);

                using (context.PushTransform(Matrix.CreateTranslation(x + 3, 3)))
                    context.DrawGeometry(fg, null, item.Icon);

                x += item.Width + 4;
            }
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            InvalidateMeasure();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            _items.Clear();

            var commit = DataContext as Models.Commit;
            if (commit == null)
                return new Size(0, 0);

            var refs = commit.Decorators;
            if (refs != null && refs.Count > 0)
            {
                var typeface = new Typeface(FontFamily);
                var typefaceBold = new Typeface(FontFamily, FontStyle.Normal, FontWeight.Bold);
                var fg = Foreground;
                var tagBG = TagBackground;
                var labelSize = FontSize;
                var requiredWidth = 0.0;

                foreach (var decorator in refs)
                {
                    var isHead = decorator.Type == Models.DecoratorType.CurrentBranchHead ||
                        decorator.Type == Models.DecoratorType.CurrentCommitHead;

                    var label = new FormattedText(
                        decorator.Name,
                        CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        isHead ? typefaceBold : typeface,
                        isHead ? labelSize + 1 : labelSize,
                        fg);

                    var item = new RenderItem() { Label = label, Brush = commit.Brush, IsHead = isHead };
                    StreamGeometry geo;
                    switch (decorator.Type)
                    {
                        case Models.DecoratorType.CurrentBranchHead:
                        case Models.DecoratorType.CurrentCommitHead:
                            geo = this.FindResource("Icons.Head") as StreamGeometry;
                            break;
                        case Models.DecoratorType.RemoteBranchHead:
                            geo = this.FindResource("Icons.Remote") as StreamGeometry;
                            break;
                        case Models.DecoratorType.Tag:
                            item.Brush = tagBG;
                            geo = this.FindResource("Icons.Tag") as StreamGeometry;
                            break;
                        default:
                            geo = this.FindResource("Icons.Branch") as StreamGeometry;
                            break;
                    }

                    var drawGeo = geo!.Clone();
                    var iconBounds = drawGeo.Bounds;
                    var translation = Matrix.CreateTranslation(-(Vector)iconBounds.Position);
                    var scale = Math.Min(10.0 / iconBounds.Width, 10.0 / iconBounds.Height);
                    var transform = translation * Matrix.CreateScale(scale, scale);
                    if (drawGeo.Transform == null || drawGeo.Transform.Value == Matrix.Identity)
                        drawGeo.Transform = new MatrixTransform(transform);
                    else
                        drawGeo.Transform = new MatrixTransform(drawGeo.Transform.Value * transform);

                    item.Icon = drawGeo;
                    item.Width = 16 + (isHead ? 0 : 4) + label.Width + 4;
                    _items.Add(item);

                    requiredWidth += item.Width + 4;
                }

                InvalidateVisual();
                return new Size(requiredWidth + 2, 16);
            }

            InvalidateVisual();
            return new Size(0, 0);
        }

        private List<RenderItem> _items = new List<RenderItem>();
    }
}
