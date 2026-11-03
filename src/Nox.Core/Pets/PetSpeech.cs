namespace Nox.Core.Pets;

/// <summary>Lo que pone en el bocadillo de la mascota.</summary>
public static class PetSpeech
{
    public static string Text(PetKind kind, PetBubble bubble) => bubble switch
    {
        PetBubble.Talk => kind == PetKind.Beagle ? "¡Guau!" : "¡Miau!",
        PetBubble.Hungry => "¡Tengo hambre!",
        _ => "",
    };
}
