﻿using Library.Readers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Writers
{
    public class FileWriterConfig
    {
        public FileTypes FileType { get; set; } = FileTypes.Csv;
        public char Delimiter { get; set; } = ';';
        public string CultureInfo { get; set; } = "pt-BR";
        public bool HasHeader { get; set; } = true;
        public int NotifyAfter { get; set; } = 1000;
    }
}