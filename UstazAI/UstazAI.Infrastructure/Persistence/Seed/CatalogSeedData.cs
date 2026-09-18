using UstazAI.Domain.Entities;
using UstazAI.Domain.Enums;
using UstazAI.Domain.ValueObjects;

namespace UstazAI.Infrastructure.Persistence.Seed;

public static class CatalogSeedData
{
    private sealed record ProgramSeed(
        string Name, string Field, DegreeLevel Level, string Language,
        decimal TuitionUsd, decimal LivingUsd, int DeadlineDaysFromNow,
        decimal? MinGpa, List<ExamRequirement> Exams,
        bool ScholarshipAvailable, decimal ScholarshipPct, decimal AdmitRatePct, decimal? StartingSalaryUsd,
        List<AdmissionThresholdSeed>? Thresholds = null);

    private sealed record AdmissionThresholdSeed(
        AdmissionExamTrack Track, decimal State, decimal University,
        decimal CutoffMin, decimal CutoffMax, decimal CutoffMedian, int SampleSize);

    private sealed record UniversitySeed(
        string Name, string Country, string City, string? Url, List<ProgramSeed> Programs,
        GeoCoordinates? Coordinates = null, EnvironmentProfile? Environment = null);

    public static (List<University> Universities, List<ProgramOffering> Programs, List<Scholarship> Scholarships, List<AdmitArchetype> Archetypes) Build()
    {
        var provenance = () => DataProvenance.Demo("UstazAI illustrative seed dataset (approximate figures for demo purposes)");
        var seeds = GetUniversitySeeds();

        var universities = new List<University>();
        var programs = new List<ProgramOffering>();
        var scholarships = new List<Scholarship>();
        var archetypes = new List<AdmitArchetype>();

        foreach (var u in seeds)
        {
            var university = new University
            {
                Name = u.Name, Country = u.Country, City = u.City, WebsiteUrl = u.Url, Provenance = provenance(),
                Coordinates = u.Coordinates, Environment = u.Environment
            };
            universities.Add(university);

            foreach (var p in u.Programs)
            {
                var program = new ProgramOffering
                {
                    University = university,
                    Name = p.Name,
                    FieldOfStudy = p.Field,
                    DegreeLevel = p.Level,
                    LanguageOfInstruction = p.Language,
                    TuitionPerYearUsd = p.TuitionUsd,
                    LivingCostPerYearUsd = p.LivingUsd,
                    ApplicationDeadline = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(p.DeadlineDaysFromNow)),
                    MinGpa = p.MinGpa,
                    RequiredExams = p.Exams,
                    ScholarshipAvailable = p.ScholarshipAvailable,
                    ScholarshipCoveragePercent = p.ScholarshipPct,
                    TypicalAdmitRatePercent = p.AdmitRatePct,
                    AverageStartingSalaryUsd = p.StartingSalaryUsd,
                    Provenance = provenance()
                };
                university.Programs.Add(program);
                programs.Add(program);

                if (p.ScholarshipAvailable)
                {
                    var scholarship = new Scholarship
                    {
                        Name = $"{university.Name} Merit Scholarship",
                        CoveragePercent = p.ScholarshipPct,
                        EligibilityCriteria = p.MinGpa is { } g ? $"GPA {g:0.00}+ and required exam scores met" : "Competitive academic record",
                        DeadlineDate = program.ApplicationDeadline.AddDays(-14),
                        Provenance = provenance()
                    };
                    program.Scholarships.Add(scholarship);
                    scholarships.Add(scholarship);
                }

                var programArchetypes = BuildArchetypesFor(p.MinGpa, p.Exams);
                foreach (var a in programArchetypes) program.AdmitArchetypes.Add(a);
                archetypes.AddRange(programArchetypes);

                if (p.Thresholds is { Count: > 0 })
                {
                    foreach (var t in p.Thresholds)
                    {
                        program.AdmissionThresholds.Add(new AdmissionThreshold
                        {
                            Track = t.Track,
                            StateThreshold = t.State,
                            UniversityInternalThreshold = t.University,
                            HistoricalCutoffMin = t.CutoffMin,
                            HistoricalCutoffMax = t.CutoffMax,
                            HistoricalCutoffMedian = t.CutoffMedian,
                            HistoricalCutoffSampleSize = t.SampleSize,
                            Provenance = provenance()
                        });
                    }
                }
            }
        }

