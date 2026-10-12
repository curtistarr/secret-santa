using System.Security.Cryptography;
using SecretSanta.Api.Data;

namespace SecretSanta.Api.Services;

public static class Draw
{
    /// <summary>
    /// Randomly assigns each participant a recipient. Participants are shuffled into a single
    /// loop and each gives to the next person, so nobody ever draws themselves.
    /// </summary>
    public static void Assign(IList<Participant> participants)
    {
        if (participants.Count < 2)
            throw new InvalidOperationException("At least two participants are needed for a draw.");

        var shuffled = participants.ToArray();
        RandomNumberGenerator.Shuffle(shuffled.AsSpan());

        for (var i = 0; i < shuffled.Length; i++)
            shuffled[i].RecipientId = shuffled[(i + 1) % shuffled.Length].Id;
    }

    public static string NewInviteToken() =>
        Base64UrlEncode(RandomNumberGenerator.GetBytes(18));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
