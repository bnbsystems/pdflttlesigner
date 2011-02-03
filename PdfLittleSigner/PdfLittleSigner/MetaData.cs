using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using iTextSharp.text.xml.xmp;

namespace PdfLttleSigner
{
    public class MetaData
    {
        private Dictionary<string, string> info = new Dictionary<string, string>();

        #region Property

        public Dictionary<string, string> Info
        {
            get { return info; }
            set { info = value; }
        }

        public string Author
        {
            get { return (string)info["Author"]; }
            set { info.Add("Author", value); }
        }

        public string Title
        {
            get { return (string)info["Title"]; }
            set { info.Add("Title", value); }
        }

        public string Subject
        {
            get { return (string)info["Subject"]; }
            set { info.Add("Subject", value); }
        }

        public string Keywords
        {
            get { return (string)info["Keywords"]; }
            set { info.Add("Keywords", value); }
        }

        #endregion Property

        public Dictionary<string, string> GetMetaData()
        {
            return this.info;
        }

        public Hashtable GetMetaDataHashtable()
        {
            return new Hashtable(this.info);
        }

        public byte[] GetStreamedMetaData()
        {
            MemoryStream os = new System.IO.MemoryStream();
            var hashTable = new Hashtable(this.info);
            XmpWriter xmp = new XmpWriter(os, hashTable);
            xmp.Close();
            return os.ToArray();
        }
    }
}