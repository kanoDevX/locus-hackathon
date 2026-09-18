using UstazAI.Domain.Enums;
using UstazAI.Domain.ValueObjects;

namespace UstazAI.Domain.Services;

public static class ResourceLinkCatalog
{
    private static readonly (string[] Keywords, List<ResourceLink> Links)[] Entries =
    [
        (["math"], [
            Link("Khan Academy — Mathematics", "https://www.khanacademy.org/math", ResourceType.Course),
            Link("National Testing Center — official ENT prep materials", "https://www.testcenter.kz", ResourceType.PracticeTest)
        ]),
        (["physic"], [
            Link("Khan Academy — Physics", "https://www.khanacademy.org/science/physics", ResourceType.Course),
            Link("National Testing Center — official ENT prep materials", "https://www.testcenter.kz", ResourceType.PracticeTest)
        ]),
        (["chemistry"], [
            Link("Khan Academy — Chemistry", "https://www.khanacademy.org/science/chemistry", ResourceType.Course),
            Link("National Testing Center — official ENT prep materials", "https://www.testcenter.kz", ResourceType.PracticeTest)
        ]),
        (["biology"], [
            Link("Khan Academy — Biology", "https://www.khanacademy.org/science/biology", ResourceType.Course),
            Link("National Testing Center — official ENT prep materials", "https://www.testcenter.kz", ResourceType.PracticeTest)
        ]),
        (["history"], [
            Link("National Testing Center — official ENT prep materials", "https://www.testcenter.kz", ResourceType.PracticeTest)
        ]),
        (["kazakh", "russian", "language"], [
            Link("National Testing Center — official ENT prep materials", "https://www.testcenter.kz", ResourceType.PracticeTest)
        ]),
        (["reading"], [
            Link("Khan Academy — Reading & Vocabulary", "https://www.khanacademy.org/test-prep/sat-reading-and-writing", ResourceType.Course)
        ]),
        (["inform", "computing", "computer"], [
            Link("Khan Academy — Computing", "https://www.khanacademy.org/computing", ResourceType.Course)
        ]),
        (["ielts"], [
            Link("IELTS — official practice materials", "https://www.ielts.org", ResourceType.PracticeTest)
        ]),
        (["toefl"], [
            Link("ETS — official TOEFL practice materials", "https://www.ets.org/toefl.html", ResourceType.PracticeTest)
        ]),
        (["sat"], [
            Link("College Board — official SAT practice", "https://www.collegeboard.org", ResourceType.PracticeTest),
            Link("Khan Academy — SAT prep partnership", "https://www.khanacademy.org/test-prep/sat", ResourceType.Course)
        ]),
        (["duolingo"], [
            Link("Duolingo English Test — official practice", "https://englishtest.duolingo.com", ResourceType.PracticeTest)
        ])
    ];

    private static readonly List<ResourceLink> Fallback =
    [
        Link("National Testing Center — official ENT prep materials", "https://www.testcenter.kz", ResourceType.PracticeTest),
        Link("Khan Academy", "https://www.khanacademy.org", ResourceType.Course)
    ];

    public static List<ResourceLink> ForSubject(string subjectName)
    {
        var needle = subjectName.ToLowerInvariant();
        var match = Entries.FirstOrDefault(e => e.Keywords.Any(k => needle.Contains(k, StringComparison.Ordinal)));
        return match.Links is { Count: > 0 } ? [.. match.Links] : [.. Fallback];
    }

    private static ResourceLink Link(string title, string url, ResourceType type) => new()
    {
        Title = title,
        Url = url,
        ResourceType = type,
        Provenance = DataProvenance.Demo("UstazAI curated resource catalog — illustrative starter set, not an exhaustive vetted curriculum")
    };
}
