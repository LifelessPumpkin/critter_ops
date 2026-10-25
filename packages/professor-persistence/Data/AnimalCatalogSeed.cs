using skipper_api.Domain.Animals;

namespace skipper_api.Data;

internal static class AnimalCatalogSeed
{
    internal static readonly AnimalGroup[] Groups =
    [
        new() { Id = 1, Name = "Mammal", SortOrder = 10, LegacyAnimalType = AnimalType.Mammal },
        new() { Id = 2, Name = "Bird", SortOrder = 20, LegacyAnimalType = AnimalType.Avian },
        new() { Id = 3, Name = "Reptile", SortOrder = 30, LegacyAnimalType = AnimalType.Reptile },
        new() { Id = 4, Name = "Amphibian", SortOrder = 40, LegacyAnimalType = AnimalType.Amphibian },
        new() { Id = 5, Name = "Fish", SortOrder = 50, LegacyAnimalType = AnimalType.Fish },
        new() { Id = 6, Name = "Invertebrate", SortOrder = 60, LegacyAnimalType = AnimalType.Invertebrate },
        new() { Id = 7, Name = "Other", SortOrder = 70, LegacyAnimalType = AnimalType.Other },
    ];
    internal static readonly Species[] Species =
    [
        new() { Id = 1, CommonName = "Domestic dog", ScientificName = "Canis lupus familiaris", AnimalGroupId = 1, PreferredVariantType = VariantType.Breed },
        new() { Id = 2, CommonName = "Domestic cat", ScientificName = "Felis catus", AnimalGroupId = 1, PreferredVariantType = VariantType.Breed },
        new() { Id = 3, CommonName = "Domestic ferret", ScientificName = "Mustela putorius furo", AnimalGroupId = 1, PreferredVariantType = null },
        new() { Id = 4, CommonName = "Ball python", ScientificName = "Python regius", AnimalGroupId = 3, PreferredVariantType = VariantType.Morph },
        new() { Id = 5, CommonName = "Bearded dragon", ScientificName = "Pogona vitticeps", AnimalGroupId = 3, PreferredVariantType = VariantType.Morph },
        new() { Id = 6, CommonName = "Leopard gecko", ScientificName = "Eublepharis macularius", AnimalGroupId = 3, PreferredVariantType = VariantType.Morph },
        new() { Id = 7, CommonName = "Corn snake", ScientificName = "Pantherophis guttatus", AnimalGroupId = 3, PreferredVariantType = VariantType.Morph },
        new() { Id = 8, CommonName = "Axolotl", ScientificName = "Ambystoma mexicanum", AnimalGroupId = 4, PreferredVariantType = VariantType.Morph },
        new() { Id = 9, CommonName = "Goldfish", ScientificName = "Carassius auratus", AnimalGroupId = 5, PreferredVariantType = VariantType.Variety },
        new() { Id = 10, CommonName = "Betta", ScientificName = "Betta splendens", AnimalGroupId = 5, PreferredVariantType = VariantType.Strain },
        new() { Id = 11, CommonName = "Budgerigar", ScientificName = "Melopsittacus undulatus", AnimalGroupId = 2, PreferredVariantType = VariantType.Mutation },
        new() { Id = 12, CommonName = "Cockatiel", ScientificName = "Nymphicus hollandicus", AnimalGroupId = 2, PreferredVariantType = VariantType.Mutation },
        new() { Id = 13, CommonName = "Guinea pig", ScientificName = "Cavia porcellus", AnimalGroupId = 1, PreferredVariantType = VariantType.Breed },
        new() { Id = 14, CommonName = "Rabbit", ScientificName = "Oryctolagus cuniculus", AnimalGroupId = 1, PreferredVariantType = VariantType.Breed },
        new() { Id = 15, CommonName = "Madagascar hissing cockroach", ScientificName = "Gromphadorhina portentosa", AnimalGroupId = 6, PreferredVariantType = null },
        new() { Id = 16, CommonName = "Mexican red-knee tarantula", ScientificName = "Brachypelma hamorii", AnimalGroupId = 6, PreferredVariantType = null },
    ];
    internal static readonly AnimalVariant[] Variants =
    [
        new() { Id = 1, SpeciesId = 1, Name = "Labrador Retriever", VariantType = VariantType.Breed },
        new() { Id = 2, SpeciesId = 1, Name = "Poodle", VariantType = VariantType.Breed },
        new() { Id = 3, SpeciesId = 4, Name = "Albino", VariantType = VariantType.Morph },
        new() { Id = 4, SpeciesId = 4, Name = "Pastel", VariantType = VariantType.Morph },
    ];
}
