using System.Text.RegularExpressions;

namespace HowellSync.Providers.Gbif;

public static class GbifTaxonMapper
{
    public static string Clean(string? name) => Regex.Replace(name?.Trim() ?? "", @"\s+", " ");
    public static string Key(string? name) => Clean(name).ToLowerInvariant();
    public static bool IsSynonym(string? status) => status is "SYNONYM" or "HOMOTYPIC_SYNONYM" or "HETEROTYPIC_SYNONYM";
    public static string? Group(GbifTaxon taxon)
    {
        if (taxon.Kingdom != "Animalia") return null;
        return taxon.Class switch
        {
            "Mammalia" => "Mammal", "Aves" => "Bird", "Reptilia" => "Reptile", "Amphibia" => "Amphibian",
            "Actinopterygii" or "Chondrichthyes" or "Dipnoi" or "Coelacanthi" or "Myxini" or "Cephalaspidomorphi" => "Fish",
            _ => taxon.Phylum is "Arthropoda" or "Mollusca" or "Annelida" or "Cnidaria" or "Echinodermata" or "Porifera" or "Nematoda" or "Platyhelminthes" ? "Invertebrate" : null
        };
    }
}
