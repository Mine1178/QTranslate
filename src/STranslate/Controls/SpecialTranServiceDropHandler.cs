using GongSolutions.Wpf.DragDrop;
using STranslate.Plugin;
using STranslate.ViewModels.Pages;
using System.Windows;

namespace STranslate.Controls;

public class ReplaceServiceDropHandler : IDropTarget
{
    public void DragOver(IDropInfo dropInfo)
    {
        // 检查拖拽的数据是否为 Service 类型
        if (dropInfo.Data is Service)
        {
            dropInfo.Effects = DragDropEffects.Copy;
            dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
        }
        else
        {
            dropInfo.Effects = DragDropEffects.None;
        }
    }

    public void Drop(IDropInfo dropInfo)
    {
        // 处理放置操作
        if (dropInfo.Data is Service service)
        {
            // 找到包含 TranslateViewModel 的 DataContext
            var target = dropInfo.VisualTarget as FrameworkElement;
            while (target != null)
            {
                if (target.DataContext is TranslateViewModel viewModel)
                {
                    // 调用 ViewModel 的命令来设置替换服务
                    viewModel.ActiveReplaceCommand?.Execute(service);
                    break;
                }
                target = target.Parent as FrameworkElement;
                target ??= System.Windows.Media.VisualTreeHelper.GetParent(dropInfo.VisualTarget as DependencyObject) as FrameworkElement;
            }
        }
    }
}

public class ImTranServiceDropHandler : IDropTarget
{
    public void DragOver(IDropInfo dropInfo)
    {
        // 检查拖拽的数据是否为 Service 类型
        if (dropInfo.Data is Service)
        {
            dropInfo.Effects = DragDropEffects.Copy;
            dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
        }
        else
        {
            dropInfo.Effects = DragDropEffects.None;
        }
    }

    public void Drop(IDropInfo dropInfo)
    {
        // 处理放置操作
        if (dropInfo.Data is Service service)
        {
            // 找到包含 TranslateViewModel 的 DataContext
            var target = dropInfo.VisualTarget as FrameworkElement;
            while (target != null)
            {
                if (target.DataContext is TranslateViewModel viewModel)
                {
                    // 调用 ViewModel 的命令来设置替换服务
                    viewModel.ActiveImTranCommand?.Execute(service);
                    break;
                }
                target = target.Parent as FrameworkElement;
                target ??= System.Windows.Media.VisualTreeHelper.GetParent(dropInfo.VisualTarget as DependencyObject) as FrameworkElement;
            }
        }
    }
}

public class ImTranOcrServiceDropHandler : IDropTarget
{
    public void DragOver(IDropInfo dropInfo)
    {
        if (dropInfo.Data is Service { Plugin: IOcrPlugin plugin } && plugin.SupportBoxPoints())
        {
            dropInfo.Effects = DragDropEffects.Copy;
            dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
        }
        else
        {
            dropInfo.Effects = DragDropEffects.None;
        }
    }

    public void Drop(IDropInfo dropInfo)
    {
        // OCR 功能已移除，拖放激活逻辑不再需要。
    }
}
