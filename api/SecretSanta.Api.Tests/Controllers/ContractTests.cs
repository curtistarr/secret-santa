using SecretSanta.Api.Contracts;

namespace SecretSanta.Api.Tests.Controllers;

public class ContractTests
{
    [Test]
    public void AdminResponses_NeverExposeWhoHasWho()
    {
        // The organiser must never be able to see assignments, so nothing the admin page
        // receives may carry recipient information.
        var properties = new[] { typeof(ExchangeDetail), typeof(ExchangeSummary), typeof(ParticipantView) }
            .SelectMany(t => t.GetProperties())
            .Select(p => p.Name);

        Assert.That(properties, Has.None.Contains("Recipient").IgnoreCase);
    }
}
