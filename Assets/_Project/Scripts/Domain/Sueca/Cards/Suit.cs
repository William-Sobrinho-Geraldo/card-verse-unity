namespace SuecaOnline.Domain.Sueca.Cards
{
    /// <summary>
    /// Naipe da carta. A ordem declarada é apenas de identidade; a força relativa
    /// entre naipes nunca é comparada (o trunfo é definido dinamicamente por rodada).
    /// </summary>
    public enum Suit
    {
        Ouros = 0,
        Copas = 1,
        Espadas = 2,
        Paus = 3
    }
}
