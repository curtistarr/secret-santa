using SecretSanta.Api.Data;
using SecretSanta.Api.Services;
using SecretSanta.Api.Tests.Support;

namespace SecretSanta.Api.Tests.Services;

public class DrawTests
{
    private static List<Participant> MakeParticipants(int count) =>
        TestData.Participants(Guid.NewGuid(), Enumerable.Range(1, count).Select(i => $"Person {i}").ToArray());

    [TestCase(2)]
    [TestCase(3)]
    [TestCase(5)]
    [TestCase(20)]
    public void Assign_NobodyDrawsThemselves(int count)
    {
        for (var run = 0; run < 100; run++)
        {
            var participants = MakeParticipants(count);

            Draw.Assign(participants);

            Assert.That(participants, Has.All.Matches<Participant>(p => p.RecipientId != null && p.RecipientId != p.Id));
        }
    }

    [TestCase(2)]
    [TestCase(7)]
    public void Assign_EveryoneReceivesExactlyOneGift(int count)
    {
        var participants = MakeParticipants(count);

        Draw.Assign(participants);

        Assert.That(participants.Select(p => p.RecipientId!.Value), Is.EquivalentTo(participants.Select(p => p.Id)));
    }

    [Test]
    public void Assign_FormsASingleLoop()
    {
        var participants = MakeParticipants(10);

        Draw.Assign(participants);

        var byId = participants.ToDictionary(p => p.Id);
        var current = participants[0];
        var visited = new HashSet<Guid>();
        while (visited.Add(current.Id))
            current = byId[current.RecipientId!.Value];

        Assert.That(visited, Has.Count.EqualTo(participants.Count));
    }

    [TestCase(0)]
    [TestCase(1)]
    public void Assign_WithFewerThanTwoParticipants_Throws(int count)
    {
        Assert.Throws<InvalidOperationException>(() => Draw.Assign(MakeParticipants(count)));
    }

    [Test]
    public void NewInviteToken_IsUrlSafeAndUnique()
    {
        var tokens = Enumerable.Range(0, 1000).Select(_ => Draw.NewInviteToken()).ToList();

        Assert.That(tokens, Is.Unique);
        Assert.That(tokens, Has.All.Length.EqualTo(24));
        Assert.That(tokens, Has.All.Matches("^[A-Za-z0-9_-]+$"));
    }
}
