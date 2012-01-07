using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Nomad.Commons.Controls.Actions;
using Nomad.Commons.Plugin;
using Nomad.FileSystem.Virtual;
using Nomad.Shared;

namespace Nomad.Plugin
{
  [Guid("65C71558-4FB5-4913-93B8-BAF81075344A")]
  [ExportExtension(typeof(IAction))]
  [Category("catTab")]
  [DisplayName("New Command Processor Tab")]
  [DefaultShortcuts(Keys.Control | Keys.P)]
  [ToolboxBitmap(typeof(NewCmdPromptTabCommand), "application_xp_terminal_add.png")]
  [ToolboxItem(false)]
  public class NewCmdPromptTabCommand : Component, IAction, ISupportInitialize
  {
    private static string GetFolderLocalPath(IVirtualFolder folder)
    {
      if (folder != null)
      {
        IGetFileSystemInfoService GetFileSystemInfo = folder.GetService<IGetFileSystemInfoService>();
        if (GetFileSystemInfo != null)
        {
          DirectoryInfo CurrentDirectory = GetFileSystemInfo.Info as DirectoryInfo;
          if (CurrentDirectory != null)
            return CurrentDirectory.FullName;
        }
      }
      return null;
    }

    public bool Execute(object source, object target)
    {
      var FolderView = (IFolderView)GetService(typeof(IFolderView));
      string CurrentFolderLocalPath = GetFolderLocalPath(FolderView.CurrentFolder);

      CommandProcessorController Controller = new CmdPromptTabController();
      if (Controller.Start(CurrentFolderLocalPath))
      {
        var TabManager = (ITabManager)GetService(typeof(ITabManager));

        ITab Tab = TabManager.AddNewTab(Controller.OutputBox, "Command Processor");
        Tab.Image = ToolboxBitmapAttribute.GetImageFromResource(typeof(NewCmdPromptTabCommand), "application_xp_terminal.png", false);
        Tab.Select();

        return true;
      }

      return false;
    }

    public ActionState Update(object source, object target)
    {
      return ActionState.Visible | ActionState.Enabled;
    }

    public void BeginInit()
    {
    }

    public void EndInit()
    {
      IMainForm MainForm = (IMainForm)GetService(typeof(IMainForm));
      ToolStripMenuItem TabItem = MainForm.MainMenuStrip.Items["tsmiTab"] as ToolStripMenuItem;
      if (TabItem != null)
      {
        ToolStripMenuItem NewCmdTabItem = new ToolStripMenuItem();
        ICommandManager CommandManager = (ICommandManager)GetService(typeof(ICommandManager));
        if (CommandManager.CreateLink(this, NewCmdTabItem) != null)
          TabItem.DropDownItems.Insert(1, NewCmdTabItem);
      }
    }
  }
}