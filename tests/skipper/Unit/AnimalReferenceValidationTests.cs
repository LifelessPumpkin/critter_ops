using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using skipper_api.Domain.Animals;
using skipper_api.Dtos.Animals;
using Xunit;
namespace MaryAnn.Skipper.UnitTests;
public class AnimalReferenceValidationTests
{
    [Theory]
    [InlineData(null, null, false)]
    [InlineData(" ", null, false)]
    [InlineData("Custom species", null, true)]
    [InlineData(null, 4, true)]
    [InlineData("Custom", -1, false)]
    public void SpeciesRequiresCatalogOrNonblankCustom(string? name, int? id, bool expected)
    {
        var request = new CreateAnimalRequestDto { Name = "Mango", CustomSpeciesName = name, SpeciesId = id, EnclosureId = 1, AcquiredDate = new DateOnly(2026, 1, 1), Status = AnimalStatus.Active, Sex = AnimalSex.Unknown };
        Assert.Equal(expected, Validator.TryValidateObject(request, new ValidationContext(request), new List<ValidationResult>(), true));
    }
    [Fact]
    public void UpdateDistinguishesOmittedFromExplicitlyClearedReferences()
    {
        Assert.False(JsonSerializer.Deserialize<UpdateAnimalRequestDto>("{}")!.SpeciesIdSpecified);
        var clear = JsonSerializer.Deserialize<UpdateAnimalRequestDto>("{\"SpeciesId\":null,\"AnimalVariantId\":null}")!;
        Assert.True(clear.SpeciesIdSpecified); Assert.True(clear.AnimalVariantIdSpecified); Assert.Null(clear.SpeciesId);
    }
    [Fact]
    public void ExplicitCustomContractsRejectConflictsAndAcceptOmittedUpdateReferences()
    {
        var create = new CreateAnimalRequestDto { Name = "Custom", CustomSpeciesName = "Hybrid", EnclosureId = 1, AcquiredDate = new(2026, 1, 1), Status = AnimalStatus.Active, Sex = AnimalSex.Unknown };
        bool Valid(object value) => Validator.TryValidateObject(value, new ValidationContext(value), new List<ValidationResult>(), true);
        Assert.True(Valid(create));
        create.SpeciesId = 4;
        Assert.False(Valid(create));
        create.CustomSpeciesName = null;
        create.AnimalVariantId = 3;
        create.CustomVariantName = "Different morph";
        Assert.False(Valid(create));
        create.CustomVariantName = null;
        Assert.True(Valid(create));
        var update = new UpdateAnimalRequestDto { Name = "Renamed", EnclosureId = 1, AcquiredDate = new(2026, 1, 1), Status = AnimalStatus.Active, Sex = AnimalSex.Unknown };
        Assert.True(Valid(update));
        var clear = JsonSerializer.Deserialize<UpdateAnimalRequestDto>("{\"CustomVariantName\":null}")!;
        Assert.True(clear.CustomVariantNameSpecified);
        Assert.False(clear.CustomSpeciesNameSpecified);
    }
    [Fact]
    public void ObsoleteWriteFieldsAreRejectedRatherThanSilentlyIgnored()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<CreateAnimalRequestDto>("{\"Species\":\"Legacy\"}"));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<UpdateAnimalRequestDto>("{\"SubspeciesOrMorph\":\"Legacy\"}"));
    }
}
