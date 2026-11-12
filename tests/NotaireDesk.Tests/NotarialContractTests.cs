using Microsoft.EntityFrameworkCore;
using NotaireDesk.Domain.Contracts;
using NotaireDesk.Domain.Parties;
using NotaireDesk.Domain.Properties;
using NotaireDesk.Infrastructure.Persistence;
using Xunit;

namespace NotaireDesk.Tests;

public sealed class NotarialContractTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 12, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset GeneratedAt = CreatedAt.AddMinutes(10);

    [Fact]
    public void GenerationSnapshot_PreservesPartyAndPropertyValuesAfterMasterDataChanges()
    {
        var party = Party.Create(PartyKind.NaturalPerson, "أحمد بن صالح", "Ahmed Ben Salah", CreatedAt);
        var property = PropertyRecord.Create(
            PropertyKind.Apartment,
            "شقة رقم 12",
            "Appartement numéro 12",
            "وهران",
            "السانية",
            92.5m,
            "OR-E-TEST-0012",
            CreatedAt);
        var contract = CreateContract();
        contract.AttachParty(party.Id, ContractPartyRole.Buyer, CreatedAt);
        contract.AttachProperty(property.Id, CreatedAt);

        var snapshot = contract.CreateGenerationSnapshot(
            Guid.NewGuid(), 1, Guid.NewGuid(), GeneratedAt, [party], [property]);

        party.UpdateNames("اسم جديد", "Nouveau nom", GeneratedAt.AddMinutes(1));
        property.UpdateDescription(
            "وصف معدل",
            "Description modifiée",
            "وهران",
            "بئر الجير",
            95m,
            "OR-E-CHANGED",
            GeneratedAt.AddMinutes(1));

        var partySnapshot = Assert.Single(snapshot.Parties);
        Assert.Equal("أحمد بن صالح", partySnapshot.NameAr);
        Assert.Equal("Ahmed Ben Salah", partySnapshot.NameFr);

        var propertySnapshot = Assert.Single(snapshot.Properties);
        Assert.Equal("شقة رقم 12", propertySnapshot.DescriptionAr);
        Assert.Equal("Appartement numéro 12", propertySnapshot.DescriptionFr);
        Assert.Equal("السانية", propertySnapshot.Commune);
        Assert.Equal(92.5m, propertySnapshot.AreaSquareMeters);
        Assert.Equal("OR-E-TEST-0012", propertySnapshot.LandRegistryReference);
    }

    [Fact]
    public void SamePartyCanBeAttachedInDifferentRoles_AndIsSnapshottedForEachRole()
    {
        var party = Party.Create(PartyKind.NaturalPerson, "الشخص نفسه", null, CreatedAt);
        var contract = CreateContract();

        contract.AttachParty(party.Id, ContractPartyRole.Buyer, CreatedAt);
        contract.AttachParty(party.Id, ContractPartyRole.Representative, CreatedAt.AddMinutes(1));

        var snapshot = contract.CreateGenerationSnapshot(
            Guid.NewGuid(), 1, Guid.NewGuid(), GeneratedAt, [party], []);

        Assert.Equal(2, snapshot.Parties.Count);
        Assert.Contains(snapshot.Parties, x => x.Role == ContractPartyRole.Buyer);
        Assert.Contains(snapshot.Parties, x => x.Role == ContractPartyRole.Representative);
    }

    [Fact]
    public void DuplicatePartyRoleAndDuplicatePropertyAreRejected()
    {
        var party = Party.Create(PartyKind.NaturalPerson, "طرف", null, CreatedAt);
        var property = CreateProperty();
        var contract = CreateContract();

        contract.AttachParty(party.Id, ContractPartyRole.Buyer, CreatedAt);
        contract.AttachProperty(property.Id, CreatedAt);

        Assert.Throws<InvalidOperationException>(
            () => contract.AttachParty(party.Id, ContractPartyRole.Buyer, CreatedAt.AddMinutes(1)));
        Assert.Throws<InvalidOperationException>(
            () => contract.AttachProperty(property.Id, CreatedAt.AddMinutes(1)));

        Assert.Single(contract.Parties);
        Assert.Single(contract.Properties);
    }

    [Fact]
    public void InvalidTimestampDoesNotPartiallyAttachParty()
    {
        var party = Party.Create(PartyKind.NaturalPerson, "طرف", null, CreatedAt);
        var contract = CreateContract();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => contract.AttachParty(party.Id, ContractPartyRole.Buyer, CreatedAt.AddSeconds(-1)));

        Assert.Empty(contract.Parties);
        Assert.Equal(CreatedAt, contract.UpdatedAt);
    }

    [Fact]
    public void GenerationSnapshotRequiresEveryLinkedSourceRecord()
    {
        var party = Party.Create(PartyKind.NaturalPerson, "طرف", null, CreatedAt);
        var property = CreateProperty();
        var contract = CreateContract();
        contract.AttachParty(party.Id, ContractPartyRole.Buyer, CreatedAt);
        contract.AttachProperty(property.Id, CreatedAt);

        Assert.Throws<InvalidOperationException>(() => contract.CreateGenerationSnapshot(
            Guid.NewGuid(), 1, Guid.NewGuid(), GeneratedAt, [], [property]));

        Assert.Empty(contract.Generations);
    }

    [Fact]
    public void ContractStatusTransitionsRequireExplicitNotaryApproval()
    {
        var contract = CreateContract();
        var notaryId = Guid.NewGuid();

        Assert.Equal(ContractStatus.Draft, contract.Status);
        contract.SubmitForReview(CreatedAt.AddMinutes(1));
        Assert.Equal(ContractStatus.UnderReview, contract.Status);

        Assert.Throws<InvalidOperationException>(() => contract.MarkExecuted(CreatedAt.AddMinutes(2)));
        contract.Approve(notaryId, CreatedAt.AddMinutes(2));
        Assert.Equal(ContractStatus.Approved, contract.Status);
        Assert.Equal(notaryId, contract.ApprovedBy);

        contract.MarkExecuted(CreatedAt.AddMinutes(3));
        Assert.Equal(ContractStatus.Executed, contract.Status);

        contract.Archive(CreatedAt.AddMinutes(4));
        Assert.Equal(ContractStatus.Archived, contract.Status);
        Assert.Throws<InvalidOperationException>(() => contract.ReturnToDraft(CreatedAt.AddMinutes(5)));
    }

    [Fact]
    public void ContractUnderReviewCannotBeEditedUntilReturnedToDraft()
    {
        var party = Party.Create(PartyKind.NaturalPerson, "طرف", null, CreatedAt);
        var contract = CreateContract();
        contract.SubmitForReview(CreatedAt.AddMinutes(1));

        Assert.Throws<InvalidOperationException>(
            () => contract.AttachParty(party.Id, ContractPartyRole.Buyer, CreatedAt.AddMinutes(2)));

        contract.ReturnToDraft(CreatedAt.AddMinutes(3));
        contract.AttachParty(party.Id, ContractPartyRole.Buyer, CreatedAt.AddMinutes(4));
        Assert.Single(contract.Parties);
    }

    private static NotarialContract CreateContract() =>
        NotarialContract.Create("2026/001", "Vente immobilière", Guid.NewGuid(), CreatedAt);

    private static PropertyRecord CreateProperty() => PropertyRecord.Create(
        PropertyKind.Apartment,
        "شقة اختبار",
        "Appartement test",
        "وهران",
        "السانية",
        90m,
        null,
        CreatedAt);
}
