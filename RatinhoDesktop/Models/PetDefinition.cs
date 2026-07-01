using System.Collections.Generic;

namespace RatinhoDesktop.Models;

/// <summary>
/// Tipo de som sintetizado a ser usado para o efeito de "clique" de cada bichinho.
/// Cada um gera uma forma de onda diferente em SoundGenerator, então não precisamos
/// de arquivos de áudio externos para cada bicho.
/// </summary>
public enum SoundCharacter
{
    Squeak,   // rato: guincho agudo e curto
    Moo,      // vaca: som grave e longo, com vibrato
    Meow,     // gato: miado de duas sílabas
    Pop,      // efeitos "genéricos" (dança/limpeza/etc): som curto e alegre
    Chime     // som tipo "sininho", usado para o eisque
}

/// <summary>
/// Descreve um bichinho selecionável: nome de exibição, gif e o som associado a ele.
/// </summary>
public class PetDefinition
{
    public string Id { get; }
    public string DisplayName { get; }
    public string PackUri { get; }
    public SoundCharacter Sound { get; }

    public PetDefinition(string id, string displayName, string packUri, SoundCharacter sound)
    {
        Id = id;
        DisplayName = displayName;
        PackUri = packUri;
        Sound = sound;
    }

    /// <summary>
    /// Catálogo com todos os bichinhos disponíveis na pasta Assets.
    /// Para adicionar um novo gif no futuro, basta:
    ///   1) Colocar o arquivo em Assets/Novos/
    ///   2) Adicionar uma linha &lt;Resource Include="Assets\Novos\seuarquivo.gif" /&gt; no .csproj
    ///   3) Adicionar uma entrada aqui embaixo.
    /// </summary>
    public static readonly List<PetDefinition> Catalog = new()
    {
        new PetDefinition("rato", "Ratinho", "pack://application:,,,/Assets/rato.gif", SoundCharacter.Squeak),
        new PetDefinition("vaca", "Vaca", "pack://application:,,,/Assets/Novos/vaca.gif", SoundCharacter.Moo),
        new PetDefinition("cat", "Gatinho", "pack://application:,,,/Assets/Novos/cat.gif", SoundCharacter.Meow),
        new PetDefinition("silly-cat-dance", "Gato Dançarino", "pack://application:,,,/Assets/Novos/silly-cat-dance.gif", SoundCharacter.Pop),
        new PetDefinition("dancing-dance", "Dançarino", "pack://application:,,,/Assets/Novos/dancing-dance.gif", SoundCharacter.Pop),
        new PetDefinition("limpando", "Limpando", "pack://application:,,,/Assets/Novos/limpando.gif", SoundCharacter.Pop),
        new PetDefinition("eisque", "Eisque", "pack://application:,,,/Assets/Novos/eisque.gif", SoundCharacter.Chime),
    };

    public static PetDefinition GetByIdOrDefault(string? id)
    {
        if (!string.IsNullOrEmpty(id))
        {
            foreach (var pet in Catalog)
            {
                if (pet.Id == id) return pet;
            }
        }
        return Catalog[0];
    }
}
