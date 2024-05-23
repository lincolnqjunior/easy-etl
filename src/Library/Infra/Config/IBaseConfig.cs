using Library.Infra.ColumnActions;

namespace Library.Infra.Config
{
    #region BASE

    public interface IBaseConfig
    {
        public string CultureInfo { get; set; }
        public int RaiseChangeEventAfer { get; set; }
    }

    public interface IDataBaseConfig : IBaseConfig
    {
        public string ConnectionString { get; set; }
        public string TableName { get; set; }
    }

    public interface ICsvConfig : IBaseConfig
    {
        public char Delimiter { get; set; }
        public bool HasHeader { get; set; }
    }

    public interface IJsonConfig : IBaseConfig
    {
        public bool IndentJson { get; set; }
        public bool IsJsonl { get; set; }
    }

    #endregion

    #region EXTRACTOR

    public interface IExtractorConfig : IBaseConfig
    {
        public List<IColumnAction> Columns { get; set; }
    }

    public interface IFileExtractorConfig : IExtractorConfig
    {
        public string FilePath { get; set; }
    }

    public interface IMultiFileExtractorConfig : IExtractorConfig
    {
        public string Directory { get; set; }
        public string Mask { get; set; }
    }

    public interface IDataBaseExtractorConfig : IDataBaseConfig, IExtractorConfig
    {
        public int PageSize { get; set; }
        public string QuerySelect { get; set; }
        public string QueryCount { get; set; }
    }

    #endregion

    #region TRANSFORMER

    public interface ITransformerConfig : IBaseConfig
    {
        public List<TransformationFilter> Transformations { get; set; }
    }

    #endregion

    #region LOADER

    public interface IDataBaseLoaderConfig : IDataBaseConfig
    {
        public long BatchSize { get; set; }
        public int WriteThreads { get; set; }
    }

    public interface IFileLoaderConfig : IBaseConfig
    {
        public string OutputPath { get; set; }
    }

    #endregion
}
