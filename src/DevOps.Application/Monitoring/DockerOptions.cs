namespace DevOps.Application.Monitoring;

public sealed class DockerOptions
{
    public const string SectionName = "Docker";

    public bool Enabled { get; set; } = true;
    public string Endpoint { get; set; } = "unix:///var/run/docker.sock";
}
