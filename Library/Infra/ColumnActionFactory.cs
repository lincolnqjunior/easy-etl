namespace Library.Infra
{
    public static class ColumnActionFactory
    {
        public static IColumnAction CreateAction(IColumnAction config)
        {
            return config.Action switch
            {
                ColumnAction.Ignore or ColumnAction.Parse or ColumnAction.Replace or ColumnAction.Split => throw new NotImplementedException(),
                _ => new DefaultColumnAction(config.Name, config.Position, config.IsHeader, config.OutputName, config.OutputType),
            };
        }
    }
}