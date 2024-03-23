﻿using Library.Extractors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Loaders.Csv
{
    public class CsvDataLoaderConfig
    {
        public char Delimiter { get; set; } = ';';
        public string CultureInfo { get; set; } = "pt-BR";
        public bool HasHeader { get; set; } = true;
        public int NotifyAfter { get; set; } = 1000;
        public string OutputPath { get; set; } = string.Empty;
    }
}