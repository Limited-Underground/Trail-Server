using System.ComponentModel.DataAnnotations;

namespace TrailServer.Api.Configuration;

public sealed class TrailServerOptions
{
    public const string SectionName = "TrailServer";

    [Required]
    [RegularExpression("^[a-z0-9][a-z0-9-]{0,62}$")]
    public string InstanceName { get; init; } = "ts002-development";
}
