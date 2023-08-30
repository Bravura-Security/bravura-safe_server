namespace Bit.Setup;

public class GrafanaDataSourceBuilder
{
    private readonly Context _context;

    public GrafanaDataSourceBuilder(Context context)
    {
        _context = context;
    }

    public void Build()
    {
        var model = new TemplateModel
        {
            Url = _context.Config.Url
        };

        // Needed for backwards compatability with migrated U2F tokens.
        Helpers.WriteLine(_context, "Building Grafana datasources.yml.");
        Directory.CreateDirectory("/bitwarden/appdata/datasources");
        Directory.CreateDirectory("/bitwarden/appdata/grafana");
        var template = Helpers.ReadTemplate("GrafanaDataSources");
        using (var sw = File.CreateText("/bitwarden/appdata/datasources/datasources.yml"))
        {
            sw.Write(template(model));
        }
    }

    public class TemplateModel
    {
        public string Url { get; set; }
    }
}
