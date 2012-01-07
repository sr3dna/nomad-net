using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using Nomad.Commons.Controls;
using Nomad.Shared;

namespace Nomad.Plugin
{
  public class CmdPromptTabController : CommandProcessorController
  {
    // In order to support duplicate tab functionality, control added to tab should implement ICloneable interface
    private class OutputTextBox : TextBoxEx, ICloneable
    {
      private CommandProcessorController _Owner;

      public OutputTextBox(CommandProcessorController owner)
      {
        _Owner = owner;
      }

      private static string GetFolderName(string currentDirectory)
      {
        string Result = Path.GetFileName(currentDirectory);
        if (Result.Length == 0)
          return currentDirectory;
        return Result;
      }

      public void UpdateTabCaption()
      {
        ITab Tab = (ITab)GetService(typeof(ITab));
        if (!string.IsNullOrEmpty(PermanentCaption))
          Tab.Caption = PermanentCaption;
        else
          Tab.Caption = GetFolderName(_Owner.CurrentDirectory) + " - cmd";
      }

      public object Clone()
      {
        CommandProcessorController Result = _Owner.Clone();
        return Result.Running ? Result.OutputBox : null;
      }

      public string PermanentCaption { get; set; }
    }

    private void OutputBox_DragEnter(object sender, DragEventArgs e)
    {
      DragImage.DragEnter((Control)sender, e);
      if (((e.AllowedEffect & (DragDropEffects.Copy | DragDropEffects.Link)) > 0) && Idling && e.Data.HasFileName())
        e.Effect = DragDropEffects.Copy;
      else
        e.Effect = DragDropEffects.None;
    }

    private void OutputBox_DragOver(object sender, DragEventArgs e)
    {
      DragImage.DragOver((Control)sender, e);
      if (!Idling)
        e.Effect = DragDropEffects.None;
    }

    private void OutputBox_DragLeave(object sender, EventArgs e)
    {
      DragImage.DragLeave((Control)sender);
    }

    private void OutputBox_DragDrop(object sender, DragEventArgs e)
    {
      DragImage.DragLeave((Control)sender);
      string FileName = e.Data.GetFileName();
      if (!string.IsNullOrEmpty(FileName) && Idling)
        InsertToInputBuffer(FileName);
    }

    protected override TextBox CreateOutputBox()
    {
      OutputTextBox Result = new OutputTextBox(this);
      Result.CustomWordBreaks = true;
      Result.DragEnter += OutputBox_DragEnter;
      Result.DragOver += OutputBox_DragOver;
      Result.DragLeave += OutputBox_DragLeave;
      Result.DragDrop += OutputBox_DragDrop;
      return Result;
    }

    protected override void OnBeforeCommand(BeforeCommandEventArgs e)
    {
      if (e.Command.StartsWith("title", System.StringComparison.OrdinalIgnoreCase))
      {
        OutputTextBox Output = (OutputTextBox)OutputBox;
        Output.PermanentCaption = null;
        if ((e.Command.Length > 5) && (e.Command[5] == ' '))
          Output.PermanentCaption = e.Command.Substring(6).Trim();
        Output.UpdateTabCaption();
      }
      base.OnBeforeCommand(e);
    }

    protected override void OnCurrentDirectoryChanged(EventArgs e)
    {
      ((OutputTextBox)OutputBox).UpdateTabCaption();
      base.OnCurrentDirectoryChanged(e);
    }

    public override Encoding DefaultEncoding
    {
      get { return Encoding.GetEncoding(Windows.GetOEMCP()); }
    }
  }
}