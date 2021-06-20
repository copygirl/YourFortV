using System.Collections.Generic;

public interface IWorldGenerator
{
    string Name { get; }
    void SetSeed(int seed); // TODO: Allow loading additional generator options.
    void Generate(Chunk chunk);
}

public static class WorldGeneratorRegistry
{
    private static readonly Dictionary<string, IWorldGenerator> _generators
        = new Dictionary<string, IWorldGenerator>();

    static WorldGeneratorRegistry()
    {
        Register(new GeneratorVoid());
        Register(new GeneratorSimple());
    }

    public static void Register(IWorldGenerator generator)
        => _generators.Add(generator.Name, generator);
    public static IWorldGenerator GetOrNull(string name)
        => _generators.TryGetValue(name, out var value) ? value : null;
}
