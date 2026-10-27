namespace NotchBar.Core.Models;

/// <summary>Forma de la pastilla cuando está contraída.</summary>
public enum NotchStyle
{
    /// <summary>Pegada al borde superior, esquinas redondeadas solo abajo.</summary>
    Notch,

    /// <summary>Separada del borde, redondeada por completo.</summary>
    Capsule,

    /// <summary>Solo una rayita mínima arriba.</summary>
    Indicator,
}
