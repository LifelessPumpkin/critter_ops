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
        var request = new CreateAnimalRequestDto { Name = "Mango", Species = name, SpeciesId = id, EnclosureId = 1, AcquiredDate = new DateOnly(2026, 1, 1), Status = AnimalStatus.Active, Sex = AnimalSex.Unknown };
        Assert.Equal(expected, Validator.TryValidateObject(request, new ValidationContext(request), new List<ValidationResult>(), true));
    }
    [Fact]
    public void UpdateDistinguishesOmittedFromExplicitlyClearedReferences()
    {
        Assert.False(JsonSerializer.Deserialize<UpdateAnimalRequestDto>("{}")!.SpeciesIdSpecified);
        var clear = JsonSerializer.Deserialize<UpdateAnimalRequestDto>("{\"SpeciesId\":null,\"AnimalVariantId\":null}")!;
        Assert.True(clear.SpeciesIdSpecified); Assert.True(clear.AnimalVariantIdSpecified); Assert.Null(clear.SpeciesId);
    }
}
