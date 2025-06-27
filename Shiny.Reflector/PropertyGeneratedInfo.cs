namespace Shiny.Reflector;

public record PropertyGeneratedInfo(
    string Name, 
    Type Type, 
    bool HasSetter
);