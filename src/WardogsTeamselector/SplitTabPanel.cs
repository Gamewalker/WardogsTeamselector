using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WardogsTeamselector;

/// <summary>Places setup/operation on the left and the remaining tabs on the right.</summary>
public sealed class SplitTabPanel : Panel
{
    protected override Size MeasureOverride(Size available)
    {
        foreach (UIElement child in InternalChildren) child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var sizes = InternalChildren.Cast<UIElement>().Select(c => c.DesiredSize).ToArray();
        var left = sizes.Take(2).Sum(s => s.Width);
        var right = sizes.Skip(2).Sum(s => s.Width);
        var height = sizes.Select(s => s.Height).DefaultIfEmpty().Max();
        var width = double.IsInfinity(available.Width) ? left + right + 24 : available.Width;
        return new Size(width, left + right + 24 > width ? height * 2 : height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var children = InternalChildren.Cast<UIElement>().ToArray();
        var left = children.Take(2).Sum(c => c.DesiredSize.Width);
        var right = children.Skip(2).Sum(c => c.DesiredSize.Width);
        var height = children.Select(c => c.DesiredSize.Height).DefaultIfEmpty().Max();
        var secondRow = left + right + 24 > finalSize.Width;
        double x = 0;
        for (int i = 0; i < children.Length; i++)
        {
            if (i == 2) x = Math.Max(0, finalSize.Width - right);
            children[i].Arrange(new Rect(x, i >= 2 && secondRow ? height : 0, children[i].DesiredSize.Width, height));
            x += children[i].DesiredSize.Width;
        }
        return finalSize;
    }
}