        return (universities, programs, scholarships, archetypes);
    }

    private static List<AdmitArchetype> BuildArchetypesFor(decimal? minGpa, List<ExamRequirement> requiredExams)
    {
        var gpaCenter = minGpa ?? 3.0m;
        var examCenter = requiredExams.Count > 0 ? requiredExams[0].MinScore : 6.0m;

        return
        [
            new AdmitArchetype
            {
                ArchetypeLabel = "Strong academic profile, on-budget",
                GpaMin = gpaCenter, GpaMax = 4.0m, ExamScoreMin = examCenter, ExamScoreMax = examCenter * 1.3m,
                BudgetBand = BudgetBand.Medium, Outcome = AdmitOutcome.Admitted, TimelineMonths = 8,
                CommonBlocker = null, Weight = 6
            },
            new AdmitArchetype
            {
                ArchetypeLabel = "Average academic profile, tight budget",
                GpaMin = Math.Max(0, gpaCenter - 0.4m), GpaMax = gpaCenter, ExamScoreMin = examCenter * 0.8m, ExamScoreMax = examCenter,
                BudgetBand = BudgetBand.Low, Outcome = AdmitOutcome.Waitlisted, TimelineMonths = 10,
                CommonBlocker = "Insufficient scholarship coverage relative to budget", Weight = 4
            },
            new AdmitArchetype
            {
                ArchetypeLabel = "Below-threshold exam score",
                GpaMin = Math.Max(0, gpaCenter - 0.6m), GpaMax = gpaCenter, ExamScoreMin = 0, ExamScoreMax = examCenter * 0.75m,
                BudgetBand = BudgetBand.Medium, Outcome = AdmitOutcome.Rejected, TimelineMonths = 6,
                CommonBlocker = "Required exam score not met", Weight = 3
            }
        ];
    }

    private static List<UniversitySeed> GetUniversitySeeds() =>
    [
        new("Nazarbayev University", "Kazakhstan", "Astana", "https://nu.edu.kz",
        [
            new("Computer Science", "Computer Science", DegreeLevel.Bachelor, "English", 0m, 4000m, 150,
                3.2m, [new() { ExamType = ExamType.Ielts, MinScore = 6.0m }], true, 100, 18, 22000,
                [
                    new(AdmissionExamTrack.StandardEnt, 50, 100, 115, 135, 125, 15),
                    new(AdmissionExamTrack.ContinuingSpecialtyGrant, 25, 50, 55, 65, 60, 8)
                ]),
            new("Mechanical Engineering", "Engineering", DegreeLevel.Bachelor, "English", 0m, 4000m, 150,
                3.0m, [new() { ExamType = ExamType.Ielts, MinScore = 6.0m }], true, 100, 22, 19000,
                [
                    new(AdmissionExamTrack.StandardEnt, 50, 95, 105, 125, 115, 12),
                    new(AdmissionExamTrack.ContinuingSpecialtyGrant, 25, 48, 50, 60, 55, 6)
                ])
        ],
        Coordinates: new() { Latitude = 51.0908, Longitude = 71.4139 },
        Environment: new()
        {
            CostOfLivingIndexUsdPerMonth = 550, SafetyIndex = 78, ClimateSummary = "Continental — very cold winters (-20°C), hot dry summers",
            PublicTransitQuality = PublicTransitQuality.Fair, InternationalStudentPercent = 8
        }),
        new("Astana IT University", "Kazakhstan", "Astana", "https://astanait.edu.kz",
        [
            new("Software Engineering", "Computer Science", DegreeLevel.Bachelor, "English", 3200m, 3500m, 120,
                2.8m, [new() { ExamType = ExamType.Ielts, MinScore = 5.5m }], true, 50, 45, 15000,
                [
                    new(AdmissionExamTrack.StandardEnt, 50, 75, 90, 110, 100, 18),
                    new(AdmissionExamTrack.ContinuingSpecialtyGrant, 25, 38, 42, 52, 47, 10)
                ]),
            new("Data Science", "Data Science", DegreeLevel.Bachelor, "English", 3200m, 3500m, 120,
                2.8m, [new() { ExamType = ExamType.Ielts, MinScore = 5.5m }], true, 50, 40, 16000,
                [
                    new(AdmissionExamTrack.StandardEnt, 50, 75, 88, 108, 98, 14),
                    new(AdmissionExamTrack.ContinuingSpecialtyGrant, 25, 38, 40, 50, 45, 8)
                ])
        ],
        Coordinates: new() { Latitude = 51.1284, Longitude = 71.4306 },
        Environment: new()
        {
            CostOfLivingIndexUsdPerMonth = 480, SafetyIndex = 76, ClimateSummary = "Continental — very cold winters (-20°C), hot dry summers",
            PublicTransitQuality = PublicTransitQuality.Fair, InternationalStudentPercent = 5
        }),
        new("KIMEP University", "Kazakhstan", "Almaty", "https://kimep.kz",
        [
            new("Business Administration", "Business", DegreeLevel.Bachelor, "English", 6500m, 4500m, 100,
                2.7m, [new() { ExamType = ExamType.Ielts, MinScore = 5.5m }], true, 30, 55, 14000,
                [
                    new(AdmissionExamTrack.StandardEnt, 50, 65, 75, 95, 85, 20),
                    new(AdmissionExamTrack.ContinuingSpecialtyGrant, 25, 33, 35, 45, 40, 12)
                ]),
            new("International Journalism", "Media", DegreeLevel.Bachelor, "English", 6000m, 4500m, 100,
                2.6m, [new() { ExamType = ExamType.Ielts, MinScore = 5.5m }], true, 25, 60, 11000,
                [
                    new(AdmissionExamTrack.StandardEnt, 50, 60, 70, 90, 80, 10),
                    new(AdmissionExamTrack.ContinuingSpecialtyGrant, 25, 30, 32, 42, 37, 6)
                ])
        ],
        Coordinates: new() { Latitude = 43.2265, Longitude = 76.9286 },
        Environment: new()
        {
            CostOfLivingIndexUsdPerMonth = 520, SafetyIndex = 72, ClimateSummary = "Continental, moderated by mountains — cold winters, warm summers",
            PublicTransitQuality = PublicTransitQuality.Good, InternationalStudentPercent = 12
        }),
        new("Novosibirsk State University", "Russia", "Novosibirsk", "https://nsu.ru",
        [
            new("Applied Mathematics", "Mathematics", DegreeLevel.Bachelor, "Russian", 4000m, 3000m, 140,
                3.0m, [new() { ExamType = ExamType.Ent, MinScore = 70m }], true, 60, 35, 13000)
        ]),
        new("HSE University", "Russia", "Moscow", "https://hse.ru",
        [
            new("Economics", "Economics", DegreeLevel.Bachelor, "English", 7500m, 6000m, 130,
                3.2m, [new() { ExamType = ExamType.Ielts, MinScore = 6.5m }], true, 40, 25, 17000),
            new("Business Informatics", "Computer Science", DegreeLevel.Bachelor, "English", 7000m, 6000m, 130,
                3.1m, [new() { ExamType = ExamType.Ielts, MinScore = 6.0m }], true, 40, 30, 19000)
        ]),
        new("Seoul National University", "South Korea", "Seoul", "https://snu.ac.kr",
        [
            new("Computer Science and Engineering", "Computer Science", DegreeLevel.Bachelor, "English", 8000m, 9000m, 160,
                3.6m, [new() { ExamType = ExamType.Toefl, MinScore = 90m }], true, 70, 12, 35000)
        ],
        Coordinates: new() { Latitude = 37.4601, Longitude = 126.9520 },
        Environment: new()
        {
            CostOfLivingIndexUsdPerMonth = 900, SafetyIndex = 88, ClimateSummary = "Humid continental — cold winters, hot humid summers",
            PublicTransitQuality = PublicTransitQuality.Excellent, InternationalStudentPercent = 10
        }),
        new("KAIST", "South Korea", "Daejeon", "https://kaist.ac.kr",
        [
            new("Electrical Engineering", "Engineering", DegreeLevel.Bachelor, "English", 6000m, 8000m, 170,
                3.7m, [new() { ExamType = ExamType.Toefl, MinScore = 88m }], true, 80, 10, 38000)
        ]),
        new("Yonsei University", "South Korea", "Seoul", "https://yonsei.ac.kr",
        [
            new("International Business", "Business", DegreeLevel.Bachelor, "English", 12000m, 9000m, 145,
                3.4m, [new() { ExamType = ExamType.Toefl, MinScore = 85m }], true, 50, 20, 28000)
        ]),
        new("Bilkent University", "Turkey", "Ankara", "https://bilkent.edu.tr",
        [
            new("Computer Engineering", "Computer Science", DegreeLevel.Bachelor, "English", 15000m, 5000m, 110,
                3.1m, [new() { ExamType = ExamType.Sat, MinScore = 1200m }], true, 100, 30, 20000)
        ]),
        new("Sabancı University", "Turkey", "Istanbul", "https://sabanciuniv.edu",
        [
            new("Industrial Engineering", "Engineering", DegreeLevel.Bachelor, "English", 18000m, 6000m, 115,
                3.2m, [new() { ExamType = ExamType.Sat, MinScore = 1250m }], true, 80, 25, 21000)
        ]),
        new("Warsaw University of Technology", "Poland", "Warsaw", "https://pw.edu.pl",
        [
            new("Computer Science", "Computer Science", DegreeLevel.Bachelor, "English", 4000m, 6000m, 125,
                2.9m, [new() { ExamType = ExamType.Ielts, MinScore = 5.5m }], false, 0, 50, 24000),
            new("Robotics and Automation", "Engineering", DegreeLevel.Bachelor, "English", 4000m, 6000m, 125,
                2.9m, [new() { ExamType = ExamType.Ielts, MinScore = 5.5m }], false, 0, 48, 25000)
        ],
        Coordinates: new() { Latitude = 52.2210, Longitude = 21.0111 },
        Environment: new()
        {
            CostOfLivingIndexUsdPerMonth = 700, SafetyIndex = 80, ClimateSummary = "Temperate — cold winters, warm summers",
            PublicTransitQuality = PublicTransitQuality.Excellent, InternationalStudentPercent = 9
        }),
        new("Jagiellonian University", "Poland", "Kraków", "https://uj.edu.pl",
        [
            new("International Relations", "Social Sciences", DegreeLevel.Bachelor, "English", 3500m, 5500m, 118,
                3.0m, [new() { ExamType = ExamType.Ielts, MinScore = 6.0m }], false, 0, 55, 18000)
        ]),
        new("Charles University", "Czech Republic", "Prague", "https://cuni.cz",
        [
            new("Economics and Finance", "Economics", DegreeLevel.Bachelor, "English", 5000m, 7000m, 135,
                3.1m, [new() { ExamType = ExamType.Ielts, MinScore = 6.0m }], false, 0, 40, 22000)
        ]),
        new("Czech Technical University", "Czech Republic", "Prague", "https://cvut.cz",
        [
            new("Cybernetics and Robotics", "Engineering", DegreeLevel.Bachelor, "English", 5000m, 7000m, 138,
                3.0m, [new() { ExamType = ExamType.Ielts, MinScore = 5.5m }], false, 0, 42, 26000)
        ]),
        new("Technical University of Munich", "Germany", "Munich", "https://tum.de",
        [
            new("Informatics", "Computer Science", DegreeLevel.Bachelor, "German/English", 0m, 12000m, 155,
                3.6m, [new() { ExamType = ExamType.Ielts, MinScore = 6.5m }], true, 100, 15, 55000)
        ],
        Coordinates: new() { Latitude = 48.1497, Longitude = 11.5680 },
        Environment: new()
        {
            CostOfLivingIndexUsdPerMonth = 1300, SafetyIndex = 85, ClimateSummary = "Temperate — cold winters, mild summers",
            PublicTransitQuality = PublicTransitQuality.Excellent, InternationalStudentPercent = 32
        }),
        new("RWTH Aachen University", "Germany", "Aachen", "https://rwth-aachen.de",
        [
            new("Mechanical Engineering", "Engineering", DegreeLevel.Bachelor, "German/English", 0m, 11000m, 150,
                3.5m, [new() { ExamType = ExamType.Ielts, MinScore = 6.5m }], true, 100, 20, 52000)
        ]),
        new("University of Manchester", "United Kingdom", "Manchester", "https://manchester.ac.uk",
        [
            new("Computer Science", "Computer Science", DegreeLevel.Bachelor, "English", 28000m, 14000m, 200,
                3.5m, [new() { ExamType = ExamType.Ielts, MinScore = 6.5m }], true, 25, 25, 45000),
            new("Mechanical Engineering", "Engineering", DegreeLevel.Bachelor, "English", 27000m, 14000m, 200,
                3.4m, [new() { ExamType = ExamType.Ielts, MinScore = 6.5m }], true, 25, 30, 42000)
        ],
        Coordinates: new() { Latitude = 53.4668, Longitude = -2.2339 },
        Environment: new()
        {
            CostOfLivingIndexUsdPerMonth = 1100, SafetyIndex = 74, ClimateSummary = "Temperate oceanic — mild, frequent rain year-round",
            PublicTransitQuality = PublicTransitQuality.Good, InternationalStudentPercent = 38
        }),
        new("Coventry University", "United Kingdom", "Coventry", "https://coventry.ac.uk",
        [
            new("Business Management", "Business", DegreeLevel.Bachelor, "English", 18000m, 12000m, 195,
                2.8m, [new() { ExamType = ExamType.Ielts, MinScore = 6.0m }], true, 30, 60, 28000)
        ]),
        new("Arizona State University", "United States", "Tempe", "https://asu.edu",
        [
            new("Computer Science", "Computer Science", DegreeLevel.Bachelor, "English", 29000m, 15000m, 210,
                3.3m, [new() { ExamType = ExamType.Toefl, MinScore = 80m }, new() { ExamType = ExamType.Sat, MinScore = 1150m }],
                true, 35, 88, 65000)
        ]),
        new("University of Wisconsin–Madison", "United States", "Madison", "https://wisc.edu",
        [
            new("Data Science", "Data Science", DegreeLevel.Bachelor, "English", 39000m, 14000m, 205,
                3.6m, [new() { ExamType = ExamType.Toefl, MinScore = 92m }, new() { ExamType = ExamType.Sat, MinScore = 1350m }],
                true, 20, 49, 72000)
        ]),
        new("Khalifa University", "United Arab Emirates", "Abu Dhabi", "https://ku.ac.ae",
        [
            new("Aerospace Engineering", "Engineering", DegreeLevel.Bachelor, "English", 20000m, 10000m, 128,
                3.4m, [new() { ExamType = ExamType.Ielts, MinScore = 6.0m }], true, 90, 30, 48000)
        ]),
        new("University of Malaya", "Malaysia", "Kuala Lumpur", "https://um.edu.my",
        [
            new("Computer Science", "Computer Science", DegreeLevel.Bachelor, "English", 6000m, 4500m, 122,
                2.9m, [new() { ExamType = ExamType.Ielts, MinScore = 5.5m }], true, 40, 45, 16000)
        ]),
        new("Nanyang Technological University", "Singapore", "Singapore", "https://ntu.edu.sg",
        [
            new("Computer Science", "Computer Science", DegreeLevel.Bachelor, "English", 24000m, 11000m, 165,
                3.7m, [new() { ExamType = ExamType.Sat, MinScore = 1450m }], true, 40, 8, 60000),
            new("Materials Engineering", "Engineering", DegreeLevel.Bachelor, "English", 22000m, 11000m, 165,
                3.5m, [new() { ExamType = ExamType.Sat, MinScore = 1400m }], true, 45, 12, 50000)
        ]),
        new("University of Toronto", "Canada", "Toronto", "https://utoronto.ca",
        [
            new("Computer Science", "Computer Science", DegreeLevel.Bachelor, "English", 45000m, 15000m, 190,
                3.7m, [new() { ExamType = ExamType.Toefl, MinScore = 100m }], true, 20, 43, 58000),
            new("Life Sciences", "Biology", DegreeLevel.Bachelor, "English", 42000m, 15000m, 190,
                3.5m, [new() { ExamType = ExamType.Toefl, MinScore = 93m }], true, 20, 45, 40000)
        ],
        Coordinates: new() { Latitude = 43.6629, Longitude = -79.3957 },
        Environment: new()
        {
            CostOfLivingIndexUsdPerMonth = 1600, SafetyIndex = 82, ClimateSummary = "Humid continental — cold snowy winters, warm humid summers",
            PublicTransitQuality = PublicTransitQuality.Excellent, InternationalStudentPercent = 25
        })
    ];
}
