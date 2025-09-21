using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    class FileDlgClass
    {
        public string filebasename;
        string filename = "";

        public FileDlgClass()
        {
            filebasename = "";
            filename = "";
        }

        public string FileDlg()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            //openFileDialog1.InitialDirectory = "c:\\";
            openFileDialog.Filter = "xls files (*.xls)|*.xls|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                filename = openFileDialog.FileName;
                filebasename = System.IO.Path.GetFileName(filename);
            }
            openFileDialog = null;
            
            return filename;
        }
    }
     
}
