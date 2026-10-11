using SecretSanta.Api.Data;

namespace SecretSanta.Api.Tests;

public class DrawTests
{
    private static List<Participant> MakeParticipants(int count) =>
        Enumerable.Range(1, count)
            .Select(i => new Participant { Id = Guid.NewGuid(), Name = $"Person {i}", InviteToken = $"token-{i}" })
            .ToList();

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(20)]
    public void Assign_NobodyDrawsThemselves(int count)
    {
        for (var run = 0; run < 100; run++)
        {
            var participants = MakeParticipants(count);

            Draw.Assign(participants);

            Assert.All(participants, p =>
            {
                Assert.NotNull(p.RecipientId);
                Assert.NotEqual(p.Id, p.RecipientId);
            });
        }
    }

    [Theory]
    [InlineData(2)]
    [InlineData(7)]
    public void Assign_EveryoneReceivesExactlyOneGift(int count)
    {
        var participants = MakeParticipants(count);

        Draw.Assign(participants);

        var recipients = participants.Select(p => p.RecipientId!.Value).OrderBy(id => id);
        Assert.Equal(participants.Select(p => p.Id).OrderBy(id => id), recipients);
    }

    [Fact]
    public void Assign_FormsASingleLoop()
    {
        var participants = MakeParticipants(10);

        Draw.Assign(participants);

        var byId = participants.ToDictionary(p => p.Id);
        var current = participants[0];
        var visited = new HashSet<Guid>();
        while (visited.Add(current.Id))
            current = byId[current.RecipientId!.Value];

        Assert.Equal(participants.Count, visited.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Assign_WithFewerThanTwoParticipants_Throws(int count)
    {
        Assert.Throws<InvalidOperationException>(() => Draw.Assign(MakeParticipants(count)));
    }

    [Fact]
    public void NewInviteToken_IsUrlSafeAndUnique()
    {
        var tokens = Enumerable.Range(0, 1000).Select(_ => Draw.NewInviteToken()).ToList();

        Assert.Equal(tokens.Count, tokens.Distinct().Count());
        Assert.All(tokens, t =>
        {
            Assert.Equal(24, t.Length);
            Assert.Matches("^[A-Za-z0-9_-]+$", t);
        });
    }
}
