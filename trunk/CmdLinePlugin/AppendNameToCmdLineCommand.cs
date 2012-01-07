using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Nomad.Commons.Controls.Actions;
using Nomad.Commons.IO;
using Nomad.Commons.Plugin;
using Nomad.FileSystem.Virtual;
using Nomad.Shared;

namespace Nomad.Plugin
{
  public abstract class AppendNameToCmdLineCommandBase : CmdLineCommandBase
  {
    // Convert command target to selected item
    protected IVirtualItem GetTarget(object target)
    {
      // Target can be item itself
      IVirtualItem Result = target as IVirtualItem;
      if (Result != null)
        return Result;

      // Or collection of items
      var Collection = target as IEnumerable<IVirtualItem>;
      foreach (IVirtualItem NextItem in Collection.AsEnumerable())
      {
        if (Result == null)
          Result = NextItem;
        else
          return null;
      }

      if (Result == null)
      {
        // Or panel
        IPanel Panel = target as IPanel;
        if (Panel != null)
          return Panel.FocusedItem;

        // If none of above, then get FocusedItem of current two panel tab
        ITwoPanelTab TwoPanelTab = CurrentTab as ITwoPanelTab;
        if (TwoPanelTab != null)
        {
          Result = TwoPanelTab.CurrentPanel.FocusedItem;
          if ((Result != null) && Result.Equals(TwoPanelTab.CurrentPanel.ParentFolder))
            return null;
        }
      }

      return Result;
    }

    protected bool AppendTextToCmdLine(string text)
    {
      CommandLinePanel CommandLine = CurrentCommandLine;
      if (CommandLine != null)
      {
        CommandLine.CommandLine += PathHelper.EnquoteString(text) + ' ';
        return true;
      }
      return false;
    }

    public ActionState Update(object source, object target)
    {
      ActionState Result = ActionState.Visible;
      if ((CurrentCommandLine != null) && (GetTarget(target) != null))
        Result |= ActionState.Enabled;
      return Result;
    }
  }

  [Guid("9D2AB5E3-3309-486E-A403-C152A27BBE40")]
  [ExportExtension(typeof(IAction))]
  [Category("catMisc")]
  [DisplayName("Append Name to Command Line")]
  [DefaultShortcuts(Keys.Control | Keys.Alt | Keys.Down)]
  [ToolboxItem(false)]
  public class AppendNameToCmdLineCommand : AppendNameToCmdLineCommandBase, IAction
  {
    public bool Execute(object source, object target)
    {
      IVirtualItem Item = GetTarget(target);
      return (Item != null) && AppendTextToCmdLine(Item.Name);
    }
  }

  [Guid("0A7FB1B3-9064-480A-A9BE-8B98A32F41C3")]
  [ExportExtension(typeof(IAction))]
  [Category("catMisc")]
  [DisplayName("Append Name and Path to Command Line")]
  [DefaultShortcuts(Keys.Control | Keys.Shift | Keys.Down)]
  [ToolboxItem(false)]
  public class AppendNameAndPathToCmdLineCommand : AppendNameToCmdLineCommandBase, IAction
  {
    public bool Execute(object source, object target)
    {
      IVirtualItem Item = GetTarget(target);
      return (Item != null) && AppendTextToCmdLine(Item.FullName);
    }
  }
}