namespace Library.Infra.ColumnActions
{
    public static class ColumnActionFactory
    {
        public static IColumnAction CreateAction(IColumnAction config)
        {
            return config.Action switch
            {
                ColumnAction.Ignore or ColumnAction.Replace or ColumnAction.Split => throw new NotImplementedException(),
                ColumnAction.Parse => new ParseColumnAction(config.Name, config.Position, config.IsHeader, config.OutputName, config.OutputType),
                _ => new DefaultColumnAction(config.Name, config.Position, config.IsHeader, config.OutputName, config.OutputType),
            };
        }
    }
}