namespace SuecaOnline.Domain.Sueca.Random
{
    /// <summary>
    /// Fonte de aleatoriedade deterministica e injetavel.
    /// O dominio nunca depende de <c>System.Random</c> nem de <c>UnityEngine.Random</c>.
    /// </summary>
    public interface IRandomSource
    {
        /// <summary>Retorna um inteiro no intervalo [0, <paramref name="maxExclusive"/>).</summary>
        int Next(int maxExclusive);
    }
}
