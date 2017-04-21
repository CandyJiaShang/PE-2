using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace PEFile
{
    public partial class MainForm : Form
    {
        private int childFormNumber = 0;
        private FileForm pChildForm;
        

        public MainForm()
        {
            InitializeComponent();
        }

        private void OpenFile(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            openFileDialog.Filter = "Exe Files (*.exe)|*.exe|Dynamic Libary Files (*.dll)|*.dll|Driver Files (*.ocx)|*.ocx|Memory Images Files (*.com)|*.com|System Files (*.sys)|*.sys|All Files (*.*)|*.*";
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                if (File.Exists(openFileDialog.FileName))
                {
                    pChildForm = new FileForm();
                    pChildForm.MdiParent = this;
                    pChildForm.Text = openFileDialog.FileName.Substring(openFileDialog.FileName.LastIndexOf("\\") + 1);
                    pChildForm.Show();
                    try
                    {
                        pChildForm.OpenPEFile(openFileDialog.FileName);
                        pChildForm.DrawSummary();
                    }
                    catch (ArgumentException ex)
                    {
                        MessageBox.Show(ex.Message);
                        return;
                    }
                    
                }
            }
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            saveFileDialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
            if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                string FileName = saveFileDialog.FileName;
            }
        }

        private void ExitToolsStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void CutToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void CopyToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void PasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void ToolBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStrip.Visible = toolBarToolStripMenuItem.Checked;
        }

        private void StatusBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            statusStrip.Visible = statusBarToolStripMenuItem.Checked;
        }

        private void CascadeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.Cascade);
        }

        private void TileVerticalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.TileVertical);
        }

        private void TileHorizontalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.TileHorizontal);
        }

        private void ArrangeIconsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.ArrangeIcons);
        }

        private void CloseAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form childForm in MdiChildren)
            {
                childForm.Close();
            }
        }

        private void exportToolStripButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            DialogResult dr = fbd.ShowDialog();
            if (pChildForm != null)
            {
                if (fbd.SelectedPath != "")
                {
                    pChildForm = (FileForm)this.ActiveMdiChild;
                    pChildForm.Export(fbd.SelectedPath);
                }
            }
            else
            {
                MessageBox.Show("请先打开需要输出的文件。");
            }

        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            DialogResult dr = fbd.ShowDialog();
            if (pChildForm != null)
            {
                if (fbd.SelectedPath != "")
                {
                    pChildForm = (FileForm)this.ActiveMdiChild;
                    pChildForm.Export(fbd.SelectedPath);
                }
            }
            else
            {
                MessageBox.Show("请先打开需要输出的文件。");
            }
        }
    }
}
