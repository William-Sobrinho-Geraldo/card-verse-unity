namespace SuecaOnline.Domain.Sueca.Cards
{
    /// <summary>
    /// Valor nominal da carta da Sueca (baralho francês sem 8, 9 e 10).
    /// Os valores numéricos do enum refletem a ordem do baralho comum e
    /// NÃO representam a força do jogo. Use <see cref="RankStrength"/>.
    /// </summary>
    public enum Rank
    {
        Dois = 2,
        Tres = 3,
        Quatro = 4,
        Cinco = 5,
        Seis = 6,
        Sete = 7,
        Valete = 11,
        Dama = 12,
        Rei = 13,
        As = 14
    }
}
