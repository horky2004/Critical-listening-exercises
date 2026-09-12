using CriticalListeningLab.Api.Domain;
using CriticalListeningLab.Api.Domain.Progression;

namespace CriticalListeningLab.Tests.Progression;

public class UnlockEvaluatorTests
{
    private readonly Guid _boost1 = Guid.NewGuid();
    private readonly Guid _boost2 = Guid.NewGuid();
    private readonly Guid _boost3 = Guid.NewGuid();
    private readonly Guid _boost4 = Guid.NewGuid();
    private readonly Guid _boost5 = Guid.NewGuid();
    private readonly Guid _cut1 = Guid.NewGuid();
    private readonly Guid _cut2 = Guid.NewGuid();
    private readonly Guid _cut3 = Guid.NewGuid();
    private readonly Guid _cut4 = Guid.NewGuid();
    private readonly Guid _combined1 = Guid.NewGuid();
    private readonly Guid _comp1 = Guid.NewGuid();
    private readonly Guid _comp2 = Guid.NewGuid();
    private readonly Guid _comp3 = Guid.NewGuid();

    [Fact]
    public void Level_without_requirements_is_Unlocked_for_a_new_student()
    {
        UnlockEvaluator.Evaluate(EqTree(), Empty)[_boost1].ShouldBe(LevelStatus.Unlocked);
        UnlockEvaluator.Evaluate(CompressionTree(), Empty)[_comp1].ShouldBe(LevelStatus.Unlocked);
    }

    [Fact]
    public void Level_with_requirements_is_Locked_for_a_new_student()
    {
        var statuses = UnlockEvaluator.Evaluate(EqTree(), Empty);

        statuses[_boost2].ShouldBe(LevelStatus.Locked);
        statuses[_cut1].ShouldBe(LevelStatus.Locked);
        statuses[_combined1].ShouldBe(LevelStatus.Locked);
    }

    [Fact]
    public void Boost_L3_pass_unlocks_Cut_L1_and_Boost_L4()
    {
        var progress = Passed(_boost1, _boost2, _boost3);
        var statuses = UnlockEvaluator.Evaluate(EqTree(), progress);

        statuses[_cut1].ShouldBe(LevelStatus.Unlocked);
        statuses[_boost4].ShouldBe(LevelStatus.Unlocked);
        statuses[_boost3].ShouldBe(LevelStatus.Completed);
    }

    [Fact]
    public void Boost_L3_fail_unlocks_neither_Cut_L1_nor_Boost_L4()
    {
        var progress = new Dictionary<Guid, ProgressSnapshot>
        {
            [_boost1] = Pass(_boost1),
            [_boost2] = Pass(_boost2),
            [_boost3] = Fail(_boost3)
        };

        var statuses = UnlockEvaluator.Evaluate(EqTree(), progress);

        statuses[_boost3].ShouldBe(LevelStatus.InProgress);
        statuses[_cut1].ShouldBe(LevelStatus.Locked);
        statuses[_boost4].ShouldBe(LevelStatus.Locked);
    }

    [Fact]
    public void Cut_L3_pass_unlocks_Combined_L1_and_Cut_L4()
    {
        var progress = Passed(_boost1, _boost2, _boost3, _cut1, _cut2, _cut3);
        var statuses = UnlockEvaluator.Evaluate(EqTree(), progress);

        statuses[_combined1].ShouldBe(LevelStatus.Unlocked);
        statuses[_cut4].ShouldBe(LevelStatus.Unlocked);
    }

    [Fact]
    public void Combined_L1_stays_Locked_until_Cut_L3_even_if_all_boost_levels_passed()
    {
        var progress = Passed(_boost1, _boost2, _boost3, _boost4, _boost5);
        var statuses = UnlockEvaluator.Evaluate(EqTree(), progress);

        statuses[_combined1].ShouldBe(LevelStatus.Locked);
        statuses[_cut1].ShouldBe(LevelStatus.Unlocked);
    }

    [Fact]
    public void Chain_is_not_skipped_Boost_L1_does_not_unlock_Boost_L3()
    {
        var statuses = UnlockEvaluator.Evaluate(EqTree(), Passed(_boost1));

        statuses[_boost2].ShouldBe(LevelStatus.Unlocked);
        statuses[_boost3].ShouldBe(LevelStatus.Locked);
    }

    [Fact]
    public void Compression_unlocks_linearly()
    {
        var tree = CompressionTree();

        UnlockEvaluator.Evaluate(tree, Empty)[_comp2].ShouldBe(LevelStatus.Locked);
        UnlockEvaluator.Evaluate(tree, Passed(_comp1))[_comp2].ShouldBe(LevelStatus.Unlocked);
        UnlockEvaluator.Evaluate(tree, Passed(_comp1, _comp2))[_comp3].ShouldBe(LevelStatus.Unlocked);
    }

    private IReadOnlyList<LevelDefinition> EqTree() =>
    [
        Def(_boost1),
        Def(_boost2, _boost1),
        Def(_boost3, _boost2),
        Def(_boost4, _boost3),
        Def(_boost5, _boost4),
        Def(_cut1, _boost3),
        Def(_cut2, _cut1),
        Def(_cut3, _cut2),
        Def(_cut4, _cut3),
        Def(_combined1, _cut3)
    ];

    private IReadOnlyList<LevelDefinition> CompressionTree() =>
    [
        Def(_comp1),
        Def(_comp2, _comp1),
        Def(_comp3, _comp2)
    ];

    private static readonly IReadOnlyDictionary<Guid, ProgressSnapshot> Empty =
        new Dictionary<Guid, ProgressSnapshot>();

    private static LevelDefinition Def(Guid id, params Guid[] required) =>
        new(id, required.Select(r => new UnlockRequirementDefinition(r, UnlockRequirementType.PassedLevel, null)).ToList());

    private static Dictionary<Guid, ProgressSnapshot> Passed(params Guid[] ids) =>
        ids.ToDictionary(id => id, Pass);

    private static ProgressSnapshot Pass(Guid id) => new(id, 12, true, 1, DateTimeOffset.UtcNow);
    private static ProgressSnapshot Fail(Guid id) => new(id, 11, false, 1, null);
}
