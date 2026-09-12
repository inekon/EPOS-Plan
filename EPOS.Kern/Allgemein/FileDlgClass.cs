using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace WindowsFormsApplication1
{
    class FileDlgClass
    {
        public string filebasename;
        public string filename = "";
        public string default_folder = "";

        public FileDlgClass()
        {
            filebasename = "";
            filename = "";
            default_folder = "";
        }

        /// <summary>
        /// Zeigt die Dateiwahl und liefert den gewaehlten Pfad; <c>""</c>, wenn der
        /// Anwender abbricht.
        ///
        /// <para>Startordner ist wie bisher <c>LocalApplicationData\WP-Plan</c> plus
        /// <see cref="default_folder"/> — seit iU5 ueber <c>Dienste.Pfade</c> statt ueber
        /// <c>Program.ApplicationPath_User</c>; leere Bestandteile werden dabei
        /// uebergangen, genau wie <c>Path.Combine</c> es tat.</para>
        /// </summary>
        public string Show()
        {
            string path = Dienste.Pfade.Verbinde(Dienste.Pfade.BenutzerLokal, default_folder);
            string gewaehlt = Dienste.Datei.DateiOeffnen(null, "xls files (*.xls)|*.xls", path);

            if (!string.IsNullOrEmpty(gewaehlt))
            {
                filename = gewaehlt;
                filebasename = Path.GetFileName(filename);
                filebasename = Path.GetFileNameWithoutExtension(filebasename);
            }

            return filename;
        }
    }
     
}
