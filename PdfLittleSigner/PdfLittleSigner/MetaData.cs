using iText.Kernel.XMP;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace PdfLttleSigner
{
    public class MetaData
    {
        private Dictionary<string, string> _info = new Dictionary<string, string>();

        #region Property

        public Dictionary<string, string> Info
        {
            get { return _info; }
            set { _info = value; }
        }

        public string Author
        {
            get { return (string)_info["Author"]; }
            set { _info.Add("Author", value); }
        }

        public string Title
        {
            get { return (string)_info["Title"]; }
            set { _info.Add("Title", value); }
        }

        public string Subject
        {
            get { return (string)_info["Subject"]; }
            set { _info.Add("Subject", value); }
        }

        public string Keywords
        {
            get { return (string)_info["Keywords"]; }
            set { _info.Add("Keywords", value); }
        }

        #endregion Property

        public Dictionary<string, string> GetMetaData()
        {
            return _info;
        }

        public Hashtable GetMetaDataHashtable()
        {
            return new Hashtable(_info);
        }

        public byte[] GetStreamedMetaData()
        {
            MemoryStream os = new System.IO.MemoryStream();
            var hashTable = new Hashtable(_info);
            XmpWriter xmp = new XmpWriter(os, hashTable);
            xmp.Close();
            return os.ToArray();
        }
    }
}