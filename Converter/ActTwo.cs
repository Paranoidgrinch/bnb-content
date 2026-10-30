using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;

namespace BnbContent.Converter;

// Act II — The Endless Archives: the shared vocabulary, and the Stage-1 identities that introduce it.
//
// The archive's pressure is source-bound. Overdue is not one debt the player owes the room, it is a separate
// debt owed to each enemy that filed it, and each of them collects its own. That is why Overdue is applied one
// stack at a time as its OWN instance: two instances from the Brass Maw are the Maw's two, and the Ouroboros
// standing beside it cannot spend them.
//
// Each source's collection is its **Delinquency**: when it is owed 2, it takes them back, files a Paperwork,
// and does whatever that particular source does about being kept waiting.
public static class ActTwo
{
    // ── the vocabulary ────────────────────────────────────────────────────────────────────────────────────

    public const string OverdueId = "overdue";

    // One stack per filing, each its own instance carrying its own source. Merging would collapse the whole
    // point: the threshold is "2 from the same source", and merged stacks remember only the last source.
    public static StatusData Overdue() => new()
    {
        Id = OverdueId,
        NameKey = "Overdue",
        DescriptionKey = "A debt owed to whoever filed it. At 2 from one source, that source collects.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.CreateSeparateInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // A source's collection, checked at its OWN turn start — which is what makes "this source's" unambiguous:
    // the bearer is the acting source, so the stacks it spends are provably its own and never a neighbour's.
    // (The design collects the instant the second Overdue lands; here it collects on the next turn of the one
    // owed. See ADAPTATIONS.)
    public static StatusData Delinquency(
        string id, string name, string description,
        IEffectNode<TurnStartedTriggeredEffectContext>? lateConsequence = null)
    {
        var steps = new List<IEffectNode<TurnStartedTriggeredEffectContext>>
        {
            // Two instances of one stack each, so two picks — a single −2 would empty one filing and leave
            // the other standing.
            SpendOne(), SpendOne(),
            new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                Opponent, new StatusDefinitionId(Keywords.Paperwork),
                new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
            // A collection is the archive's own moment, and this rule runs on the body that collects — so it
            // announces for itself, under both names: which moment it was, and that a body's rule fired.
            AnnounceSelf<TurnStartedTriggeredEffectContext>(AnnounceDelinquency),
            AnnounceSelf<TurnStartedTriggeredEffectContext>(AnnounceSignature),
        };
        if (lateConsequence is not null)
            steps.Add(lateConsequence);

        var program = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    new CombatantStatusStacksFromSourceExpression<TurnStartedTriggeredEffectContext>(
                        Opponent, new StatusDefinitionId(OverdueId), CombatantTargetSelectors.Source),
                    ComparisonOperator.GreaterOrEqual,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(2)),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(steps)));

        return Rule(id, name, description,
            [new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                program, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()))]);
    }

    private static IEffectNode<TurnStartedTriggeredEffectContext> SpendOne() =>
        new ModifySelectedStatusStacksNode<TurnStartedTriggeredEffectContext>(
            Opponent,
            new StatusSelectionSpec(StatusPolarityFilter.Debuff)
            {
                Definition = new StatusDefinitionId(OverdueId),
                FromActingSource = true,
            },
            new ConstantExpression<TurnStartedTriggeredEffectContext>(-1));

    // ── what the archive says out loud ───────────────────────────────────────────────────────────────────
    //
    // Three of Act II's moments are CONCLUSIONS another rule reached rather than anything the engine did — a
    // Delinquency collecting, a Reference being fulfilled, a Misfiled card actually being skipped — and two
    // bodies are written entirely about them: the Miscellany Index counts them, and the Detached Footnote
    // watches its Source reach one. Neither could be built, because none of those moments announced itself.
    // The engine now lets a rule say so (`node.announceRule`), and these are the names it says.
    //
    // ⚠ TWO VOCABULARIES, ON PURPOSE. `Delinquency`/`Reference`/`Misfiling` name WHICH moment it was, which
    // is what an Index counting four different things needs. `Signature` names only that a body's own rule
    // reached its moment, which is what the Footnote needs — it cares whose rule fired, not which. A
    // per-body rule therefore announces BOTH; a moment with no single owner (a misfiling skip, where the mark
    // cannot say which shelf filed it) announces only its own name, and the Footnote is deaf to it.
    public const string AnnounceDelinquency = "delinquency";
    public const string AnnounceReference = "reference";
    public const string AnnounceMisfiling = "misfiling";
    public const string AnnounceSignature = "signature";

    // A rule that lives on the PLAYER — as most of Act II's do, because it is the player's hand they are
    // about — still belongs to a BODY, and an announcement has to come from that body or every listener on
    // the enemy side is on the wrong side of it. The body is found by a marker it wears.
    private static IEffectNode<TContext> AnnounceFrom<TContext>(string bodyMarker, string rule)
        where TContext : class =>
        new AnnounceRuleNode<TContext>(
            CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(bodyMarker)), rule);

    private static IEffectNode<TContext> AnnounceSelf<TContext>(string rule) where TContext : class =>
        new AnnounceRuleNode<TContext>(CombatantTargetSelectors.Source, rule);

    // ── Misfiled ──────────────────────────────────────────────────────────────────────────────────────────
    //
    // "A Misfiled card does not enter the hand on its next draw; it goes to discard, you draw a replacement,
    // and the mark clears." The engine hands the cards over BEFORE anything can object — CardsDrawn fires with
    // the hand already holding them — so the archive takes its card back a beat after it arrives. Invisible in
    // the numbers, visible in a combat log; the same beat-late shape Act I's Exception Imp uses.
    //
    // Which shelf a card was misfiled BY changes where it goes, so the destination is written into the mark
    // rather than looked up from the marker's source: a program cannot ask who put a mark there, and "the
    // Crabwise Shelf's misfilings go to the bottom of the draw pile" has to be answerable at the moment the
    // card is taken back. One rule owns that moment, exactly as one place owns the Paperwork tick.
    public const string MisfiledMark = "misfiled";
    public const string MisfiledSidewaysMark = "misfiled_sideways";
    public const string ArchiveRegulationsId = "archive_regulations";

    // The hero carries this in every fight where something can misfile. Idempotent by construction: two
    // misfiling enemies ask for the same marker and it merges, so the card is taken back once.
    public static StatusData ArchiveRegulations()
    {
        var program = new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                TakeBack(MisfiledMark, CardZone.DiscardPile),
                TakeBack(MisfiledSidewaysMark, CardZone.DrawPile),
            ]));

        return Rule(ArchiveRegulationsId, "Archive Regulations",
            "A misfiled card is taken back as it reaches you, and something else is fetched in its place.",
            [new StatusTriggerData("CardsDrawn", JsonSerializer.SerializeToElement(
                program, CombatJson.CreateOptions<CardsDrawnTriggeredEffectContext>()))]);
    }

    // Take back every card in hand carrying one kind of misfiling, and fetch a replacement for each. The mark
    // is cleared BEFORE the replacement is drawn, so a replacement that is itself misfiled waits for the next
    // draw rather than being swept up by the pass that fetched it.
    //
    // The replacement draw names its own card (`Replacement`), because one elite cares WHICH card came back:
    // the Rolling Stacks Colossus grants it Open Aisle. Nothing else reads the key, and the skip itself is the
    // only moment the Colossus's Compression can be counted from — so the hook lives here, in the one place
    // that knows a misfiling was actually skipped, and does nothing when no Colossus is on the field.
    private static readonly EffectResultKey<OrderedTargetOutcomes<DrawCardsOutcome>> Replacement =
        new("misfiled_replacement");

    private static IEffectNode<CardsDrawnTriggeredEffectContext> TakeBack(string mark, CardZone destination)
    {
        var steps = new List<IEffectNode<CardsDrawnTriggeredEffectContext>>
        {
            new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                new TagId(mark), remove: true),
            new MoveCardToZoneNode<CardsDrawnTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                destination),
            new DrawCardsNode<CardsDrawnTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new ConstantExpression<CardsDrawnTriggeredEffectContext>(1),
                resultKey: Replacement),
            Elites.RollingStacksColossus.OnMisfilingSkipped(
                new DrawCardOutcomeExpression<CardsDrawnTriggeredEffectContext>(Replacement)),
            // ⚠ FROM THE WHOLE ENEMY SIDE, because the mark cannot say which shelf filed it (that is why
            // destinations are written into marks at all). The Index only asks THAT a misfiling was skipped;
            // nothing here claims to know whose it was, which is also why this announcement is not a
            // `signature` and the Footnote does not hear it.
            new AnnounceRuleNode<CardsDrawnTriggeredEffectContext>(
                CombatantTargetSelectors.AllEnemiesOfSource, AnnounceMisfiling),
        };
        return new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
            CombatantTargetSelectors.Source, CardZone.Hand,
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(steps),
            markFilter: new TagId(mark));
    }

    // ── Stage 2 — The Misfiled Stacks ─────────────────────────────────────────────────────────────────────

    public const string WrongEditionMark = "wrong_edition";
    public const string WrongEditionId = "wrong_edition_rule";

    // "Misfile 1 card": the mark goes on the draw pile, which is where a misfiling can still cost the player
    // something. The pile is already shuffled, so its top card IS the random one — the reading Act I's
    // Unclaimed Property Tag uses.
    public static IEffectNode<EnemyActionContext> MisfileOne(string mark) =>
        new ForEachCardInZoneNode<EnemyActionContext>(
            Opponent, CardZone.DrawPile,
            new MarkCardInstanceNode<EnemyActionContext>(
                Opponent, new IteratedCardExpression<EnemyActionContext>(), new TagId(mark)),
            takeFirst: 1);

    // "After normal draw, select one valid card in hand. If the player plays it this turn, it resolves normally
    // and THEN becomes Misfiled." The rule lives on the player because that is whose hand and whose play it is
    // about; it is only there when the Corridor is (see EncounterPassives.HeroOpeningStatuses).
    public static StatusData WrongEdition()
    {
        var mark = new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
                CombatantTargetSelectors.Source, CardZone.Hand,
                new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                    CombatantTargetSelectors.Source,
                    new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                    new TagId(WrongEditionMark)),
                takeFirst: 1));

        // Played, so it resolved — and only now is it the wrong edition.
        var convert = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new CardInstanceHasMarkExpression<CardPlayedTriggeredEffectContext>(
                    new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                    new TagId(WrongEditionMark)),
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source,
                        new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                        new TagId(WrongEditionMark), remove: true),
                    new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source,
                        new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                        new TagId(MisfiledMark)),
                ])));

        return Rule(WrongEditionId, "Wrong Edition",
            "One card in your hand is from the wrong edition; playing it works, and then it is filed away.",
            [
                new StatusTriggerData("CardsDrawn", JsonSerializer.SerializeToElement(
                    mark, CombatJson.CreateOptions<CardsDrawnTriggeredEffectContext>())),
                new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                    convert, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>())),
            ]);
    }

    // ── Referenced ────────────────────────────────────────────────────────────────────────────────────────
    //
    // "A source-bound mark on a card. Play it and the reference is fulfilled; let it leave your hand unplayed
    // and the reference clears and costs you 1 Overdue from its source."
    //
    // The whole rule lives on the CITING ENEMY rather than on the player, for one reason: the Overdue an
    // unfulfilled reference costs has to come FROM that enemy, and a rule running on the player would file it
    // from the player. The enemy checking at its own turn start is also the moment that knows the answer — the
    // player's hand has just been put down, so anything still carrying the mark was not played.
    //
    // Each citer marks with its own tag, because a program cannot ask who put a mark on a card.
    //
    // `onFulfilled` / `onFailed` let a citer hang consequences on the two outcomes — the Volumes' Concordance
    // is exactly that: fulfilling a Causes reference costs Causes and strengthens Consequences, failing it
    // does the ordinary Act-II thing AND weakens the linked attack. In the fulfilment hook the acting source
    // is the PLAYER (it is their play); in the failure hook it is the citing enemy, at its own turn start.
    public static StatusData Reference(string id, string name, string mark, string description,
        IEffectNode<CardsDrawnTriggeredEffectContext>? cite = null,
        IEffectNode<CardPlayedTriggeredEffectContext>? onFulfilled = null,
        IEffectNode<TurnStartedTriggeredEffectContext>? onFailed = null)
    {
        // Cite one card in the player's hand after the player's draw. `cite` overrides which one.
        var citing = new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                IsTheApplicant<CardsDrawnTriggeredEffectContext>(),
                cite ?? CiteFirst(mark)));

        // Played, so fulfilled — the mark simply goes, and the citer says so.
        //
        // ⚠ THE ANNOUNCEMENT COMES FROM THE CITER, NOT FROM THE PLAYER. In this hook the acting Source is the
        // player (it is their play), and an announcement from the player would be heard by the player's own
        // allies — which on this side of the fight is nobody who cares. The citing body is found by the
        // citation it wears, the same handle `onFulfilled` uses to reach it.
        var fulfil = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new CardInstanceHasMarkExpression<CardPlayedTriggeredEffectContext>(
                    new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                    new TagId(mark)),
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    Unmark<CardPlayedTriggeredEffectContext>(
                        new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(), mark),
                    AnnounceFrom<CardPlayedTriggeredEffectContext>(id, AnnounceReference),
                    AnnounceFrom<CardPlayedTriggeredEffectContext>(id, AnnounceSignature),
                    .. onFulfilled is null ? [] : new[] { onFulfilled },
                ])));

        // My turn, and the player's hand is down: anything still marked was never played.
        var collect = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [Unfulfilled(mark, CardZone.DiscardPile, onFailed), Unfulfilled(mark, CardZone.Hand, onFailed)]));

        return Rule(id, name, description,
        [
            new StatusTriggerData("CardsDrawn", JsonSerializer.SerializeToElement(
                citing, CombatJson.CreateOptions<CardsDrawnTriggeredEffectContext>()),
                StatusTriggerScope.Anywhere),
            new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                fulfil, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()),
                StatusTriggerScope.Anywhere),
            new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                collect, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>())),
        ]);
    }

    private static IEffectNode<CardsDrawnTriggeredEffectContext> CiteFirst(string mark) =>
        new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
            CombatantTargetSelectors.Source, CardZone.Hand,
            new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(), new TagId(mark)),
            takeFirst: 1);

    private static IEffectNode<TurnStartedTriggeredEffectContext> Unfulfilled(
        string mark, CardZone zone, IEffectNode<TurnStartedTriggeredEffectContext>? onFailed = null) =>
        new ForEachCardInZoneNode<TurnStartedTriggeredEffectContext>(
            Opponent, zone,
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                new MarkCardInstanceNode<TurnStartedTriggeredEffectContext>(
                    Opponent, new IteratedCardExpression<TurnStartedTriggeredEffectContext>(),
                    new TagId(mark), remove: true),
                // Filed by ME — the bearer is the acting source, which is the whole reason this rule is here
                // and not on the player.
                new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                    Opponent, new StatusDefinitionId(OverdueId),
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                .. onFailed is null ? [] : new[] { onFailed },
            ]),
            markFilter: new TagId(mark));

    private static IEffectNode<TContext> Unmark<TContext>(ICardInstanceExpression<TContext> card, string mark)
        where TContext : class =>
        new MarkCardInstanceNode<TContext>(
            CombatantTargetSelectors.Source, card, new TagId(mark), remove: true);

    // "It was the player who drew / played", from inside a rule that watches the whole fight. Every fight
    // marks the hero as the applicant (Act I), which is the only structural handle on "the player".
    private static ICombatExpression<TContext, bool> IsTheApplicant<TContext>() where TContext : class =>
        new TargetHasStatusExpression<TContext>(
            CombatantTargetSelectors.Source, new StatusDefinitionId(PassiveStatuses.ApplicantId));

    // ── Stage 1 — The Hall of Returns ─────────────────────────────────────────────────────────────────────

    public const string BrassMawDelinquencyId = "brass_maw_delinquency";
    public const string ReturnParcelId = "return_parcel";
    public const string OuroborosDelinquencyId = "ouroboros_delinquency";

    public static IReadOnlyList<StatusData> All() =>
    [
        Overdue(),
        ReturnParcel(),
        BrassMawDelinquency(),
        OuroborosDelinquency(),
        ArchiveRegulations(),
        WrongEdition(),
        SecondPersonReference(),
        LearnedLetter(),
        FangedAlphabetReference(),
        OrphanCitationReference(),
        BlankCertificateReference(),
        CertificationRequired(),
        BehindTheDesk(),
        StoredLife(),
        SpareLifeOuroboros(),
        SpareLifeTestator(),
        CertificateCertified(),
        CertificateClause(),
        RememberedVolume(),
        ChainCollect(),
    ];

    public const string CertificateReferenceMark = "referenced_certificate";
    public const string CertificateReferenceId = "blank_certificate_reference";

    // The Certificate cites you and then asks, when it dies, whether you answered.
    //
    // Answering it takes the death clause OFF for the rest of the turn — which is what "fulfilled a Reference
    // from this enemy during the same turn" means. The hook runs on the PLAYER's play, so the Certificate is
    // reached from the other side: the one enemy wearing the citation is the one whose clause this was.
    public static StatusData BlankCertificateReference() =>
        Reference(CertificateReferenceId, "Serve Certificate", CertificateReferenceMark,
            "A card is cited. Answer it, or owe the Certificate for it.",
            onFulfilled: new RemoveStatusNode<CardPlayedTriggeredEffectContext>(
                CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(
                    new StatusDefinitionId(CertificateReferenceId)),
                new StatusDefinitionId(CertificationRequiredId)));

    // ── Stage 9 — the Blank Death Certificate's death clause ──────────────────────────────────────────────
    //
    // "On the first lethal damage: if the player fulfilled a Reference from this enemy during the same turn,
    // death is certified and final. Otherwise the Certificate returns once at roughly 35% HP. After this
    // return it cannot return again."
    //
    // ⚠⚠ THIS STANDS BEFORE THE DEATH, IT DOES NOT UNDO ONE. The obvious build — a Downed trigger that heals
    // the body back up — cannot work, and the reason is worth keeping: the combat's outcome is decided by
    // `UpdateStandardCombatResultOnLifecycleChangedHandler`, which enqueues Victory the moment no enemy is
    // living and never re-checks when that request resolves. The Certificate's ONLY encounter is a solo, so a
    // revive-after-down would have been unreachable in the one fight it exists in. The engine's data-authored
    // `StatusDeathPreventionData` is a PRE-down interceptor: the body never stops living, so no Victory is
    // ever enqueued. The Obituary with Three Endings — also a solo — proves the shape.
    //
    // A prevention interceptor cannot ask a question; it fires whenever it is there. So the CONDITION is
    // whether the clause is on the Certificate at all, exactly as the Obituary expresses its two lives:
    //   · it starts there (EncounterPassives),
    //   · answering the citation takes it off for that turn (BlankCertificateReference's onFulfilled),
    //   · and the Certificate puts it back at its own turn start — so an answer certifies THIS turn's death
    //     and not every later one.
    // "Cannot return again" is the interceptor's own doing: a one-shot prevention is spent when it fires, and
    // what it applies on the way out is the flag that stops the turn-start rule re-arming it.
    public const string CertificationRequiredId = "certification_required";
    public const string CertificateCertifiedId = "death_certified";
    public const string CertificateClauseId = "certification_clause";

    // 35 of the Certificate's 100 — "roughly 35% HP" at the one statline it is ever fielded with.
    private const int CertificateReturnHealth = 35;

    public static StatusData CertificationRequired() => new()
    {
        Id = CertificationRequiredId,
        NameKey = "Certification Required",
        DescriptionKey = "This death is not certified. Unanswered, it will not take.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
        DeathPrevention = new StatusDeathPreventionData(CertificateReturnHealth,
        [
            new InterceptorEffectData(nameof(EffectKind.ApplyStatus), nameof(EffectTarget.Self), 1,
                CertificateCertifiedId, 0, StatusPolarity.Neutral),
        ]),
    };

    // The receipt for a return already spent. Inert — it exists to be read by the clause's turn-start rule.
    public static StatusData CertificateCertified() =>
        Rule(CertificateCertifiedId, "Death Certified",
            "The return has been used. The next one is final.", []);

    // The Certificate re-arms its own clause at its own turn start, unless the return is already spent. Its
    // turn start is the right moment because it is the first one AFTER the player's: an answer given during
    // the player's turn stands for that turn, and the clause is back for the next one.
    public static StatusData CertificateClause() =>
        Rule(CertificateClauseId, "Blank Until Signed",
            "A fresh certificate is drawn up each round, until one of them is signed.",
            [
                Watch("TurnStarted", new EffectProgram<TurnStartedTriggeredEffectContext>(
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        new NotExpression<TurnStartedTriggeredEffectContext>(
                            new TargetHasStatusExpression<TurnStartedTriggeredEffectContext>(
                                CombatantTargetSelectors.Source,
                                new StatusDefinitionId(CertificateCertifiedId))),
                        new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source,
                            new StatusDefinitionId(CertificationRequiredId),
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(1))))),
            ]);

    // "Whenever Brass Maw resolves its Delinquency: gain 1 Return Parcel, maximum 2." The parcels ride on the
    // Maw's next direct attack and are spent by it.
    private static StatusData BrassMawDelinquency() =>
        Delinquency(BrassMawDelinquencyId, "Return Intake",
            "What it is owed comes back as a parcel.",
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(ReturnParcelId)),
                    ComparisonOperator.Less,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(2)),
                new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(ReturnParcelId),
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(1))));

    // "+5 damage per Return Parcel on the next direct attack; after that attack, Return Parcel → 0." The same
    // shape Act I's Contempt uses: the modifier is the parcel's presence, the trigger is what spends it.
    private static StatusData ReturnParcel()
    {
        var spend = new EffectProgram<DamageDealtTriggeredEffectContext>(
            new ModifyStatusStacksNode<DamageDealtTriggeredEffectContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(ReturnParcelId),
                new SubtractExpression<DamageDealtTriggeredEffectContext>(
                    new ConstantExpression<DamageDealtTriggeredEffectContext>(0),
                    new CombatantStatusStacksExpression<DamageDealtTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(ReturnParcelId)))));

        return new StatusData
        {
            Id = ReturnParcelId,
            NameKey = "Return Parcel",
            DescriptionKey = "Returned lateness, waiting to be spat back. Spent by the next direct attack.",
            Polarity = StatusPolarity.Buff,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = true,
            Tags = [],
            PassiveModifiers =
            [
                new PassiveModifierData(PassiveModifierPipeline.DamageDealt,
                    PassiveModifierOperation.AddPerStack, 5, RestrictDamageKind: DamageKind.Direct),
            ],
            Triggers =
            [
                new StatusTriggerData("DamageDealt", JsonSerializer.SerializeToElement(
                    spend, CombatJson.CreateOptions<DamageDealtTriggeredEffectContext>())),
            ],
        };
    }

    // "Whenever this enemy's Delinquency fully resolves: immediately apply 1 new Overdue from this same
    // source." 2 Overdue → collected → 0 → 1 again, so the loop never quite closes.
    private static StatusData OuroborosDelinquency() =>
        Delinquency(OuroborosDelinquencyId, "Return to Sender",
            "Nothing it is owed ever finishes being owed.",
            new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                Opponent, new StatusDefinitionId(OverdueId),
                new ConstantExpression<TurnStartedTriggeredEffectContext>(1)));

    // ── Stage 3 — The Whispering Catalogue ────────────────────────────────────────────────────────────────

    public const string EntryReferenceMark = "referenced_entry";
    public const string AlphabetReferenceMark = "referenced_alphabet";
    public const string CitationReferenceMark = "referenced_citation";
    public const string EntryReferenceId = "second_person_reference";
    public const string AlphabetReferenceId = "fanged_alphabet_reference";
    public const string CitationReferenceId = "orphan_citation_reference";
    public const string AlphabetMemoryId = "learned_letter";

    private static CounterId LastCostCounter => new("alphabet_last_cost");
    private static CounterId LearnedCostCounter => new("alphabet_learned_cost");
    private static CounterId CitedThisDrawCounter => new("alphabet_cited");

    // "Second-Person Entry cites you, again and again." The design chains the follow-up citation to the TYPE
    // of the card that fulfilled the last one; here every draw is simply cited afresh. See ADAPTATIONS.
    public static StatusData SecondPersonReference() =>
        Reference(EntryReferenceId, "You Are Cited Again", EntryReferenceMark,
            "One card in your hand is cited. Play it, or owe the Entry for it.");

    // "If the player plays two consecutive cards with the same Base Cost, remember that cost class; after the
    // next draw, cite a card of that cost." Two counters: what the last card cost, and what was learned.
    public static StatusData LearnedLetter()
    {
        var watch = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                IsTheApplicant<CardPlayedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    // Two in a row at the same price is a class worth learning.
                    new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                        new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                            PlayedCost<CardPlayedTriggeredEffectContext>(),
                            ComparisonOperator.Equal,
                            OnWearer<CardPlayedTriggeredEffectContext>(LastCostCounter)),
                        SetOnWearer<CardPlayedTriggeredEffectContext>(
                            LearnedCostCounter, PlayedCost<CardPlayedTriggeredEffectContext>())),
                    SetOnWearer<CardPlayedTriggeredEffectContext>(
                        LastCostCounter, PlayedCost<CardPlayedTriggeredEffectContext>()),
                ])));

        return Rule(AlphabetMemoryId, "Learned Letter",
            "The alphabet listens for a price you pay twice in a row.",
            [new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                watch, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()),
                StatusTriggerScope.Anywhere)]);
    }

    // …and cites a card of the learned price, one per draw.
    public static StatusData FangedAlphabetReference() =>
        Reference(AlphabetReferenceId, "Bitten Letter", AlphabetReferenceMark,
            "A card of the price the alphabet learned is cited.",
            cite: new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                    OnlyWearer, CitedThisDrawCounter,
                    new ConstantExpression<CardsDrawnTriggeredEffectContext>(0), relative: false),
                new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, CardZone.Hand,
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        new AndExpression<CardsDrawnTriggeredEffectContext>(
                            new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                                IteratedCost<CardsDrawnTriggeredEffectContext>(),
                                ComparisonOperator.Equal,
                                new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(
                                    OnlyWearer, LearnedCostCounter)),
                            new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                                new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(
                                    OnlyWearer, CitedThisDrawCounter),
                                ComparisonOperator.Equal,
                                new ConstantExpression<CardsDrawnTriggeredEffectContext>(0))),
                        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                        [
                            new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                                CombatantTargetSelectors.Source,
                                new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                                new TagId(AlphabetReferenceMark)),
                            new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                                OnlyWearer, CitedThisDrawCounter,
                                new ConstantExpression<CardsDrawnTriggeredEffectContext>(1), relative: false),
                        ]))),
            ]));

    // "The player may fulfil the citation by playing the exact card, OR another card of the same Base Cost and
    // the same type." The second path is answered where the citation is: compare what was played against the
    // card still carrying the mark.
    public static StatusData OrphanCitationReference()
    {
        var citation = Reference(CitationReferenceId, "Reconstruct the Source", CitationReferenceMark,
            "A card is cited; something enough like it will also do.");

        // Same price and same kind counts as the same citation.
        var standIn = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new AndExpression<CardPlayedTriggeredEffectContext>(
                    IsTheApplicant<CardPlayedTriggeredEffectContext>(),
                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        PlayedCost<CardPlayedTriggeredEffectContext>(),
                        ComparisonOperator.Equal,
                        new CardInstanceBaseCostExpression<CardPlayedTriggeredEffectContext>(
                            new FirstMarkedCardInOwnerZoneExpression<CardPlayedTriggeredEffectContext>(
                                CombatantTargetSelectors.Source, CardZone.Hand,
                                new TagId(CitationReferenceMark)),
                            StandardCombatIds.EnergyResource))),
                new ForEachCardInZoneNode<CardPlayedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, CardZone.Hand,
                    new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source,
                        new IteratedCardExpression<CardPlayedTriggeredEffectContext>(),
                        new TagId(CitationReferenceMark), remove: true),
                    markFilter: new TagId(CitationReferenceMark))));

        return citation with
        {
            Triggers =
            [
                .. citation.Triggers,
                new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                    standIn, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
            ],
        };
    }

    // What the alphabet learned is kept on the PLAYER, not on the alphabet. Both moments it cares about — a
    // card played, a hand drawn — are the player's, and in a fight-wide trigger the player is the one
    // structural single target available ("who did this"): a "whoever wears the rule" selector can match
    // several combatants and so cannot be read as one counter at all.
    private static readonly ICombatantTargetSelector OnlyWearer = CombatantTargetSelectors.Source;

    private static ICombatExpression<TContext, int> PlayedCost<TContext>() where TContext : class =>
        new CardInstanceBaseCostExpression<TContext>(
            new TriggerEventCardInstanceExpression<TContext>(), StandardCombatIds.EnergyResource);

    private static ICombatExpression<TContext, int> IteratedCost<TContext>() where TContext : class =>
        new CardInstanceBaseCostExpression<TContext>(
            new IteratedCardExpression<TContext>(), StandardCombatIds.EnergyResource);

    private static ICombatExpression<TContext, int> OnWearer<TContext>(CounterId counter) where TContext : class =>
        new CombatantCounterExpression<TContext>(OnlyWearer, counter);

    private static IEffectNode<TContext> SetOnWearer<TContext>(
        CounterId counter, ICombatExpression<TContext, int> value) where TContext : class =>
        new SetCombatantCounterNode<TContext>(OnlyWearer, counter, value, relative: false);

    // ── Redacted ──────────────────────────────────────────────────────────────────────────────────────────
    //
    // "On its next play, positive numerical effects are halved, rounded down. Then Redacted clears." The engine
    // owns the halving: two reserved per-instance counters scale a card's next play and are consumed by it. The
    // content's part is the MARK beside them, which is what a rule can find and answer — an enemy that cares
    // "the player played a Redacted card" cannot see a scale factor, only a mark.
    public const string RedactedMark = "redacted";

    // The owner is a parameter and not `Source`: a redaction usually comes from an ENEMY's program, where
    // "source" is the enemy and the card belongs to the player. Getting that wrong marks a card nobody holds,
    // which is silent.
    public static IEffectNode<TContext> Redact<TContext>(
        ICombatantTargetSelector owner, ICardInstanceExpression<TContext> card) where TContext : class =>
        new CausalSequenceEffectNode<TContext>(
        [
            new SetCardInstanceMarkCounterNode<TContext>(
                owner, card, StandardCombatIds.CardOutputScaleNumeratorCounter,
                new ConstantExpression<TContext>(1), relative: false),
            new SetCardInstanceMarkCounterNode<TContext>(
                owner, card, StandardCombatIds.CardOutputScaleDenominatorCounter,
                new ConstantExpression<TContext>(2), relative: false),
            new MarkCardInstanceNode<TContext>(owner, card, new TagId(RedactedMark)),
        ]);

    // "Redact one card": the top of the draw pile, the same reading of randomness Act I's Unclaimed Property
    // Tag uses — the pile is already shuffled, so its first card is the random one.
    public static IEffectNode<EnemyActionContext> RedactOne() =>
        new ForEachCardInZoneNode<EnemyActionContext>(
            Opponent, CardZone.DrawPile,
            Redact<EnemyActionContext>(Opponent, new IteratedCardExpression<EnemyActionContext>()),
            takeFirst: 1);

    // ── Stage 6 — Scriptorium of Errata ───────────────────────────────────────────────────────────────────

    // A rule about the card that was just played, with no once-a-turn limit.
    private static EffectProgram<CardPlayedTriggeredEffectContext> WhenPlayedCardIs(
        string mark, IEffectNode<CardPlayedTriggeredEffectContext> then) =>
        new(new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            PlayedCardIs<CardPlayedTriggeredEffectContext>(mark), then));

    // "The card this play is about carries mark X."
    private static ICombatExpression<TContext, bool> PlayedCardIs<TContext>(string mark) where TContext : class =>
        new CardInstanceHasMarkExpression<TContext>(
            new TriggerEventCardInstanceExpression<TContext>(), new TagId(mark));

    // ── Stage 7 — Restricted Annex ────────────────────────────────────────────────────────────────────────

    public const string RememberedMark = "remembered_volume";
    public const string ChainReferenceMark = "referenced_chain";
    public const string RememberedVolumeId = "remembered_volume_rule";
    public const string ChainCollectId = "mnemonic_chain_collect";

    private static CounterId RememberedCounter => new("chain_remembered");

    // "The first eligible card played against the Chain becomes a remembered concrete card INSTANCE. When that
    // exact instance later re-enters the hand it is Referenced and costs 1 more; play it anyway and the Chain
    // takes 8. Let it go unplayed and the Reference fails as usual."
    //
    // "That exact instance" is what a per-card mark is for: the mark rides the copy through every zone, so the
    // Chain recognises the card coming back rather than merely a card of the same name.
    public static StatusData RememberedVolume() =>
        Rule(RememberedVolumeId, "Remembered Volume",
            "The chain remembers one book, and knows it when it comes round again.",
            [
                // The first card played is the one it remembers.
                Watch("CardPlayed", Guarded(
                    new AndExpression<CardPlayedTriggeredEffectContext>(
                        IsTheApplicant<CardPlayedTriggeredEffectContext>(),
                        Unspent<CardPlayedTriggeredEffectContext>(RememberedCounter)),
                    new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                    [
                        new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source,
                            new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                            new TagId(RememberedMark)),
                        Spend<CardPlayedTriggeredEffectContext>(RememberedCounter),
                    ]))),
                // Coming back into the hand, it is cited and it costs more.
                Watch("CardsDrawn", Guarded(
                    IsTheApplicant<CardsDrawnTriggeredEffectContext>(),
                    new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, CardZone.Hand,
                        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                        [
                            new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                                CombatantTargetSelectors.Source,
                                new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                                new TagId(ChainReferenceMark)),
                            new SetCardInstanceMarkCounterNode<CardsDrawnTriggeredEffectContext>(
                                CombatantTargetSelectors.Source,
                                new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                                StandardCombatIds.CardCostDeltaCounter,
                                new ConstantExpression<CardsDrawnTriggeredEffectContext>(1), relative: false),
                        ]),
                        markFilter: new TagId(RememberedMark)))),
                // Played anyway: the citation is answered and the chain pays for it. Looks IMMEDIATELY, like
                // every rule about the card that was just played — waiting a beat carries it out of the moment.
                Watch("CardPlayed", WhenPlayedCardIs(ChainReferenceMark,
                    new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                    [
                        new DealDamageNode<CardPlayedTriggeredEffectContext>(
                            Choir, new ConstantExpression<CardPlayedTriggeredEffectContext>(8),
                            ignoresBlock: true),
                        new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source,
                            new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                            new TagId(ChainReferenceMark), remove: true),
                    ]))),
            ]);

    // The Chain's own half: a citation left unanswered is filed against it, from IT.
    public static StatusData ChainCollect() =>
        Rule(ChainCollectId, "Overdue Volume",
            "A book you were reminded of and did not return is a book you owe for.",
            [
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    new EffectProgram<TurnStartedTriggeredEffectContext>(
                        new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                        [
                            new NoOpEffectNode<TurnStartedTriggeredEffectContext>(),
                            Unfulfilled(ChainReferenceMark, CardZone.DiscardPile),
                            Unfulfilled(ChainReferenceMark, CardZone.Hand),
                        ])),
                    CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>())),
            ]);

    // ── Stage 7 — the Checkout Codex ──────────────────────────────────────────────────────────────────────
    //
    // "After normal draw, choose one valid non-Junk card and place it visibly Behind the Desk. It is
    // temporarily unavailable. The player has three options: play another card first and it returns to hand;
    // demand immediate access with a free encounter action and it returns Redacted; or end the turn without
    // retrieving it and the Codex files 1 Overdue, the card returning at the start of the next turn."
    //
    // ⚠⚠ "IN HAND BUT UNPLAYABLE" IS NOT EXPRESSIBLE, so the card LEAVES the hand. Every prohibition the
    // engine has reads a card's DEFINITION tags (`UnplayableCardPlayValidator`) or a rule the bearer wears
    // (`CombatDecrees`, a fixed vocabulary of seven rules); nothing refuses one particular INSTANCE. So
    // Behind the Desk is a place rather than a condition: the card goes to the TOP of the draw pile wearing
    // the desk's mark, which is genuinely "temporarily unavailable" and puts it exactly where retrieving it
    // is one draw away.
    //
    // That one detail makes all three of the design's outcomes fall out of the same arrangement:
    //   · **Wait Properly** — the first card played each turn draws 1, and the top of the pile IS the held
    //     card. "Play another card first and it comes back" is then literally what happens.
    //   · **End the Turn Without Retrieval** — the Codex checks at its OWN turn start, which is the moment
    //     the player's hand is down and anything still marked was provably not retrieved. It files 1 Overdue
    //     — from itself, which is why this half lives on the Codex and not on the player — and releases the
    //     card, so the next normal draw brings it back.
    //   · **Demand Immediate Access** — NOT BUILT. It is a free player-activated action, and the engine has
    //     no player-activated abilities at all; the Municipal Dragon's two writs record the same limit and
    //     fire themselves instead. There is nothing here to fire: the whole point of the option is that the
    //     PLAYER chooses to pay a Redaction for speed. Named, not approximated.
    //
    // ⚠ "NON-JUNK" IS READ AS "THE FIRST CARD". A zone iteration can require a tag but not refuse one, so
    // excluding Junk is not expressible — the same reading Act I takes for the Wrong-Window Scribe's "first
    // non-Junk card type", already recorded in ADAPTATIONS.
    public const string BehindTheDeskMark = "behind_the_desk";
    public const string AskAtTheDeskCardId = "ask_at_the_desk";

    // DEMAND IMMEDIATE ACCESS (content cut 2026-10-29): a free card dealt with the shelving. Played, it is the first
    // card of the turn like any other — so it hands the held card back — and on the way it prices that card 1
    // higher for its next play (the engine's per-instance cost delta, spent by the play that pays it).
    public static CardData AskAtTheDesk() => new()
    {
        Id = AskAtTheDeskCardId,
        NameKey = "Ask at the Desk",
        DescriptionKey = "Get the card behind the desk back now. It costs 1 more this time.",
        Costs = [],
        Tags = [new TagId("form")],
        Program = new EffectProgram<CardPlayContext>(
            new ForEachCardInZoneNode<CardPlayContext>(
                CombatantTargetSelectors.Source, CardZone.DrawPile,
                new SetCardInstanceMarkCounterNode<CardPlayContext>(
                    CombatantTargetSelectors.Source, new IteratedCardExpression<CardPlayContext>(),
                    StandardCombatIds.CardCostDeltaCounter, new ConstantExpression<CardPlayContext>(1), relative: true),
                markFilter: new TagId(BehindTheDeskMark))),
        PlayedCardDestinationZone = CardZone.ExhaustPile,
        TurnEndHandDestinationZone = CardZone.ExhaustPile,
    };
    public const string BehindTheDeskId = "behind_the_desk_rule";

    private static CounterId DeskRetrievedCounter => new("desk_retrieved");
    private static CounterId DeskShelvedCounter => new("desk_shelved");

    public static StatusData BehindTheDesk()
    {
        // After the draw: one card off the hand and onto the top of the pile, wearing the desk's mark.
        //
        // ⚠⚠ ONCE PER TURN, AND THAT LATCH IS LOAD-BEARING. The design says "after NORMAL draw", and the
        // difference is not pedantry: handing the card back is itself a draw, so an unlatched desk shelves a
        // fresh card the instant it returns the last one. The symptom was the retrieval looking broken —
        // a card played, the hand one lighter, something still marked in the pile — when in fact the fetch
        // had worked and the desk had simply taken the next card. MEASURED: with the fetch replaced by a
        // bare draw the hand went 4→4 and TWO cards were marked, which is what named the real culprit.
        var shelve = new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                new AndExpression<CardsDrawnTriggeredEffectContext>(
                    IsTheApplicant<CardsDrawnTriggeredEffectContext>(),
                    Unspent<CardsDrawnTriggeredEffectContext>(DeskShelvedCounter)),
                new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, CardZone.Hand,
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                            CombatantTargetSelectors.Source,
                            new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                            new TagId(BehindTheDeskMark)),
                        new MoveCardToZoneNode<CardsDrawnTriggeredEffectContext>(
                            CombatantTargetSelectors.Source,
                            new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                            CardZone.DrawPile, placement: ZonePlacement.Top),
                        Spend<CardsDrawnTriggeredEffectContext>(DeskShelvedCounter),
                        // …and the desk bell: Ask at the Desk hands it back at once, for a price.
                        new CreateCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, new CardDefinitionId(AskAtTheDeskCardId), CardZone.Hand,
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                    ]),
                    takeFirst: 1)));

        // Waiting properly: the first card played this turn fetches it back. The latch is what makes it the
        // FIRST — and it is cleared by the shelving above, which happens once per turn at the draw.
        // ⚠ THE CONDITION IS THE CARD, NOT WHO PLAYED. Guarding this on `IsTheApplicant` made it never fire:
        // the shelving above is a CardsDrawn hook, whose acting Source is the combatant who DREW, while this
        // is a CardPlayed hook on a status the ENEMY wears — and the two do not agree about who Source is.
        // MEASURED, not reasoned: the card sat in the pile still marked after a play (`markedInDraw=1`).
        // The Reference machinery next door never asks either; it guards on the mark, which is the fact that
        // actually matters. So does this, and the DRAW lives INSIDE the loop over the marked card — so it
        // happens exactly when there is something behind the desk to hand back, and never otherwise.
        var fetch = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                Unspent<CardPlayedTriggeredEffectContext>(DeskRetrievedCounter),
                new ForEachCardInZoneNode<CardPlayedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, CardZone.DrawPile,
                    new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                    [
                        // Released first, then drawn: a card handed back still wearing the desk's mark would
                        // read as being behind a desk it is no longer behind.
                        new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source,
                            new IteratedCardExpression<CardPlayedTriggeredEffectContext>(),
                            new TagId(BehindTheDeskMark), remove: true),
                        new DrawCardsNode<CardPlayedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source,
                            new ConstantExpression<CardPlayedTriggeredEffectContext>(1)),
                        Spend<CardPlayedTriggeredEffectContext>(DeskRetrievedCounter),
                    ]),
                    markFilter: new TagId(BehindTheDeskMark))));

        // My turn, and the hand is down: anything still behind the desk was never asked for.
        var charge = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                new ForEachCardInZoneNode<TurnStartedTriggeredEffectContext>(
                    Opponent, CardZone.DrawPile,
                    new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                    [
                        new MarkCardInstanceNode<TurnStartedTriggeredEffectContext>(
                            Opponent, new IteratedCardExpression<TurnStartedTriggeredEffectContext>(),
                            new TagId(BehindTheDeskMark), remove: true),
                        // Filed by ME — the reason this half is the Codex's and not the player's.
                        new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                            Opponent, new StatusDefinitionId(OverdueId),
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                    ]),
                    markFilter: new TagId(BehindTheDeskMark)),
                // A fresh turn shelves again and may retrieve again. Released HERE, at the Codex's own turn
                // start, because that is the last moment before the player's next normal draw.
                new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                    Opponent, DeskRetrievedCounter,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false),
                new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                    Opponent, DeskShelvedCounter,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false),
            ]));

        return Rule(BehindTheDeskId, "Behind the Desk",
            "Each turn one of your cards goes behind the desk. Your first card played hands it back; Ask at the "
            + "Desk does it at once, but it costs 1 more. Left there: 1 Overdue.",
            [
                new StatusTriggerData("CardsDrawn", JsonSerializer.SerializeToElement(
                    shelve, CombatJson.CreateOptions<CardsDrawnTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                    fetch, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    charge, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>())),
            ]);
    }

    // ── Stage 9 — Necrology Vaults ────────────────────────────────────────────────────────────────────────

    // BUILT (see the death clause under Stage 9's Reference above). The open question that stood here — does
    // a bearer-scoped Downed trigger fire for the bearer's own downing — turned out to be the wrong question:
    // the answer is yes, and it still does not help, because the combat is already over by then. The cure is
    // to stand BEFORE the down rather than to undo one.

    // ── Stage 9 — the Spare-Life Jar ──────────────────────────────────────────────────────────────────────
    //
    // "When another enemy dies, store its identity. Prepare Pour Back the Life (countdown 1 enemy turn). If
    // the Jar remains alive when the countdown resolves, return the stored enemy once at 30% Max HP. If the
    // Jar dies before resolution, Stored Life is lost. Maximum one resurrection per combat."
    //
    // ⚠⚠ THE JAR CATCHES THE ALLY INSTEAD OF POURING IT BACK. The engine can stand BEFORE a death and not
    // undo one (the Blank Death Certificate above is the long version of why), so the countdown collapses
    // into the moment the fatal blow lands: the ally simply does not die, and is left on 30 % of its own
    // maximum. Everything else the design asks for survives the move intact —
    //   · "one resurrection per combat" is the one-shot prevention, spent by firing;
    //   · **"if the Jar dies before resolution, Stored Life is lost" is the whole point of the mechanic**,
    //     and it still holds: the Jar's own downing takes the clause off its allies, so a player who breaks
    //     the jar first faces bodies with no spare life. What is lost is the WINDOW — you cannot kill the
    //     Jar in the beat between the ally's death and its return, because there is no longer a beat.
    //
    // ⚠ 30 % IS A NUMBER, NOT A PERCENTAGE. `StatusDeathPreventionData.SurvivingHealth` is absolute, and the
    // Jar's two partners are 35 and 63 HP, so one clause cannot be 30 % of both. Each partner therefore gets
    // its own, fielded through the encounter's slot-indexed scaffolding — which is also how the design states
    // these numbers (per body), and it is why a THIRD partner would need a third clause rather than working
    // by accident.
    public const string StoredLifeId = "stored_life";
    public const string SpareLifeOuroborosId = "spare_life_ouroboros";
    public const string SpareLifeTestatorId = "spare_life_testator";

    private const int OuroborosSpare = 11;      // 30 % of 35, rounded up
    private const int TestatorSpare = 15;       // 30 % of 48, rounded up

    public static StatusData SpareLifeOuroboros() => SpareLife(SpareLifeOuroborosId, OuroborosSpare);

    public static StatusData SpareLifeTestator() => SpareLife(SpareLifeTestatorId, TestatorSpare);

    private static StatusData SpareLife(string id, int survivingHealth) => new()
    {
        Id = id,
        NameKey = "Stored Life",
        DescriptionKey = "The jar holds a life for this one. Break the jar and it is lost.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
        DeathPrevention = new StatusDeathPreventionData(survivingHealth, []),
    };

    // The marker the Jar wears: what says on the body which enemy the spare life belongs to, and what the
    // jar-breaking rule finds it by.
    public static StatusData StoredLife() =>
        Rule(StoredLifeId, "Spare Life",
            "While the jar stands, the body beside it has a life in reserve.", []);

    // ★ AND THE JAR BREAKING LOSES WHAT IT HELD — as an ENCOUNTER trigger, which is what finally worked.
    // Written as a bearer-scoped Downed rule on the Jar's own status it did nothing (measured: breaking the
    // jar first still left the body beside it catching), and an encounter trigger takes the status filter out
    // of the question entirely: it fires for any downing, and the guard is the one fact that matters — the
    // body that went down is the one holding the spare life.
    //
    // In a Downed program the Source IS the downed body (CombatantDownedTriggeredEffectTargetResolver), which
    // is exactly what this needs: its allies are alive, so they resolve normally, and the clause comes off
    // whichever of them was holding it.
    public static EncounterTriggerData BreakTheJar()
    {
        IEffectNode<CombatantDownedTriggeredEffectContext> Lose(string clause) =>
            new RemoveStatusNode<CombatantDownedTriggeredEffectContext>(
                CombatantTargetSelectors.AllAlliesOfSourceWithStatus(new StatusDefinitionId(clause)),
                new StatusDefinitionId(clause));

        // ⚠⚠ THE GUARD READS `sourceIncludingDowned`, AND THAT IS THE WHOLE DIFFICULTY OF THIS RULE. Asked
        // about `eventTarget` — the obvious way to say "the body that went down" — the guard was simply
        // FALSE and the clause never came off: an ordinary target selector will not resolve to a combatant
        // that is no longer living, which is the trap this act's notes already record for allies and which
        // costs exactly as much when it is the downed body itself being asked about. The engine ships this
        // selector for precisely this moment. MEASURED: with the guard dropped the clause came off, which is
        // what named it.
        var program = new EffectProgram<CombatantDownedTriggeredEffectContext>(
            new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
                new TargetHasStatusExpression<CombatantDownedTriggeredEffectContext>(
                    CombatantTargetSelectors.SourceIncludingDowned, new StatusDefinitionId(StoredLifeId)),
                new CausalSequenceEffectNode<CombatantDownedTriggeredEffectContext>(
                    [Lose(SpareLifeOuroborosId), Lose(SpareLifeTestatorId)])));

        return new EncounterTriggerData("Downed",
            JsonSerializer.SerializeToElement(
                program, CombatJson.CreateOptions<CombatantDownedTriggeredEffectContext>()));
    }

    // ── Stage 4 — The Hushed Reading Room ─────────────────────────────────────────────────────────────────
    //
    // The Reading Room attacks the HAND: what you are still holding, how much of it you spent, what you never
    // got to say. All three rules live on the PLAYER (EncounterPassives.HeroOpeningStatuses) and watch the
    // whole fight, because the events they answer are the player's own.
    //
    // Three facts these rules are built on, all measured and all easy to get backwards:
    //   • the played-card count INCLUDES the card being played, so "the fourth card" is a count of four;
    //   • that card is already OUT of the hand when the rule runs, so reaching into the hand cannot catch it;
    //   • the played card is STILL IN THE HAND at the very first instant of a CardPlayed trigger, and gone a
    //     beat later — so a rule that reaches into the hand must let something run first or it takes the card
    //     that was just played, which is invisible because that card was on its way to the discard pile
    //     anyway. `Guarded` puts a no-op in front of every rule for exactly that reason. See
    //     RogueDeck.Sandbox.Tests/CardPlayedTriggerHandTimingTests.

    // A rule about the PLAYED card looks immediately: waiting a beat is what carries that card out of reach.
    private static EffectProgram<TContext> Immediate<TContext>(
        ICombatExpression<TContext, bool> when, IEffectNode<TContext> then) where TContext : class =>
        new(new ConditionalEffectNode<TContext>(when, then));

    // A guarded rule. The no-op is not decoration: it lets the played card leave the hand before anything
    // reaches in, which is the difference between taking a card the player still holds and taking the one they
    // just spent.
    private static EffectProgram<TContext> Guarded<TContext>(
        ICombatExpression<TContext, bool> when, IEffectNode<TContext> then) where TContext : class =>
        new(new CausalSequenceEffectNode<TContext>(
        [
            new NoOpEffectNode<TContext>(),
            new ConditionalEffectNode<TContext>(when, then),
        ]));

    private static readonly ICombatExpression<CardPlayedTriggeredEffectContext, int> PortraitBlock =
        new CombatantDefensivePoolExpression<CardPlayedTriggeredEffectContext>(
            CombatantTargetSelectors.LowestHealthEnemyOfSource, StandardCombatIds.BlockDefensivePool);

    private static readonly ICombatantTargetSelector Choir =
        CombatantTargetSelectors.LowestHealthEnemyOfSource;

    private static ICombatExpression<TContext, bool> PlayedThisTurn<TContext>(
        ComparisonOperator op, ICombatExpression<TContext, int> than) where TContext : class =>
        new ComparisonExpression<TContext>(
            new CardsPlayedThisTurnExpression<TContext>(CombatantTargetSelectors.Source), op, than);

    private static ICombatExpression<TContext, bool> Unspent<TContext>(CounterId latch) where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantCounterExpression<TContext>(CombatantTargetSelectors.Source, latch),
            ComparisonOperator.Equal, new ConstantExpression<TContext>(0));

    private static IEffectNode<TContext> Spend<TContext>(CounterId latch) where TContext : class =>
        new SetCombatantCounterNode<TContext>(
            CombatantTargetSelectors.Source, latch,
            new ConstantExpression<TContext>(1), relative: false);

    private static StatusTriggerData Watch<TContext>(string trigger, EffectProgram<TContext> program)
        where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()),
            StatusTriggerScope.Anywhere);

    // ── shapes ────────────────────────────────────────────────────────────────────────────────────────────

    // The player, addressed from inside an enemy's own trigger and serializable into the export.
    private static readonly ICombatantTargetSelector Opponent =
        CombatantTargetSelectors.LowestHealthEnemyOfSource;

    private static StatusData Rule(
        string id, string name, string description, IReadOnlyList<StatusTriggerData> triggers) => new()
        {
            Id = id,
            NameKey = name,
            DescriptionKey = description,
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = false,
            Tags = [],
            PassiveModifiers = [],
            Triggers = triggers,
        };
}
